using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using ServidorImpresion.Context;
using ServidorImpresion.Enums;
using ServidorImpresion.Models;

namespace ServidorImpresion.Workers
{
    public sealed class ReadTerminalQueueCommand
    {
        private readonly int _maxMessages;
        private readonly string _readMode;
        private readonly bool _complete;
        private readonly int _pollMs;
        private readonly ILogger<ReadTerminalQueueCommand> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public ReadTerminalQueueCommand(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ReadTerminalQueueCommand> logger,
            IOptions<WorkerOptions> options)
        {
            var opt = options.Value;

            _maxMessages = opt.MaxMessages;
            _readMode = opt.ReadMode.Trim();
            _complete = opt.Complete;
            _pollMs = opt.PollMs;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }


        public int PollMs => _pollMs;

        public async Task ExecuteAsync(ServiceBusQueueReader reader, string queueName, CancellationToken ct)
        {
            await using var receiver = reader.CreateReceiver(queueName);

            if (string.Equals(_readMode, "Receive", StringComparison.OrdinalIgnoreCase))
            {
                await ReceiveAsync(receiver, ct);
                return;
            }

        }

        private async Task ReceiveAsync(ServiceBusReceiver receiver, CancellationToken ct)
        {
            var messages = await receiver.ReceiveMessagesAsync(
                maxMessages: _maxMessages,
                maxWaitTime: TimeSpan.FromSeconds(2),
                cancellationToken: ct);

            if (messages.Count == 0)
            {
                _logger.LogInformation("No llegaron mensajes");
                return;
            }

            _logger.LogInformation("Recibidos {Count} mensajes", messages.Count);

            await Task.WhenAll(messages.Select(async msg =>
            {
                bool messageSaved = await SaveMessage(msg);

                if (messageSaved)
                    await receiver.CompleteMessageAsync(msg, ct);
                else
                    await receiver.AbandonMessageAsync(msg, cancellationToken : ct);
            }));
            //foreach (var msg in messages)
            //{
            //    bool messageSaved = await SaveMessage(msg);

            //    if (messageSaved)
            //        await receiver.CompleteMessageAsync(msg, ct);
            //    else
            //        await receiver.AbandonMessageAsync(msg, cancellationToken: ct);
            //}
        }
        private async Task<bool> SaveMessage(ServiceBusReceivedMessage msg)
        {
            var body = msg.Body.ToString();
            int inserted = 0;
            try
            {
                var list = JsonSerializer.Deserialize<List<CuponImpresionItem>>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (list == null || list.Count == 0)
                    return false;
                
                using var scope = _serviceScopeFactory.CreateScope();
                var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var impresionCuponsBD = list.Select(x => new ImpresionCupon()
                {
                    NombreCliente = x.NombreCliente,
                    NombreImpresora = x.NombreImpresora,
                    NombreSorteo = x.NombreSorteo,
                    NombreSala = x.NombreSala,
                    FechaRegistro = x.FechaRegistro,
                    Serie = x.Serie,
                    NroDocumento = x.NroDocumento,
                    SerieId = x.SerieId,
                    Tipo = x.Tipo,
                });
                await _dbContext.AddRangeAsync(impresionCuponsBD);
                inserted = await _dbContext.SaveChangesAsync();
                return inserted > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"SaveMessage - error : {ex.Message}");
                return false;
            }
        } 
     
    }
}
