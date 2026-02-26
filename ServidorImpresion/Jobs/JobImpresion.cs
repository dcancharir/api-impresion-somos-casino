using System.Collections.Concurrent;
using System.Drawing.Printing;
using ESC_POS_USB_NET.Printer;
using Microsoft.EntityFrameworkCore;
using Quartz;
using ServidorImpresion.Context;
using ServidorImpresion.Enums;
using ServidorImpresion.Models;

namespace ServidorImpresion.Jobs
{
    [DisallowConcurrentExecution]
    public class JobImpresion : IJob
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JobImpresion> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _impresorasLocks = new();

        // Valores de configuración
        private readonly int _delayImpresion = 1000;
        private readonly bool _verificarImpresorasInstaladas = false;

        public JobImpresion(
            ApplicationDbContext context,
            ILogger<JobImpresion> logger,
            IConfiguration configuration,
            IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _serviceScopeFactory = serviceScopeFactory;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Iniciando job de impresión de cupones");

            try
            {
                // Obtener cupones pendientes de imprimir
                var cuponesParaImpresion = await _context.ImpresionCupons
                    .Where(x => x.Impreso == (int)EstadoImpresionCupon.PENDIENTE)
                    .ToListAsync();

                if (!cuponesParaImpresion.Any())
                {
                    _logger.LogInformation("No hay cupones para imprimir");
                    return;
                }

                _logger.LogInformation($"Se encontraron {cuponesParaImpresion.Count} cupones para imprimir");

                // Agrupar cupones por impresora para procesamiento más eficiente
                var cuponesPorImpresora = cuponesParaImpresion
                    .GroupBy(c => c.NombreImpresora??string.Empty)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var tareas = new List<Task<(List<ImpresionCupon> impresos, List<ImpresionCupon> fallidos)>>();

                // Procesar cada grupo de impresora en paralelo
                foreach (var grupo in cuponesPorImpresora)
                {
                    var nombreImpresora = grupo.Key;
                    var cupones = grupo.Value;

                    if (string.IsNullOrEmpty(nombreImpresora))
                    {
                        _logger.LogWarning("Se encontraron cupones sin nombre de impresora asignado");
                        continue;
                    }

                    // Inicializar semáforo para esta impresora si no existe
                    var semaforo = _impresorasLocks.GetOrAdd(nombreImpresora, _ => new SemaphoreSlim(1, 1));

                    // Agregar tarea para procesar este grupo de cupones
                    tareas.Add(ProcesarCuponesPorImpresora(nombreImpresora, cupones, semaforo));
                }

                // Esperar a que todas las tareas terminen
                var resultados = await Task.WhenAll(tareas);

                // Consolidar resultados
                var cuponesImpresos = resultados.SelectMany(r => r.impresos).ToList();
                var cuponesFallidos = resultados.SelectMany(r => r.fallidos).ToList();

                _logger.LogInformation($"Se imprimieron y actualizaron {cuponesImpresos.Count} cupones en total");

                if (cuponesFallidos.Any())
                {
                    _logger.LogWarning($"No se pudieron imprimir {cuponesFallidos.Count} cupones");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general en el job de impresión de cupones");
            }

            _logger.LogInformation("Finalización del job de impresión de cupones");
        }
        private async Task<(List<ImpresionCupon> impresos, List<ImpresionCupon> fallidos)>
        ProcesarCuponesPorImpresora(string nombreImpresora, List<ImpresionCupon> cupones, SemaphoreSlim semaforo)
        {
            var impresos = new List<ImpresionCupon>();
            var fallidos = new List<ImpresionCupon>();

            try
            {
                // Verificar si la impresora existe en el sistema
                bool impresoraExiste = true;

                if (_verificarImpresorasInstaladas)
                {
                    impresoraExiste = PrinterSettings.InstalledPrinters.Cast<string>()
                        .Any(printer => printer.Equals(nombreImpresora, StringComparison.OrdinalIgnoreCase));
                }

                if (!impresoraExiste)
                {
                    _logger.LogWarning($"La impresora '{nombreImpresora}' no está instalada en el sistema");
                    fallidos.AddRange(cupones);
                    return (impresos, fallidos);
                }

                // Procesar los cupones secuencialmente para esta impresora
                foreach (var cupon in cupones)
                {
                    try
                    {
                        // Adquirir el semáforo para esta impresora
                        await semaforo.WaitAsync();

                        try
                        {
                            //cupon.FechaImpresion = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
                            var(statusImpresion,fechaImpresion) = await ImprimirCupon(cupon);
                            if (statusImpresion)
                            {
                                // Utilizar un DbContext separado para actualizar este cupón
                                using (var scope = _serviceScopeFactory.CreateScope())
                                {
                                    cupon.FechaImpresion = fechaImpresion.ToString("dd-MM-yyyy HH:mm:ss");
                                    var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                                    await ActualizarCuponImpreso(cupon, scopedContext);
                                }

                                impresos.Add(cupon);
                                _logger.LogDebug($"Cupón {cupon.ImpresionCuponId} impreso y actualizado en BD correctamente");
                            }
                            else
                            {
                                fallidos.Add(cupon);
                                _logger.LogWarning($"No se pudo imprimir el cupón {cupon.ImpresionCuponId}");
                            }
                        }
                        finally
                        {
                            // Liberar el semáforo
                            semaforo.Release();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error al procesar el cupón {cupon.ImpresionCuponId}");
                        fallidos.Add(cupon);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error general al procesar los cupones para la impresora '{nombreImpresora}'");
                fallidos.AddRange(cupones.Except(impresos));
            }

            return (impresos, fallidos);
        }
        // Método para actualizar un cupón impreso en la base de datos
        private async Task ActualizarCuponImpreso(ImpresionCupon cupon, ApplicationDbContext context)
        {
            try
            {
                // Buscar el cupón en la base de datos usando un contexto limpio
                var cuponDb = await context.ImpresionCupons.FindAsync(cupon.ImpresionCuponId);

                if (cuponDb != null)
                {
                    // Actualizar propiedades
                    cuponDb.Impreso = (int)EstadoImpresionCupon.IMPRESO;
                    cuponDb.FechaImpresion = cupon.FechaImpresion;
                    //cuponDb.FechaImpresion = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    // Guardar cambios sin iniciar una transacción explícita
                    // (EF Core ya usa una transacción implícita para SaveChanges)
                    await context.SaveChangesAsync();

                    _logger.LogDebug($"Actualizado cupón {cupon.ImpresionCuponId} en la base de datos");
                }
                else
                {
                    _logger.LogWarning($"Cupón {cupon.ImpresionCuponId} no encontrado en la base de datos");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el cupón {cupon.ImpresionCuponId} en la base de datos");
                // No relanzamos la excepción para no interrumpir el flujo de impresión
            }
        }
        private async Task<(bool statusImpresion, DateTime fechaImpresion)> ImprimirCupon(ImpresionCupon cupon)
        {
            bool statusImpresion = false;
            DateTime fechaImpresion = DateTime.Now;

            // Esperar el tiempo configurado
            await Task.Delay(_delayImpresion);
            return (statusImpresion = true, fechaImpresion);
            try
            {
                // Instanciar la impresora y configurar el documento
                Printer printer = new(cupon.NombreImpresora);
                printer.Clear();
                printer.InitializePrint();
                printer.AlignCenter();
                printer.BoldMode($"{cupon.NombreSorteo}");
                printer.Separator(' ');
                printer.AlignCenter();
                printer.Append($"{cupon.NombreSala}");
                printer.Separator(' ');
                printer.Separator(' ');
                printer.AlignLeft();
                printer.Append($"Fecha Registro : {cupon.FechaRegistro}");
                printer.Separator(' ');
                printer.Append($"Fecha Impresa : {fechaImpresion.ToString("dd-MM-yyyy HH:mm:ss")}");
                printer.Separator(' ');
                printer.Append($"Serie : {cupon.Serie}");
                printer.Separator(' ');
                printer.Append($"Slot : {cupon.CodMaquina}");
                printer.Separator(' ');
                printer.Append($"Cliente : {cupon.NombreCliente ?? "No especificado"}");
                printer.Separator(' ');
                printer.Append($"Nro. Doc. : {cupon.NroDocumento ?? "No especificado"}");
                printer.Separator(' ');
                printer.AlignRight();
                printer.Append($"{cupon.SerieId}");
                printer.AlignRight();
                printer.PartialPaperCut();
                printer.PrintDocument();
                printer.Clear();
                statusImpresion = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al imprimir cupón {cupon.ImpresionCuponId} en impresora '{cupon.NombreImpresora}' - {ex.Message}");
            }
            // Esperar el tiempo configurado
            await Task.Delay(_delayImpresion);
            return (statusImpresion,fechaImpresion);
        }
    }
}
