using Microsoft.EntityFrameworkCore;
using Quartz;
using ServidorImpresion.Context;
using ServidorImpresion.Enums;
using ServidorImpresion.Models;

namespace ServidorImpresion.Jobs
{
    [DisallowConcurrentExecution]
    public class JobHistorialImpresion : IJob
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JobHistorialImpresion> _logger;
        public JobHistorialImpresion(ApplicationDbContext context, ILogger<JobHistorialImpresion> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            var listaInsertar = new List<HistorialImpresionCupon>();
            try
            {
                var cupones = await _context.ImpresionCupons.Where(x => x.Impreso == (int)EstadoImpresionCupon.IMPRESO && x.Enviado == (int)EstadoEnvioCupon.ENVIADO).ToListAsync();
                foreach (var item in cupones)
                {
                    var historialImpresion = new HistorialImpresionCupon();
                    historialImpresion.ImpresionCuponId = item.ImpresionCuponId;
                    historialImpresion.NombreCliente = item.NombreCliente;
                    historialImpresion.NombreImpresora = item.NombreImpresora;
                    historialImpresion.NombreSorteo = item.NombreSorteo;
                    historialImpresion.NombreSala = item.NombreSala;
                    historialImpresion.FechaRegistro = item.FechaRegistro;
                    historialImpresion.FechaImpresion = item.FechaImpresion;
                    historialImpresion.Serie = item.Serie;
                    historialImpresion.CodMaquina = item.CodMaquina;
                    historialImpresion.NroDocumento = item.NroDocumento;
                    historialImpresion.SerieId = item.SerieId;
                    historialImpresion.Impreso = item.Impreso;
                    historialImpresion.Enviado = item.Enviado;
                    historialImpresion.Tipo = item.Tipo;
                    listaInsertar.Add(historialImpresion);
                }

                await _context.AddRangeAsync(listaInsertar);
                var saved = await _context.SaveChangesAsync();
                if (saved > 0)
                {
                    _context.ImpresionCupons.RemoveRange(cupones);
                    await _context.SaveChangesAsync();
                }

                _logger.LogDebug($"Cupones Migrados a historial");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error metodo JobHistorialImpresion - {ex.Message}");
                throw;
            }
        }
    }
}
