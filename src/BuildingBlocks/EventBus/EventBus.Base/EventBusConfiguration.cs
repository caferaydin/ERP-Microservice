namespace EventBus.Base
{
    public class EventBusConfiguration
    {
        public int ConnectionRetryCount { get; set; } = 3;
        public string DefaultTopicName { get; set; } = "ERPEventBus";
        public string EventBusConnectionString { get; set; } = String.Empty;
        public string SubscriptClientAppName { get; set; } = String.Empty; // Service
        public string EventNamePrefix { get; set; } = String.Empty;
        public string EventNameSuffix { get; set; } = "IntegrationEvent";
        public EventBusType EventBusType { get; set; } = EventBusType.RabbitMQ;
        public object Connection { get; set; }

        public bool DeleteEventPrefix => !String.IsNullOrEmpty(EventNamePrefix);
        public bool DeleteEventSuffix => !String.IsNullOrEmpty(EventNameSuffix);    
    }
}
