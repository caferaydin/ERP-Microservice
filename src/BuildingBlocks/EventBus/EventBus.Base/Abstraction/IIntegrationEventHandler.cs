using EventBus.Base.Events;

namespace EventBus.Base.Abstraction
{
    public interface IIntegrationEventHandler<T_IntegrationEvent> : IntegrationEventHandler where T_IntegrationEvent : IntegrationEvent
    {
        Task Handle(T_IntegrationEvent @event);
    }

    public interface IntegrationEventHandler
    {

    }
}
