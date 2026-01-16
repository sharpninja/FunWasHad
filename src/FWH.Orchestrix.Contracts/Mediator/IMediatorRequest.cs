namespace FWH.Orchestrix.Contracts.Mediator;

/// <summary>
/// Marker interface for mediator requests.
/// </summary>
/// <typeparam name="TResponse">Response type.</typeparam>
public interface IMediatorRequest<out TResponse>
{
}
