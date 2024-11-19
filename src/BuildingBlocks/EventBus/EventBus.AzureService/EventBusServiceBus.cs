using EventBus.Base;
using EventBus.Base.Events;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Reflection.Emit;
using System.Text;

namespace EventBus.AzureService
{
    public class EventBusServiceBus : BaseEventBus
    {
        private ITopicClient _topicClient;
        private ManagementClient _managamentClient;
        private ILogger _logger;
        public EventBusServiceBus(IServiceProvider serviceProvider, EventBusConfiguration configuration) : base(serviceProvider, configuration)
        {
            _logger = serviceProvider.GetService(typeof(ILogger<EventBusServiceBus>)) as  ILogger<EventBusServiceBus>;
            _managamentClient = new ManagementClient(_configuration.EventBusConnectionString);
            _topicClient = CreateTopicClient();
        }

        private ITopicClient CreateTopicClient()
        {
            if (_topicClient == null || _topicClient.IsClosedOrClosing)
            {
                _topicClient = new TopicClient(_configuration.EventBusConnectionString, _configuration.DefaultTopicName, RetryPolicy.Default);
            }

            if (!_managamentClient.TopicExistsAsync(_configuration.DefaultTopicName).GetAwaiter().GetResult())
                _managamentClient.CreateTopicAsync(_configuration.DefaultTopicName).GetAwaiter().GetResult();

            return _topicClient;
        }

        public override void Publish(IntegrationEvent @event)
        {
            var eventName = @event.GetType().Name;  // EventNameIntegrationEvent
            eventName = ProcessEventName(eventName); // EventName [Trim("IntegrationEvent")]

            var eventStr = JsonConvert.SerializeObject(@event);
            var bodyArray = Encoding.UTF8.GetBytes(eventStr);

            var message = new Message()
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = bodyArray,
                Label = eventName,
            };
            _topicClient.SendAsync(message).GetAwaiter().GetResult();
        }

        public override void Subscribe<T, TH>()
        {
            var eventName = typeof(T).Name;
            
            if(!_subscriptionManager.HasSubscriptionsForEvent(eventName))
            {
                var subScriptionCleint =  CreateSubscriptionClientIfNotExists(eventName);
                RegisterSubscriptionClientMessageHandler(subScriptionCleint);
            }

            _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);
            _subscriptionManager.AddSubscription<T, TH>();

        }


        public override void UnSubscribe<T, TH>()
        {
            var eventName = typeof(T).Name;

            try
            {
                var subScriptionClient = CreateSubscriptionClient(eventName);

                subScriptionClient
                    .RemoveRuleAsync(eventName)
                    .GetAwaiter().GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                _logger.LogWarning("The messaging entity {EventName} could not be found", eventName);
            }

            _logger.LogInformation($"Unsubscribe: {eventName}");

            _subscriptionManager.RemoveSubscription<T, TH>();
        }

        private void RegisterSubscriptionClientMessageHandler(ISubscriptionClient subscriptionClient)
        {
            subscriptionClient.RegisterMessageHandler(
                async (message, token) =>
                {
                    var eventName = $"{message.Label}";
                    var messageData = Encoding.UTF8.GetString(message.Body);

                    if (await ProcessEvent(ProcessEventName(eventName), messageData))
                    {
                        await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                    }
                },
                new MessageHandlerOptions(ExceptionReceivedHandler) { MaxConcurrentCalls = 10, AutoComplete = false });
        }

        private Task ExceptionReceivedHandler (ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var ex = exceptionReceivedEventArgs.Exception;
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

            _logger.LogError(ex, "Error handling message: {ExceptionMessage} - Context {@ExceptionContext}", ex.Message, context);

            return Task.CompletedTask;  
        }

        private SubscriptionClient CreateSubscriptionClient(string eventName)
        {
            return new SubscriptionClient(_configuration.EventBusConnectionString, _configuration.DefaultTopicName, GetSubName(eventName));
        }

        private ISubscriptionClient CreateSubscriptionClientIfNotExists(string eventName)
        {
            var subClient = CreateSubscriptionClient(eventName);

            var exists =_managamentClient.SubscriptionExistsAsync(_configuration.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult(); 
            if (!exists)
            {
                _managamentClient.CreateSubscriptionAsync(_configuration.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult();
                RemoveDefaultRule(subClient);
            }
            CreateRuleIfNotExists(ProcessEventName(eventName),subClient);

            return subClient;
        }

        private void CreateRuleIfNotExists(string eventName, ISubscriptionClient subscriptionClient)
        {
            bool ruleExists;

            try
            {
                var rule = _managamentClient.GetRuleAsync(_configuration.DefaultTopicName, eventName, eventName)
                    .GetAwaiter().GetResult();
                ruleExists = rule != null;
            }
            catch (MessagingEntityNotFoundException)
            {
                ruleExists = false;
            }
            if (!ruleExists)
            {
                subscriptionClient.AddRuleAsync(new()
                {
                    Filter = new CorrelationFilter { Label = eventName},
                    Name = eventName,
                }).GetAwaiter().GetResult();
            }
        }

        private void RemoveDefaultRule(SubscriptionClient subscriptionClient)
        {
            try
            {
                subscriptionClient
                    .RemoveRuleAsync(RuleDescription.DefaultRuleName)
                    .GetAwaiter().GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                _logger.LogWarning("The messaging entity {DefaultRuleName} could not be found.", RuleDescription.DefaultRuleName);
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            _topicClient.CloseAsync().GetAwaiter().GetResult();
            _managamentClient.CloseAsync().GetAwaiter().GetResult();
            _topicClient = null;
            _managamentClient = null;
        }
    }
}
