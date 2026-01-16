using System.Threading;
using System.Threading.Tasks;

namespace FWH.Orchestrix.Contracts.Mediator;

/// <summary>
/// Contract for handling mediator requests.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
public interface IMediatorHandler<in TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
