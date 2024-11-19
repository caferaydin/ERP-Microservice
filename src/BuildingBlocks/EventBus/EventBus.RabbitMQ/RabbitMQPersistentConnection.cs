using Microsoft.EntityFrameworkCore.Metadata;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace EventBus.RabbitMQ
{
    public class RabbitMQPersistentConnection : IDisposable
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly int _retryCount;
        private IConnection _connection;
        //private object _lockObject = new object();
        private bool _disposed;

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public RabbitMQPersistentConnection(IConnectionFactory connectionFactory, int retryCount = 3)
        {
            _connectionFactory = connectionFactory;
            _retryCount = retryCount;
        }

        public bool IsConnected => _connection != null && _connection.IsOpen;


        public async Task<IChannel> CreateChannelAsync()
        {
            if (!IsConnected)
            {
                Console.WriteLine("No RabbitMQ connection available. Attempting to reconnect...");
                if (!await TryConnectAsync())
                {
                    throw new InvalidOperationException("RabbitMQ connections could not be created.");
                }
            }

            // Asynchronous channel creation, returning IChannel
            return await _connection!.CreateChannelAsync();
        }

        public async Task<bool> TryConnectAsync()
        {
            await _semaphoreSlim.WaitAsync(); // Asenkron kilitleme
            try
            {
                var policy = Policy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetryAsync(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        Console.WriteLine($"RabbitMQ connection failed. Retrying in {time.TotalSeconds:n1} seconds...");
                    });

                return await policy.ExecuteAsync(async () =>
                {
                    _connection?.Dispose();
                    _connection = await _connectionFactory.CreateConnectionAsync();
                    if (IsConnected)
                    {
                        Console.WriteLine("RabbitMQ connection established successfully.");
                        return true;
                    }
                    Console.WriteLine("Failed to establish RabbitMQ connection.");
                    return false;
                });
            }
            finally
            {
                _semaphoreSlim.Release(); // Kilidi serbest bırakma
            }
        }

       
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _connection?.Dispose();
            _semaphoreSlim?.Dispose();
        }

        //public async ValueTask DisposeAsync()
        //{
        //    if (_disposed) return;

        //    _disposed = true;
        //    try
        //    {
        //        _connection?.Dispose();
        //        _semaphoreSlim?.Dispose();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Exception during dispose: {ex.Message}");
        //    }
        //}
    }
}
