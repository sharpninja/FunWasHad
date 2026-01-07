using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FWH.Common.Workflow.Actions;

/// <summary>
/// Typed workflow action handler.
/// Receives resolved parameters and may return variable updates (key/value strings) to apply to the instance.
/// </summary>
public interface IWorkflowActionHandler
{
    string Name { get; }

    Task<IDictionary<string,string>?> HandleAsync(ActionHandlerContext context, IDictionary<string,string> parameters, CancellationToken cancellationToken = default);
}
