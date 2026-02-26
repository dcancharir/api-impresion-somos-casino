namespace ServidorImpresion.Workers
{
    public sealed class TerminalQueueReaderWorker : BackgroundService
    {
        private readonly QueueNameBuilder _queueName;
        private readonly ServiceBusQueueReader _reader;
        private readonly IServiceScopeFactory _scopeFactory;
        public TerminalQueueReaderWorker(
            QueueNameBuilder queueName, 
            ServiceBusQueueReader reader, 
            IServiceScopeFactory scopeFactory)
        {
            _queueName = queueName;
            _reader = reader;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queue = _queueName.Build();
            Console.WriteLine($"Leyendo cola: {queue}");

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();

                var _cmd = scope.ServiceProvider.GetRequiredService<ReadTerminalQueueCommand>();
                await _cmd.ExecuteAsync(_reader, queue, stoppingToken);
                await Task.Delay(_cmd.PollMs, stoppingToken);
            }
        }
    }
}
