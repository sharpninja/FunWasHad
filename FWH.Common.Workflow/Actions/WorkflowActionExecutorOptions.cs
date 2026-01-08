namespace FWH.Common.Workflow.Actions;

public class WorkflowActionExecutorOptions
{
    /// <summary>
    /// When true the executor will create a DI scope for every handler execution so scoped services are available.
    /// </summary>
    public bool CreateScopeForHandlers { get; set; } = true;

    /// <summary>
    /// When true the executor will log handler execution time.
    /// </summary>
    public bool LogExecutionTime { get; set; } = true;

    /// <summary>
    /// When true handlers will be executed in background (fire-and-forget) and executor returns immediately.
    /// Default false preserves previous synchronous behavior expected by tests.
    /// </summary>
    public bool ExecuteHandlersInBackground { get; set; } = false;
}
