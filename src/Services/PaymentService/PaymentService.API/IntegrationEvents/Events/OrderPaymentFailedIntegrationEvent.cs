using EventBus.Base.Events;

namespace PaymentService.API.IntegrationEvents.Events
{
    public class OrderPaymentFailedIntegrationEvent : IntegrationEvent
    {
        public int OrderId { get;}
        public string? ErrorMessage { get;}

        public OrderPaymentFailedIntegrationEvent(int orderId) => 
            OrderId = orderId;

        public OrderPaymentFailedIntegrationEvent(int orderId, string? errorMessage) 
        {
            OrderId = orderId;
            ErrorMessage = errorMessage;
        }
    }
}
