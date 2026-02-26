using ServidorImpresion.Enums;

namespace ServidorImpresion.ViewModels
{
    public class CuponEnvioViewModel
    {
        public long SerieId { get; set; }
        public TipoCupon Tipo { get; set; }
        public string FechaImpresion { get; set; } = DateTime.Now.ToString();
    }
}
