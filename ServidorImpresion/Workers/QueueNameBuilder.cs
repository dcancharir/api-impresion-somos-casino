using Microsoft.Extensions.Options;

namespace ServidorImpresion.Workers
{
    public sealed class QueueNameBuilder
    {
        private readonly string _prefix;
        private readonly int _sedeId;
        private readonly int _terminalId;

        public QueueNameBuilder(IOptions<QueueNameOptions> options)
        {
            var opt = options.Value;
            _prefix = opt.QueuePrefix;
            _sedeId = opt.SedeId;
            _terminalId = opt.TerminalId;
        }

        // Igual que en tu worker: {prefix}.s{IdSede}.t{IdTerminal}
        public string Build() => $"{_prefix}.s{_sedeId}.t{_terminalId}";
    }
}
