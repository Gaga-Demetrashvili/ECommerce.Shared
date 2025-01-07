using ECommerce.Shared.Infrastructure.EventBus;
using ECommerce.Shared.Infrastructure.EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ECommerce.Shared.Infrastructure.RabbitMq;

public class RabbitMqHostedService : IHostedService
{
    private const string ExchangeName = "ecommerce-exchange";

    private readonly IServiceProvider _serviceProvider;
    private readonly EventHandlerRegistration _handlerRegistrations;
    private readonly EventBusOptions _eventBusOptions;

    public RabbitMqHostedService(IServiceProvider serviceProvider,
        IOptions<EventHandlerRegistration> handlerRegistrations,
        IOptions<EventBusOptions> eventBusOptions)
    {
        _serviceProvider = serviceProvider;
        _handlerRegistrations = handlerRegistrations.Value;
        _eventBusOptions = eventBusOptions.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Factory.StartNew(() =>
        {
            var rabbitMQConnection = _serviceProvider.GetRequiredService<IRabbitMqConnection>();

            var channel = rabbitMQConnection.Connection.CreateModel();

            channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: "fanout",
                durable: false,
                autoDelete: false,
                null);

            channel.QueueDeclare(
                queue: _eventBusOptions.QueueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += OnMessageReceived;

            channel.BasicConsume(
                queue: _eventBusOptions.QueueName,
                autoAck: true,
                consumer: consumer,
                consumerTag: string.Empty,
                noLocal: false,
                exclusive: false,
                arguments: null);

            foreach (var (eventName, _) in _handlerRegistrations.EventTypes)
            {
                channel.QueueBind(
                    queue: _eventBusOptions.QueueName,
                    exchange: ExchangeName,
                    routingKey: eventName,
                    arguments: null);
            }
        },
        TaskCreationOptions.LongRunning);

        return Task.CompletedTask;
    }

    private void OnMessageReceived(object? sender, BasicDeliverEventArgs eventArgs)
    {
        var eventName = eventArgs.RoutingKey;
        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

        using var scope = _serviceProvider.CreateScope();

        if (!_handlerRegistrations.EventTypes.TryGetValue(eventName, out var eventType))
            return;

        var @event = JsonSerializer.Deserialize(message, eventType) as Event;

        #region Explanation
        /*   
            Polymorphism in Action

            When you resolve the IEventHandler instance (e.g., using GetKeyedServices<IEventHandler>(eventType)), 
            the resolved handler is still an instance of a class implementing IEventHandler<TEvent>, 
            which provides the specific Handle(TEvent) logic. 

            The call
            handler.Handle(@event);

            Invokes the non-generic Handle(Event @event) method on IEventHandler.

            Because of the default implementation in IEventHandler<TEvent>, it casts @event to TEvent and then calls the type-specific Handle(TEvent) method.

            Why This Design Works:
            Decoupling:

            The event bus works with IEventHandler without knowing the exact TEvent type at compile time.
            Polymorphism:

            The runtime type of the handler ensures that the correct Handle(TEvent @event) is called.
            Default Interface Implementation:

            Bridges the gap between the non-generic IEventHandler and the type-specific IEventHandler<TEvent>.

            In summary, the default implementation of IEventHandler.
            Handle(Event @event) combined with polymorphism ensures that the type-specific Handle(TEvent @event) 
            method is correctly invoked for the appropriate event type at runtime.
        */
        #endregion
        foreach (var handler in scope.ServiceProvider.GetKeyedServices<IEventHandler>(eventType))
        {
            handler.Handle(@event);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
