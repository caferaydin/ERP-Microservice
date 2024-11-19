using EventBus.Base.Events;

namespace EventBus.UnitTest.Events.Events
{
    public class EmpoyeeCreatedIntegrationEvent : IntegrationEvent
    {
        public int Id { get; set; }

        public EmpoyeeCreatedIntegrationEvent(int id)
        {
            Id = id;
        }
    }


}
