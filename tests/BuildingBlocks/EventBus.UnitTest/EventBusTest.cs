using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.Factory;
using EventBus.UnitTest.Events.EventHandlers;
using EventBus.UnitTest.Events.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;


namespace EventBus.UnitTest
{
    [TestClass]
    public class EventBusTest
    {
        private ServiceCollection services;

        public EventBusTest()
        {
            services = new ServiceCollection();
            services.AddLogging(configure => configure.AddConsole());
        }

        [TestMethod]
        public void subscribe_event_on_rabbitmq_test()
        {
            services.AddSingleton<IEventBus>(sp =>
            {
                return EventBusFactory.Create(sp, GetRabbitMQConfig());
            });

            var sp = services.BuildServiceProvider();

            var eventBus = sp.GetRequiredService<IEventBus>();

            eventBus.Subscribe<EmpoyeeCreatedIntegrationEvent, EmpoyeeCreatedIntegrationEventHandler>();
            eventBus.UnSubscribe<EmpoyeeCreatedIntegrationEvent, EmpoyeeCreatedIntegrationEventHandler>();

        }

        [TestMethod]
        public void send_message_to_rabbitmq()
        {
            services.AddSingleton<IEventBus>(sp =>
            {
                return EventBusFactory.Create(sp, GetRabbitMQConfig());
            });

            var sp = services.BuildServiceProvider();
            var eventBus = sp.GetRequiredService<IEventBus>();

            eventBus.Publish(new EmpoyeeCreatedIntegrationEvent(1));

        }

       

        [TestMethod]
        public void subscribe_event_on_azure_test()
        {
            services.AddSingleton<IEventBus>(sp =>
            {
                return EventBusFactory.Create(sp, GetAzureConfig());
            });

            var sp = services.BuildServiceProvider();

            var eventBus = sp.GetRequiredService<IEventBus>();

            eventBus.Subscribe<EmpoyeeCreatedIntegrationEvent, EmpoyeeCreatedIntegrationEventHandler>();
            eventBus.UnSubscribe<EmpoyeeCreatedIntegrationEvent, EmpoyeeCreatedIntegrationEventHandler>();

        }


        [TestMethod]
        public void send_message_to_azure()
        {
            services.AddSingleton<IEventBus>(sp =>
            {
                return EventBusFactory.Create(sp, GetAzureConfig());
            });

            var sp = services.BuildServiceProvider();
            var eventBus = sp.GetRequiredService<IEventBus>();

            eventBus.Publish(new EmpoyeeCreatedIntegrationEvent(1));

        }

        private  EventBusConfiguration GetRabbitMQConfig()
        {
            return new EventBusConfiguration()
            {
                ConnectionRetryCount = 5,
                SubscriptClientAppName = "EventBusUnitTest",
                DefaultTopicName = "ERPTopicName",
                EventBusType = EventBusType.RabbitMQ,
                EventNameSuffix = "IntegrationEvent",
                //Connection = new ConnectionFactory()
                //{
                //    HostName = "localhost",
                //    Port  = 15672,
                //    UserName = "guest",
                //    Password = "guest"
                //}

            };
        }

        private EventBusConfiguration GetAzureConfig()
        {
            return new EventBusConfiguration()
            {
                ConnectionRetryCount = 5,
                SubscriptClientAppName = "EventBusUnitTest",
                DefaultTopicName = "ERPTopicName",
                EventBusType = EventBusType.AzureService,
                EventNameSuffix = "IntegrationEvent",
                EventBusConnectionString = "azure endpoint"
                //Connection = new ConnectionFactory()
                //{
                //    HostName = "localhost",
                //    Port  = 15672,
                //    UserName = "guest",
                //    Password = "guest"
                //}

            };
        }

      
    }
}