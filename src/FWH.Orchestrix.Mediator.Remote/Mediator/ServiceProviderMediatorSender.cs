using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using FWH.Orchestrix.Contracts.Mediator;

namespace FWH.Orchestrix.Mediator.Remote.Mediator;

public sealed class ServiceProviderMediatorSender : IMediatorSender
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceProviderMediatorSender(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> SendAsync<TResponse>(IMediatorRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var handlerType = typeof(IMediatorHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetService(handlerType);
        if (handler is null)
        {
            throw new InvalidOperationException($"No mediator handler registered for request type '{request.GetType().FullName}'.");
        }

        var method = handlerType.GetMethod("HandleAsync");
        if (method is null)
        {
            throw new InvalidOperationException($"Handler type '{handlerType.FullName}' does not implement HandleAsync.");
        }

        var result = method.Invoke(handler, new object?[] { request, cancellationToken });
        if (result is Task<TResponse> typed)
        {
            return typed;
        }

        throw new InvalidOperationException($"Handler '{handlerType.FullName}' returned unexpected result type.");
    }
}
