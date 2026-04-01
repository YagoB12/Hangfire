using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using System.Net.Http.Json;

namespace HangFireServer.Jobs;

public sealed class GenerateReportJob
{
    private readonly ILogger<GenerateReportJob> _logger;
    private readonly HttpClient _http;

    public GenerateReportJob(ILogger<GenerateReportJob> logger)
    {
        _logger = logger;
        _http = new HttpClient();
    }

    public async Task ExecuteAsync(int customerId, DateTime from, DateTime to, string correlationId, PerformContext? context, IJobCancellationToken token)
    {
        var con = context ?? throw new ArgumentNullException(nameof(context));
        con.WriteLine($"[START] CID={correlationId} Generando reporte remoto para cliente={customerId}");
        var p = con.WriteProgressBar();
        p.SetValue(20);

        var request = new
        {
            CustomerId = customerId,
            From = from,
            To = to,
            CorrelationId = correlationId
        };

        // URL 
        var pdfServerUrl = "https://localhost:7272/api/pdf/generate";
        var response = await _http.PostAsJsonAsync(pdfServerUrl, request, token.ShutdownToken);

        if (response.IsSuccessStatusCode)
        {
            var filePath = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Reporte generado correctamente en el PDF Server: {FilePath}", filePath);
            con.WriteLine($" PDF creado: {filePath}");
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error al generar PDF: {Error}", error);
            con.WriteLine($" Error en PDF Server: {error}");
        }

        p.SetValue(100);
        con.WriteLine("[END] Job completado.");
    }
}
