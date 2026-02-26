namespace ServidorImpresion.Models
{
    public class ImpresionCupon
    {
        public int ImpresionCuponId { get; set; }
        public string? NombreCliente { get; set; }
        public string? NombreImpresora { get; set; }
        public string? NombreSorteo { get; set; }
        public string? NombreSala { get; set; }
        public string? FechaRegistro { get; set; }
        public string? FechaImpresion { get; set; }
        public string? Serie { get; set; }
        public string? CodMaquina { get; set; }
        public string? NroDocumento { get; set; }
        public long SerieId { get; set; }
        public int Impreso { get; set; }
        public int Enviado { get; set; }
        public int Tipo { get; set; }
    }
}
