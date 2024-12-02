using EventBus.Base.Events;

namespace PaymentService.API.IntegrationEvents.Events

{
    public class OrderStartedntegrationEvent : IntegrationEvent
    {
        public int OrderId { get; set; }

        public OrderStartedntegrationEvent()
        {

        }

        public OrderStartedntegrationEvent(int orderId)
        {
            OrderId = orderId;
        }
    }
}
