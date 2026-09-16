using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TukiFact.Common.Domain.DomainEvents;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.Application.Messaging;

internal sealed class Mediator(IServiceProvider serviceProvider) : ISender, IPublisher
{
    private static readonly ConcurrentDictionary<(Type Request, Type Response), Delegate> SendInvokers = new();
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> NotificationInvokers = new();
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> DomainEventInvokers = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        where TResponse : Result
    {
        ArgumentNullException.ThrowIfNull(request);

        var invoker = (Func<IServiceProvider, IRequest<TResponse>, CancellationToken, Task<TResponse>>)SendInvokers.GetOrAdd(
            (request.GetType(), typeof(TResponse)),
            static key => CreateInvoker<Func<IServiceProvider, IRequest<TResponse>, CancellationToken, Task<TResponse>>>(
                nameof(SendCore), key.Request, key.Response));

        return invoker(serviceProvider, request, cancellationToken);
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var invoker = NotificationInvokers.GetOrAdd(
            notification.GetType(),
            static type => CreateInvoker<Func<IServiceProvider, object, CancellationToken, Task>>(nameof(PublishNotificationCore), type));

        return invoker(serviceProvider, notification, cancellationToken);
    }

    public Task Publish(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var invoker = DomainEventInvokers.GetOrAdd(
            domainEvent.GetType(),
            static type => CreateInvoker<Func<IServiceProvider, object, CancellationToken, Task>>(nameof(PublishDomainEventCore), type));

        return invoker(serviceProvider, domainEvent, cancellationToken);
    }

    private static TDelegate CreateInvoker<TDelegate>(string methodName, params Type[] typeArguments)
        where TDelegate : Delegate =>
        typeof(Mediator)
            .GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(typeArguments)
            .CreateDelegate<TDelegate>();

    // Public (on an internal type) so the cached generic invokers are built without an accessibility bypass.
    public static Task<TResponse> SendCore<TRequest, TResponse>(
        IServiceProvider services,
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
        where TResponse : Result
    {
        var typedRequest = (TRequest)request;
        var handler = services.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = services.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();

        Func<Task<TResponse>> pipeline = () => handler.Handle(typedRequest, cancellationToken);
        for (var index = behaviors.Length - 1; index >= 0; index--)
        {
            var behavior = behaviors[index];
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, cancellationToken);
        }

        return pipeline();
    }

    public static async Task PublishNotificationCore<TNotification>(
        IServiceProvider services,
        object notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        foreach (var handler in services.GetServices<INotificationHandler<TNotification>>())
        {
            await handler.Handle((TNotification)notification, cancellationToken);
        }
    }

    public static async Task PublishDomainEventCore<TDomainEvent>(
        IServiceProvider services,
        object domainEvent,
        CancellationToken cancellationToken)
        where TDomainEvent : IDomainEvent
    {
        foreach (var handler in services.GetServices<IDomainEventHandler<TDomainEvent>>())
        {
            await handler.Handle((TDomainEvent)domainEvent, cancellationToken);
        }
    }
}
