using ECommerce.Shared.Infrastructure.EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Shared.Infrastructure.EventBus;

public static class EventBusHandlerExtensions
{
    public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : Event
        where THandler : class, IEventHandler<TEvent> /* This ensures that THandler 100% derives from IEventHandler<TEvent> and that's why even though non-generic type of IEventHandlers
                                                         Handle method is called, with default interface implementation still Handle(TEvent @event) is called, because we definitely have
                                                         TEvent type of the event. */
    {
        services.AddKeyedTransient<IEventHandler, THandler>(typeof(TEvent));

        services.Configure<EventHandlerRegistration>(o =>
        {
            o.EventTypes[typeof(TEvent).Name] = typeof(TEvent);
        });

        return services;
    }
}
