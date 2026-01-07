using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Workflow.Models;
using FWH.Common.Workflow.Instance;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace FWH.Common.Workflow.Actions;

/// <summary>
/// Basic workflow action executor.
/// - Parses JSON action definitions from node.NoteMarkdown
/// - Replaces template parameters using instance manager variables
/// - Dispatches to registered typed handlers resolved from a registry.
/// Handlers may return variable updates which will be applied to the instance manager.
/// </summary>
public class WorkflowActionExecutor : IWorkflowActionExecutor
{
    private readonly ILogger<WorkflowActionExecutor> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkflowActionHandlerRegistry _registry;
    private readonly WorkflowActionExecutorOptions _options;

    public WorkflowActionExecutor(IServiceProvider serviceProvider, IWorkflowActionHandlerRegistry registry, IOptions<WorkflowActionExecutorOptions>? options = null, ILogger<WorkflowActionExecutor>? logger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<WorkflowActionExecutor>.Instance;
        _options = options?.Value ?? new WorkflowActionExecutorOptions();

        // Eagerly trigger registrar if available so handlers registered as singletons get wired when DI builds
        var registrar = _serviceProvider.GetService<WorkflowActionHandlerRegistrar>();
        // registrar's constructor performs registration
    }

    public async Task<bool> ExecuteAsync(string workflowId, WorkflowNode node, WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        if (node == null) return false;

        var note = node.NoteMarkdown?.Trim();
        if (string.IsNullOrWhiteSpace(note))
            return false;

        // Detect JSON action
        if (!note.StartsWith('{') || !note.EndsWith('}'))
            return false;

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(note);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON action on node {NodeId}", node.Id);
            return false;
        }

        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return false;

        if (!doc.RootElement.TryGetProperty("action", out var actionProp))
            return false;

        var actionName = actionProp.GetString();
        if (string.IsNullOrWhiteSpace(actionName))
            return false;

        // Resolve params object (if present)
        IDictionary<string, string> parameters = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (doc.RootElement.TryGetProperty("params", out var paramsProp) && paramsProp.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in paramsProp.EnumerateObject())
            {
                parameters[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }
        }

        // Resolve templates in parameters using instance variables if available
        // Resolve instance manager from root; when creating a scope we will re-resolve from the scope provider
        var rootInstanceManager = _serviceProvider.GetService<IWorkflowInstanceManager>();
        IDictionary<string, string>? variables = null;
        if (rootInstanceManager != null)
        {
            variables = rootInstanceManager.GetVariables(workflowId) ?? new ConcurrentDictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        }

        var resolved = ResolveTemplates(parameters, variables);

        // Try registry for typed handler
        if (!_registry.TryGetFactory(actionName ?? string.Empty, out var factory))
        {
            // Fallback: check for any IWorkflowActionHandler singletons registered directly in the container
            var directHandlers = _serviceProvider.GetServices<IWorkflowActionHandler>();
            var direct = directHandlers.FirstOrDefault(h => string.Equals(h.Name, actionName, StringComparison.OrdinalIgnoreCase));
            if (direct != null)
            {
                factory = _ => direct;
            }
            else
            {
                _logger.LogWarning("No handler registered for action {ActionName}", actionName);
                return false;
            }
        }
 
         try
         {
             // Create a scope depending on options
             if (_options.CreateScopeForHandlers)
             {
                using var scope = _serviceProvider.CreateScope();
                var handler = factory(scope.ServiceProvider);
                if (handler == null)
                {
                    _logger.LogWarning("Handler factory returned null for action {ActionName}", actionName);
                    return false;
                }
                
                // Resolve instance manager from the created scope so handler and state updates target same instance
                var scopedInstanceManager = scope.ServiceProvider.GetService<IWorkflowInstanceManager>() ?? throw new InvalidOperationException("InstanceManager required in handler context");
                var ctx = new ActionHandlerContext(workflowId, node, definition, scopedInstanceManager);
                
                var sw = Stopwatch.StartNew();
                var updates = await handler.HandleAsync(ctx, resolved, cancellationToken);
                sw.Stop();
                
                if (_options.LogExecutionTime)
                {
                    _logger.LogInformation("Action {ActionName} handled by {HandlerName} in {ElapsedMs}ms", actionName, handler.Name ?? "<null>", sw.ElapsedMilliseconds);
                }
                
                // If handler returned variable updates, apply them to instance manager
                if (updates != null)
                {
                    foreach (var kv in updates)
                    {
                        scopedInstanceManager.SetVariable(workflowId, kv.Key, kv.Value);
                    }
                }
             }
             else
             {
                var handler = factory(_serviceProvider);
                if (handler == null)
                {
                    _logger.LogWarning("Handler factory returned null for action {ActionName}", actionName);
                    return false;
                }
                
                var rootMgr = _serviceProvider.GetService<IWorkflowInstanceManager>() ?? throw new InvalidOperationException("InstanceManager required in handler context");
                var ctx = new ActionHandlerContext(workflowId, node, definition, rootMgr);
                
                var sw = Stopwatch.StartNew();
                var updates = await handler.HandleAsync(ctx, resolved, cancellationToken);
                sw.Stop();
                
                if (_options.LogExecutionTime)
                {
                    _logger.LogInformation("Action {ActionName} handled by {HandlerName} in {ElapsedMs}ms", actionName, handler.Name ?? "<null>", sw.ElapsedMilliseconds);
                }
                
                // If handler returned variable updates, apply them to instance manager
                if (updates != null)
                {
                    foreach (var kv in updates)
                    {
                        rootMgr.SetVariable(workflowId, kv.Key, kv.Value);
                    }
                }
             }

             return true;
         }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Action {ActionName} execution cancelled", actionName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Action handler for {ActionName} threw an exception", actionName);
            return false;
        }
    }

    private static IDictionary<string,string> ResolveTemplates(IDictionary<string,string> parameters, IDictionary<string,string>? variables)
    {
        var result = new ConcurrentDictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        var pattern = new Regex(@"\{\{\s*(?<key>[a-zA-Z0-9_.]+)\s*\}\}", RegexOptions.Compiled);

        foreach (var kv in parameters)
        {
            var val = kv.Value ?? string.Empty;
            var newVal = pattern.Replace(val, m =>
            {
                var key = m.Groups["key"].Value;
                if (variables != null && variables.TryGetValue(key, out var v))
                    return v ?? string.Empty;
                return string.Empty;
            });

            result[kv.Key] = newVal;
        }

        return result;
    }
}
