using Hangfire.Console;
using Hangfire.Server;
using System.Net.Http.Json;

namespace HangFireServer.Jobs;

public sealed class SendDiscordNotificationJob
{
    private readonly ILogger<SendDiscordNotificationJob> _logger;
    private readonly HttpClient _http;

    public SendDiscordNotificationJob(ILogger<SendDiscordNotificationJob> logger)
    {
        _logger = logger;
        _http = new HttpClient();
    }

    public async Task ExecuteAsync(string correlationId, PerformContext context)
    {
        context.WriteLine($"[START] Enviando notificación a Discord CID={correlationId}");
        var p = context.WriteProgressBar();
        p.SetValue(25);

        try
        {
            var messageServerUrl = "http://localhost:8004/api/messaging/send";
            var payload = new
            {
                correlationId = correlationId,
                platform = "discord",
                recipient = "default",
                message = $" Nuevo reporte disponible (CID: {correlationId})"
            };

            var response = await _http.PostAsJsonAsync(messageServerUrl, payload);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(" Notificación enviada a MessageServer CID={CID}", correlationId);
                context.WriteLine(" Notificación enviada correctamente.");
            }
            else
            {
                _logger.LogWarning(" Error al enviar notificación CID={CID} → {Status}", correlationId, response.StatusCode);
                context.WriteLine($" Error HTTP {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(" Excepción en SendDiscordNotificationJob: {Error}", ex.Message);
            context.WriteLine($" Excepción: {ex.Message}");
        }

        p.SetValue(100);
        context.WriteLine("[END] Job completado.");
    }
}
