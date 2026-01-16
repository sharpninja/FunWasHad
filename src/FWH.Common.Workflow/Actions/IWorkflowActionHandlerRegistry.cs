using System;

namespace FWH.Common.Workflow.Actions;

public interface IWorkflowActionHandlerRegistry
{
    void Register(string name, Func<IServiceProvider, IWorkflowActionHandler> factory);
    bool TryGetFactory(string name, out Func<IServiceProvider, IWorkflowActionHandler>? factory);
}
