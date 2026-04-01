using Hangfire;
using Hangfire.Console;
using Hangfire.SqlServer;
using HangFireServer.Jobs;
using HangFireServer.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// ===== Controllers y Swagger =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ===== Hangfire Config =====
var hangfireCs = builder.Configuration.GetConnectionString("HangfireDb")
    ?? throw new InvalidOperationException("Falta la cadena 'HangfireDb' en appsettings.json");

builder.Services.AddHangfire(cfg =>
{
    cfg.UseSimpleAssemblyNameTypeSerializer()
       .UseRecommendedSerializerSettings()
       .UseConsole()
       .UseSqlServerStorage(hangfireCs, new SqlServerStorageOptions
       {
           PrepareSchemaIfNecessary = true,
           QueuePollInterval = TimeSpan.FromSeconds(10)
       });
});

builder.Services.AddHangfireServer();

// ===== Jobs =====
builder.Services.AddTransient<GenerateReportJob>();
builder.Services.AddTransient<SendEmailNotificationJob>();

// builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseHangfireDashboard("/hangfire");
app.MapControllers();

app.MapGet("/", () => Results.Ok("Hangfire Server en ejecución"));

app.Run();
