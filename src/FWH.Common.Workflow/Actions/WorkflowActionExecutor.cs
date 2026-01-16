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
using System.Linq;
using FWH.Orchestrix.Contracts.Mediator;

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
    private readonly IMediatorSender? _mediator;
    private readonly WorkflowActionExecutorOptions _options;

    //public WorkflowActionExecutor(IServiceProvider serviceProvider, IWorkflowActionHandlerRegistry registry, IOptions<WorkflowActionExecutorOptions>? options = null, ILogger<WorkflowActionExecutor>? logger = null)
    //{
    //    _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    //    _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    //    _mediator = null;
    //    _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<WorkflowActionExecutor>.Instance;
    //    _options = options?.Value ?? new WorkflowActionExecutorOptions();

    //    // Eagerly trigger registrar if available so handlers registered as singletons get wired when DI builds
    //    var registrar = _serviceProvider.GetService<WorkflowActionHandlerRegistrar>();
    //    // registrar's constructor performs registration
    //}

    public WorkflowActionExecutor(IServiceProvider serviceProvider,
                                  IMediatorSender mediator,
                                  IOptions<WorkflowActionExecutorOptions>? options = null,
                                  ILogger<WorkflowActionExecutor>? logger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _registry = _serviceProvider.GetService<IWorkflowActionHandlerRegistry>() ?? new WorkflowActionHandlerRegistry();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<WorkflowActionExecutor>.Instance;
        _options = options?.Value ?? new WorkflowActionExecutorOptions();
    }

    public async Task<bool> ExecuteAsync(string workflowId, WorkflowNode node, WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        if (node == null) return false;

        // NoteMarkdown is primary; fall back to JsonMetadata (some PlantUML notes may have been parsed into JsonMetadata)
        var note = node.NoteMarkdown?.Trim();
        if (string.IsNullOrWhiteSpace(note) && !string.IsNullOrWhiteSpace(node.JsonMetadata))
        {
            note = node.JsonMetadata.Trim();
        }
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
        var rootInstanceManager = _serviceProvider.GetService<IWorkflowInstanceManager>();
        IDictionary<string, string>? variables = null;
        if (rootInstanceManager != null)
        {
            variables = rootInstanceManager.GetVariables(workflowId) ?? new ConcurrentDictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        }

        var resolved = ResolveTemplates(parameters, variables);

        if (_mediator != null)
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await _mediator.SendAsync(new WorkflowActionRequest
                {
                    WorkflowId = workflowId,
                    Node = node,
                    Definition = definition,
                    ActionName = actionName,
                    Parameters = resolved,
                    CreateScopeForHandlers = _options.CreateScopeForHandlers,
                    LogExecutionTime = _options.LogExecutionTime
                }, cancellationToken).ConfigureAwait(false);

                sw.Stop();
                if (_options.LogExecutionTime)
                {
                    _logger.LogInformation("Action {ActionName} handled by mediator in {ElapsedMs}ms", actionName, sw.ElapsedMilliseconds);
                }

                var successfull = response.Success && response.VariableUpdates != null && rootInstanceManager != null && !response.VariableUpdates["status"].Equals("error", StringComparison.InvariantCultureIgnoreCase);

                if (successfull)
                {
                    foreach (var update in response.VariableUpdates!)
                    {
                        rootInstanceManager!.SetVariable(workflowId, update.Key, update.Value);
                    }
                }

                return successfull;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Action {ActionName} execution failed via mediator", actionName);
                // Return false but don't throw - allows workflow to continue
                return false;
            }
        }

        // Prefer any singleton handlers registered directly in DI (common for delegate adapters)
        var directHandlersList = _serviceProvider.GetServices<IWorkflowActionHandler>();
        var directHandler = directHandlersList.FirstOrDefault(h => string.Equals(h.Name, actionName, StringComparison.OrdinalIgnoreCase));
        Func<IServiceProvider, IWorkflowActionHandler> factory = null!;
        if (directHandler != null)
        {
            _logger.LogInformation("Using direct singleton handler for action {ActionName}", actionName);
            factory = _ => directHandler;
        }
        else if (_registry.TryGetFactory(actionName ?? string.Empty, out var regFactory))
        {
            factory = regFactory!;
        }
        else
        {
            _logger.LogWarning("No handler registered for action {ActionName}", actionName);
            return false;
        }

        // Execution delegate shared by sync and background paths
        async Task ExecuteHandlerAsync(Func<IServiceProvider, IWorkflowActionHandler> factoryFunc)
        {
            try
            {
                if (_options.CreateScopeForHandlers)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var handler = factoryFunc(scope.ServiceProvider);
                    _logger.LogInformation("Factory produced handler instance {Handler} for action {ActionName}", handler?.Name, actionName);

                    // Fallback: if factory returns null, try resolving singleton handlers from the scoped provider
                    if (handler == null)
                    {
                        var direct = scope.ServiceProvider.GetServices<IWorkflowActionHandler>().FirstOrDefault(h => string.Equals(h.Name, actionName, StringComparison.OrdinalIgnoreCase));
                        if (direct != null)
                        {
                            handler = direct;
                        }
                    }

                    if (handler == null)
                    {
                        _logger.LogWarning("Handler factory returned null for action {ActionName}", actionName);
                        return;
                    }

                    var scopedInstanceManager = scope.ServiceProvider.GetService<IWorkflowInstanceManager>() ?? throw new InvalidOperationException("InstanceManager required in handler context");
                    var ctx = new ActionHandlerContext(workflowId, node, definition, scopedInstanceManager);

                    var sw = Stopwatch.StartNew();
                    IDictionary<string,string>? updates = null;
                    try
                    {
                        _logger.LogInformation("Invoking handler {HandlerName} for workflow {WorkflowId} node {NodeId}", handler.Name, workflowId, node.Id);
                        updates = await handler.HandleAsync(ctx, resolved, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Action {ActionName} execution cancelled", actionName);
                        throw; // Re-throw to be caught by outer try-catch
                    }

                    sw.Stop();
                    if (_options.LogExecutionTime)
                    {
                        _logger.LogInformation("Action {ActionName} handled by {HandlerName} in {ElapsedMs}ms", actionName, handler.Name ?? "<null>", sw.ElapsedMilliseconds);
                    }

                    if (updates != null)
                    {
                        foreach (var kv in updates)
                        {
                            try
                            {
                                scopedInstanceManager.SetVariable(workflowId, kv.Key, kv.Value);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to set variable {Key} from handler {ActionName}", kv.Key, actionName);
                            }
                        }
                    }
                }
                else
                {
                    var handler = factoryFunc(_serviceProvider);
                    _logger.LogInformation("Factory produced handler instance {Handler} for action {ActionName}", handler?.Name, actionName);

                    // Fallback: if factory returns null, try resolving singleton handlers from the provider
                    if (handler == null)
                    {
                        var direct = _serviceProvider.GetServices<IWorkflowActionHandler>().FirstOrDefault(h => string.Equals(h.Name, actionName, StringComparison.OrdinalIgnoreCase));
                        if (direct != null)
                        {
                            handler = direct;
                        }
                    }

                    if (handler == null)
                    {
                        _logger.LogWarning("Handler factory returned null for action {ActionName}", actionName);
                        return;
                    }

                    var rootMgr = _serviceProvider.GetService<IWorkflowInstanceManager>() ?? throw new InvalidOperationException("InstanceManager required in handler context");
                    var ctx = new ActionHandlerContext(workflowId, node, definition, rootMgr);

                    var sw = Stopwatch.StartNew();
                    IDictionary<string,string>? updates = null;
                    try
                    {
                        _logger.LogInformation("Invoking handler {HandlerName} for workflow {WorkflowId} node {NodeId}", handler.Name, workflowId, node.Id);
                        updates = await handler.HandleAsync(ctx, resolved, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Action {ActionName} execution cancelled", actionName);
                        throw; // Re-throw to be caught by outer try-catch
                    }

                    sw.Stop();
                    if (_options.LogExecutionTime)
                    {
                        _logger.LogInformation("Action {ActionName} handled by {HandlerName} in {ElapsedMs}ms", actionName, handler.Name ?? "<null>", sw.ElapsedMilliseconds);
                    }

                    if (updates != null)
                    {
                        foreach (var kv in updates)
                        {
                            try
                            {
                                rootMgr.SetVariable(workflowId, kv.Key, kv.Value);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to set variable {Key} from handler {ActionName}", kv.Key, actionName);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Propagate cancellation to outer handler
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Action handler for {ActionName} threw an exception", actionName);
            }
        };

        if (_options.ExecuteHandlersInBackground)
        {
            // Launch background task
            _ = Task.Run(() => ExecuteHandlerAsync(factory));
            return true;
        }
        else
        {
            // Execute synchronously and await completion so callers (tests) observe effects
            try
            {
                await ExecuteHandlerAsync(factory).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException)
            {
                // Return false only when cancellation occurs in synchronous mode
                return false;
            }
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
