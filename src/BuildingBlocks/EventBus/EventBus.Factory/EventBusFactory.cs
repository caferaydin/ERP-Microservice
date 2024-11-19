using EventBus.AzureService;
using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.RabbitMQ;

namespace EventBus.Factory
{
    public static class EventBusFactory
    {
        public static IEventBus Create(IServiceProvider serviceProvider, EventBusConfiguration configuration)
        {
            return configuration.EventBusType switch
            {
                EventBusType.AzureService => new EventBusServiceBus(serviceProvider, configuration),
                _ => new EventBusRabbitMQ(serviceProvider, configuration)
            };
        }
    }
}
