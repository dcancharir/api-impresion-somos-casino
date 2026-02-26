using Quartz;
using ServidorImpresion.Jobs;

namespace ServidorImpresion.Extensions
{
    public static class QuartzSetupExtensions
    {
        public static void AddQuartzJobs(this IServiceCollection services, IConfiguration configuration,ILogger logger)
        {
            services.AddQuartz(q => {
                var realizarJobImpresion = configuration.GetValue<bool>("Jobs:RealizarJobImpresion");
                var realizarJobMigracionImpresos = configuration.GetValue<bool>("Jobs:RealizarJobMigracionImpresos");
                var realizarJobHistorialImpresion = configuration.GetValue<bool>("Jobs:RealizarJobHistorialImpresion");

                if (realizarJobImpresion)
                {
                    var intervaloJobImpresion = configuration.GetValue<int>("Jobs:IntervaloSegundosJobImpresion");
                    ConfigureJobImpresion(q,logger,intervaloJobImpresion);

                }
                if (realizarJobMigracionImpresos)
                {
                    var intervaloJobMigracionImpresos = configuration.GetValue<int>("Jobs:IntervaloSegundosJobMigracionImpresos");
                    ConfigureJobMigracionImpresos(q, logger, intervaloJobMigracionImpresos);
                }

                if (realizarJobHistorialImpresion)
                {
                    var intervaloJobHistorialImpresion = configuration.GetValue<int>("Jobs:IntervaloSegundosJobHistorialImpresion");
                    ConfigureJobHistorialImpresion(q, logger, intervaloJobHistorialImpresion);
                }

            });
            services.AddQuartzHostedService(options => {
                options.WaitForJobsToComplete = true;
            });
        }
        private static void ConfigureJobImpresion(IServiceCollectionQuartzConfigurator q, ILogger logger, int intervalo)
        {
            logger.LogInformation($"Configurando JobImpresion con intervalo de {intervalo} segundos.");
            JobKey key = new JobKey("JobImpresion");
            q.AddJob<JobImpresion>(job => job.WithIdentity(key));
            q.AddTrigger(trigger => trigger
                .ForJob(key)
                .WithIdentity("JobImpresion-trigger")
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(intervalo).RepeatForever().Build())
                .StartNow());
        }
        private static void ConfigureJobMigracionImpresos(IServiceCollectionQuartzConfigurator q, ILogger logger, int intervalo)
        {
            logger.LogInformation($"Configurando JobMigracionImpresos con intervalo de {intervalo} segundos.");
            JobKey key = new JobKey("JobMigracionImpresos");
            q.AddJob<JobMigracionImpresos>(job => job.WithIdentity(key));
            q.AddTrigger(trigger => trigger
                .ForJob(key)
                .WithIdentity("JobMigracionImpresos-trigger")
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(intervalo).RepeatForever().Build())
                .StartNow());
        }
        private static void ConfigureJobHistorialImpresion(IServiceCollectionQuartzConfigurator q, ILogger logger, int intervalo)
        {
            logger.LogInformation($"Configurando JobHistorialImpresion con intervalo de {intervalo} segundos.");
            JobKey key = new JobKey("JobHistorialImpresion");
            q.AddJob<JobHistorialImpresion>(job => job.WithIdentity(key));
            q.AddTrigger(trigger => trigger
                .ForJob(key)
                .WithIdentity("JobHistorialImpresion-trigger")
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(intervalo).RepeatForever().Build())
                .StartNow());
        }
    }
}
