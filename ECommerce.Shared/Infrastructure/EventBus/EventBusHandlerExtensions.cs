using ECommerce.Shared.Infrastructure.EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Shared.Infrastructure.EventBus;

public static class EventBusHandlerExtensions
{
    public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : Event
        where THandler : class, IEventHandler<TEvent>
    {
        services.AddKeyedTransient<IEventHandler, THandler>(typeof(TEvent));

        services.Configure<EventHandlerRegistration>(o =>
        {
            o.EventTypes[typeof(TEvent).Name] = typeof(TEvent);
        });

        return services;
    }
}
