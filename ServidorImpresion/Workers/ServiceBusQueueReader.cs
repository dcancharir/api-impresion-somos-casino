using Azure.Messaging.ServiceBus;

namespace ServidorImpresion.Workers
{
    public sealed class ServiceBusQueueReader : IAsyncDisposable
    {
        private readonly ServiceBusClient _client;

        public ServiceBusQueueReader(ServiceBusClient client)
        {
            _client = client;
        }

        public ServiceBusReceiver CreateReceiver(string queueName)
            => _client.CreateReceiver(queueName, new ServiceBusReceiverOptions
            {
                // PeekLock permite Receive + Abandon/Complete
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            });

        public ValueTask DisposeAsync() => _client.DisposeAsync();
    }
}
