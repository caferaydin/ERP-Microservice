﻿using EventBus.Base.Abstraction;
using EventBus.Base.SubManagers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace EventBus.Base.Events
{
    public abstract class BaseEventBus : IEventBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventBusSubscriptionManager _subscriptionManager;

        private EventBusConfiguration _configuration;

        protected BaseEventBus(IServiceProvider serviceProvider,  EventBusConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _subscriptionManager = new InMemoryEventBusSubscriptionManager(ProcessEventName);
            _configuration = configuration;
        }

        public virtual string ProcessEventName(string eventName) 
        {
            if (_configuration.DeleteEventPrefix)
                eventName = eventName.TrimStart(_configuration.EventNamePrefix.ToArray());

            if(_configuration.DeleteEventSuffix)
                eventName = eventName.TrimEnd(_configuration.EventNameSuffix.ToArray());

            return eventName;
        }

        public virtual string GetSubName(string eventName)
        {
            return $"{_configuration.SubscriptClientAppName}.{ProcessEventName(eventName)}";
        }

        public virtual void Dispose()
        {
            _configuration = null;
        }

        public async Task<bool> ProcessEvent(string eventName, string message)
        {
            eventName = ProcessEventName(eventName);

            var processed = false;

            if(_subscriptionManager.HasSubscriptionsForEvent(eventName))
            {
                var subScriptions = _subscriptionManager.GetHandlersForEvent(eventName);

                using (var scope = _serviceProvider.CreateScope())
                {
                    foreach (var subScription in subScriptions)
                    {
                        var handler = _serviceProvider.GetService(subScription.HandlerType);
                        if (handler != null) continue;
                        
                        var eventType = _subscriptionManager.GetEventTypeByName($"{_configuration.EventNamePrefix}{eventName}{_configuration.EventNameSuffix}");
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);

                        //if (integrationEvent is IntegrationEvent)
                        //{
                        //    _configuration.CorrelationIdSetter?.Invoke((integrationEvent as IntegrationEvent).CorrelationId);
                        //}

                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                    }
                }
                processed = true;
            }
            return processed;
        }

        public void Publish(IntegrationEvent @event)
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            throw new NotImplementedException();
        }

        public void UnSubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            throw new NotImplementedException();
        }
    }
}
