using Hangfire.Console;
using Hangfire.Server;
using System.Net.Http.Json;

namespace HangFireServer.Jobs;

public sealed class SendEmailNotificationJob
{
    private readonly ILogger<SendEmailNotificationJob> _logger;
    private readonly HttpClient _http;

    public SendEmailNotificationJob(ILogger<SendEmailNotificationJob> logger)
    {
        _logger = logger;
        _http = new HttpClient();
    }

    // Usamos parámetros primitivos para evitar problemas de serialización
    public async Task ExecuteAsync(
        string correlationId,
        string to,
        string? subject,
        string? message,
        string? filename,
        PerformContext context)
    {
        context.WriteLine($"[START] Enviando email CID={correlationId} → {to}");
        var p = context.WriteProgressBar();
        p.SetValue(25);

        try
        {
            var emailServerUrl = "http://localhost:8002/api/email/send";

            var payload = new
            {
                correlationId = correlationId,
                to = to,
                subject = subject,
                message = message,
                filename = filename
            };

            var response = await _http.PostAsJsonAsync(emailServerUrl, payload);
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(" Email enviado CID={CID} to={TO}", correlationId, to);
                context.WriteLine(" Email enviado correctamente.");
            }
            else
            {
                _logger.LogWarning(" Error enviando email CID={CID} → {Status} Body={Body}", correlationId, response.StatusCode, body);
                context.WriteLine($" Error HTTP {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Excepción en SendEmailNotificationJob CID={CID}", correlationId);
            context.WriteLine($" Excepción: {ex.Message}");
        }

        p.SetValue(100);
        context.WriteLine("[END] Job completado.");
    }
}
