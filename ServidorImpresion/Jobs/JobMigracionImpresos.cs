using System.Text.Json.Nodes;
using System.Text.Json;
using Quartz;
using ServidorImpresion.Context;
using ServidorImpresion.Enums;
using ServidorImpresion.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace ServidorImpresion.Jobs
{
    public class JobMigracionImpresos : IJob
    {
        private string _urlSorteosGlobales = string.Empty;
        private IServiceScopeFactory _serviceScopeFactory;
        public JobMigracionImpresos(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var _logger = scope.ServiceProvider.GetRequiredService<ILogger<JobMigracionImpresos>>();
            var _applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var _configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var _httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientBuilder>();
            _urlSorteosGlobales = _configuration.GetSection("Configuracion")["UrlSorteosGlobalesAzure"] ?? string.Empty;
            string url = $"{_urlSorteosGlobales}/api/cupones/cambiarestado";
            var seriesImpresas = await _applicationDbContext.ImpresionCupons
                .Where(x => 
                    x.Impreso == (int)EstadoImpresionCupon.IMPRESO &&
                    x.Enviado == (int)EstadoEnvioCupon.NO_ENVIADO
                )
                .ToListAsync();
            if (seriesImpresas.Count != 0) { 
                var listaEnvio = seriesImpresas.Select( x => new CuponEnvioViewModel() { 
                    SerieId = x.SerieId,
                    FechaImpresion = x.FechaImpresion??DateTime.Now.ToString(),
                    Tipo = (TipoCupon)x.Tipo,
                });
                bool resultEnvio = await EnvioHttp(listaEnvio, url, _logger,_httpClient);
                if (resultEnvio)
                {
                    seriesImpresas.ForEach(x=>
                        x.Enviado = (int)EstadoEnvioCupon.ENVIADO
                    );
                    _applicationDbContext.UpdateRange(seriesImpresas);
                    _applicationDbContext.SaveChanges();
                }
            }
         
        }
        internal async Task<bool> EnvioHttp(
            object oEnvio,
            string url,
            ILogger<JobMigracionImpresos> logger)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var _httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var httpClient = _httpClient.CreateClient();
                using var response = await httpClient.PostAsJsonAsync(url, oEnvio);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Error HTTP: {StatusCode}", response.StatusCode);
                    return false;
                }

                return await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en EnvioHttp");
                return false;
            }
        }
    }
}
