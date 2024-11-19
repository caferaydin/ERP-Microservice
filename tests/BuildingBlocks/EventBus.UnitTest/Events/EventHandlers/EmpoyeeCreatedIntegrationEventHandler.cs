using EventBus.Base.Abstraction;
using EventBus.UnitTest.Events.Events;

namespace EventBus.UnitTest.Events.EventHandlers
{
    public class EmpoyeeCreatedIntegrationEventHandler : IIntegrationEventHandler<EmpoyeeCreatedIntegrationEvent>
    {
        public Task Handle(EmpoyeeCreatedIntegrationEvent @event)
        {
            Console.WriteLine("handle method id : " , @event.Id);
          return Task.CompletedTask;
        }
    }
}
