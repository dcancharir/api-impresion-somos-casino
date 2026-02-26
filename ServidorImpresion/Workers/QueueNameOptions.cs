namespace ServidorImpresion.Workers
{
    public class QueueNameOptions
    {
        public string QueuePrefix { get; set; } = default!;
        public int SedeId { get; set; }
        public int TerminalId { get; set; }
    }
}
