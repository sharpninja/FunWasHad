using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace FWH.Common.Workflow.Actions;

public partial class WorkflowActionHandlerRegistry : IWorkflowActionHandlerRegistry
{
    [LoggerMessage(LogLevel.Information, "Registered workflow action handler factory for {Action}")]
    private static partial void LogRegisteredFactory(ILogger logger, string action);

    private readonly ConcurrentDictionary<string, Func<IServiceProvider, IWorkflowActionHandler>> _factories = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<WorkflowActionHandlerRegistry>? _logger;

    public WorkflowActionHandlerRegistry(ILogger<WorkflowActionHandlerRegistry>? logger = null)
    {
        _logger = logger;
    }

    public void Register(string name, Func<IServiceProvider, IWorkflowActionHandler> factory)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        ArgumentNullException.ThrowIfNull(factory);

        _factories[name] = factory;
        if (_logger != null)
            LogRegisteredFactory(_logger, name);
    }

    public bool TryGetFactory(string name, out Func<IServiceProvider, IWorkflowActionHandler>? factory)
    {
        if (string.IsNullOrWhiteSpace(name)) { factory = null; return false; }
        return _factories.TryGetValue(name, out factory);
    }
}
