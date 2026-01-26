namespace FWH.Orchestrix.Contracts.Mediator;

/// <summary>
/// Marker interface for mediator requests.
/// </summary>
/// <typeparam name="TResponse">Response type.</typeparam>
#pragma warning disable CA1040 // Avoid empty interfaces - intentional marker for mediator request type identity
public interface IMediatorRequest<out TResponse>
{
}
#pragma warning restore CA1040
