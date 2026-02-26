using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ServidorImpresion.Context;
using ServidorImpresion.Extensions;
using ServidorImpresion.Workers;

System.Text.EncodingProvider provider = System.Text.CodePagesEncodingProvider.Instance;
Encoding.RegisterProvider(provider);


var builder = WebApplication.CreateBuilder(args);
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
}));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection"))
);

// Configuración del Worker
builder.Services.Configure<WorkerOptions>(
    builder.Configuration.GetSection("Worker")
);

// QueueNameBuilder con Options
builder.Services.Configure<QueueNameOptions>(builder.Configuration.GetSection("Target"));
builder.Services.AddSingleton<QueueNameBuilder>();

// ServiceBusClient y Reader (singleton y thread-safe)
builder.Services.AddSingleton(sp =>
{
    var connectionString = builder.Configuration["ServiceBus:ConnectionString"]!;
    return new ServiceBusClient(connectionString);
});
builder.Services.AddSingleton<ServiceBusQueueReader>();

// ReadTerminalQueueCommand → Scoped porque usa DbContext
builder.Services.AddScoped<ReadTerminalQueueCommand>();

// Worker → Singleton, pero obtendrá ReadTerminalQueueCommand vía ScopeFactory
builder.Services.AddHostedService<TerminalQueueReaderWorker>();

//Quartz, Swagger
builder.Services.AddQuartzJobs(builder.Configuration,logger); // Logger se puede inyectar desde DI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Logging con Serilog
var logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
Directory.CreateDirectory(logFolder);
var logFile = Path.Combine(logFolder, "servidorimpresion-.log");

builder.Host.UseSerilog((ctx, lc) => lc
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Error)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Error)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Error)
    .WriteTo.File(logFile, rollingInterval: RollingInterval.Day)
);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => {
    c.ConfigObject.AdditionalItems.Add("syntaxHighlight", false);
});
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseCors("MyPolicy");

app.Run();
