using System.Threading;
using System.Threading.Tasks;

namespace FWH.Orchestrix.Contracts.Mediator;

/// <summary>
/// Contract for sending mediator requests.
/// </summary>
public interface IMediatorSender
{
    Task<TResponse> SendAsync<TResponse>(IMediatorRequest<TResponse> request, CancellationToken cancellationToken = default);
}
