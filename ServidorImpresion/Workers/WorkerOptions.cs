namespace ServidorImpresion.Workers
{
    public class WorkerOptions
    {
        public int MaxMessages { get; set; }
        public string ReadMode { get; set; } = "Peek";
        public bool Complete { get; set; }
        public int PollMs { get; set; }
    }
}
