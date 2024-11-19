using EventBus.Base;
using EventBus.Base.Events;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.Text;

namespace EventBus.RabbitMQ
{
    public class EventBusRabbitMQ : BaseEventBus
    {
        RabbitMQPersistentConnection _persistentConnection;
        private readonly IConnectionFactory _connectionFactory;
        private readonly IChannel consumerChanel;

        public EventBusRabbitMQ(IServiceProvider serviceProvider, EventBusConfiguration configuration) : base(serviceProvider, configuration)
        {
            if (configuration.Connection != null)
            {
                var confJson = JsonConvert.SerializeObject(configuration.Connection, new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                });

                _connectionFactory = JsonConvert.DeserializeObject<ConnectionFactory>(confJson);
            }
            else
                _connectionFactory = new ConnectionFactory();

            _persistentConnection = new RabbitMQPersistentConnection(_connectionFactory, configuration.ConnectionRetryCount);
            consumerChanel = CreateConsumerChannel();

            _subscriptionManager.OnEventRemoved += _subscriptionManager_OnEventRemoved;
        }

        private void _subscriptionManager_OnEventRemoved(object? sender, string eventName)
        {
            eventName = ProcessEventName(eventName);

            if(!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnectAsync().GetAwaiter().GetResult();
            }

            consumerChanel.QueueUnbindAsync(
                queue: eventName,
                exchange: _configuration.DefaultTopicName,
                routingKey: eventName
                );

            if (_subscriptionManager.IsEmpty)
            {
                consumerChanel.CloseAsync().GetAwaiter().GetResult();
            }
        }

        public override void Publish(IntegrationEvent @event)
        {
            if(!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnectAsync().GetAwaiter().GetResult();
            }

            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_configuration.ConnectionRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                { }
                );

            var eventName = @event.GetType().Name;
            eventName = ProcessEventName(eventName);

            consumerChanel.ExchangeDeclareAsync(
                exchange: _configuration.DefaultTopicName,
                type: "direct"
                );

            var message = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(message);

            policy.Execute(() => {
                //var properties = consumerChanel.CreateBasicProperties();
                //properties.DeliveryMode = 2; // persistent

                var properties = new BasicProperties()
                {
                    DeliveryMode = DeliveryModes.Persistent, // persistent message
                };


                consumerChanel.QueueDeclareAsync( // Ensure q- re-publish
                    queue: GetSubName(eventName),
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null).GetAwaiter().GetResult();

                consumerChanel.BasicPublishAsync(
                    exchange: _configuration.DefaultTopicName,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body
                ).GetAwaiter().GetResult();
            });

            
        }

        public override void Subscribe<T, TH>()
        {
            var eventName = typeof(T).Name;
            eventName = ProcessEventName(eventName);

            if (!_subscriptionManager.HasSubscriptionsForEvent(eventName))
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnectAsync().GetAwaiter().GetResult();
                }

                consumerChanel.QueueDeclareAsync(queue: GetSubName(eventName),
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null).GetAwaiter().GetResult();

                consumerChanel.QueueBindAsync(
                    queue: GetSubName(eventName),
                    exchange: _configuration.DefaultTopicName,
                    routingKey: eventName).GetAwaiter().GetResult();
            }

            _subscriptionManager.AddSubscription<T, TH>();
            StartBasicConsume(eventName);
        }

        public override void UnSubscribe<T, TH>()
        {
          _subscriptionManager.RemoveSubscription<T, TH>();
        }

        private IChannel CreateConsumerChannel()
        {
            if (_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnectAsync().GetAwaiter().GetResult();
            }
            var channel = _persistentConnection.CreateChannelAsync().GetAwaiter().GetResult();

            channel.ExchangeDeclareAsync(exchange: _configuration.DefaultTopicName, type: "direct").GetAwaiter().GetResult();

            return channel;
        }


        private void StartBasicConsume(string eventName)
        {
            if(consumerChanel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(consumerChanel);

                consumer.ReceivedAsync += Consumer_ReceivedAsync;

                consumerChanel.BasicConsumeAsync(
                    queue: GetSubName(eventName),
                    autoAck: false,
                    consumer: consumer
                    );
            }
        }

        private async Task Consumer_ReceivedAsync(object sender, BasicDeliverEventArgs @event)
        {
            var eventName = @event.RoutingKey;
            eventName = ProcessEventName(eventName);

            var message = Encoding.UTF8.GetString(@event.Body.Span);

            try
            {
                await ProcessEvent(eventName, message);
            }
            catch (Exception)
            {

            }
            
            consumerChanel.BasicAckAsync(@event.DeliveryTag, multiple: false).GetAwaiter().GetResult();

        }

    }
}
