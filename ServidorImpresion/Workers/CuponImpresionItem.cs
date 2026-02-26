namespace ServidorImpresion.Workers
{
    public sealed record CuponImpresionItem(
     string FechaRegistro,
     string NombreCliente,
     string NombreImpresora,
     string NombreSala,
     string NombreSorteo,
     string NroDocumento,
     string Serie,
     long SerieId,
     int Tipo
    );
}
