using EventBus.Base.Abstraction;
using EventBus.Base.Events;
using PaymentService.API.IntegrationEvents.Events;

namespace PaymentService.API.IntegrationEvents.EventHandlers
{
    public class OrderStartedIntegrationEventHandler : IIntegrationEventHandler<OrderStartedntegrationEvent>
    {

        private readonly IConfiguration _configuration;
        private readonly IEventBus _eventBus;
        private readonly Logger<OrderStartedIntegrationEventHandler> _logger;

        public OrderStartedIntegrationEventHandler(IConfiguration configuration, IEventBus eventBus, Logger<OrderStartedIntegrationEventHandler> logger)
        {
            _configuration = configuration;
            _eventBus = eventBus;
            _logger = logger;
        }

        public Task Handle(OrderStartedntegrationEvent @event)
        {
            // Payment Process
            string keyword = "PaymentSuccess";

            bool paymentSuccessFlag = _configuration.GetValue<bool>(keyword);

            IntegrationEvent paymentEvent = paymentSuccessFlag
                ? new OrderPaymentSuccessIntegrationEvent(@event.OrderId)
                : new OrderPaymentFailedIntegrationEvent(@event.OrderId, "Error Message");

            _logger.LogInformation($"OrderCreatedIntegrationEventHandler in PaymentService is fired with PaymentSuccess: {paymentSuccessFlag}, orderId : {@event.OrderId}");

            _eventBus.Publish(paymentEvent);

            return Task.CompletedTask;  
        }
    }
}
