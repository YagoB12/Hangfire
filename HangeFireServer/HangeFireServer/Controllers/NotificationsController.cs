using Hangfire;
using Microsoft.AspNetCore.Mvc;
using HangFireServer.Jobs;

namespace HangFireServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class NotificationsController : ControllerBase
    {
        private readonly ILogger<NotificationsController> _logger;
        private readonly IConfiguration _cfg;

        public NotificationsController(ILogger<NotificationsController> logger, IConfiguration cfg)
        {
            _logger = logger;
            _cfg = cfg;
        }

        [HttpPost]
        public IActionResult ScheduleNotification([FromBody] NotificationRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.CorrelationId))
                return BadRequest("CorrelationId es requerido.");

            int delay = req.DelaySeconds > 0 ? req.DelaySeconds : 30;

            
            _logger.LogInformation("[Hangfire] Programando notificación Discord CID={CID} en {Delay}s",
                req.CorrelationId, delay);

            BackgroundJob.Schedule(
                () => new SendDiscordNotificationJob(null!).ExecuteAsync(req.CorrelationId, null!),
                TimeSpan.FromSeconds(delay)
            );

            var to = string.IsNullOrWhiteSpace(req.EmailTo)
                ? (_cfg["Email:DefaultTo"] ?? "cliente@ejemplo.com")
                : req.EmailTo!;

            var subject = req.EmailSubject ?? $"Reporte {req.CorrelationId}";
            var message = req.EmailMessage ?? $"Adjunto su reporte. CID={req.CorrelationId}";
            var filename = req.EmailFilename;

            _logger.LogInformation("[Hangfire] Programando email CID={CID} to={TO} en {Delay}s",
                req.CorrelationId, to, delay);

            BackgroundJob.Schedule(
                () => new SendEmailNotificationJob(null!).ExecuteAsync(
                    req.CorrelationId,
                    to,
                    subject,
                    message,
                    filename,
                    null!),
                TimeSpan.FromSeconds(delay)
            );

            return Ok(new
            {
                scheduled = true,
                correlationId = req.CorrelationId,
                delay,
                emailTo = to
            });
        }
    }

    public record NotificationRequest(
        string CorrelationId,
        int DelaySeconds,
        string? EmailTo,
        string? EmailSubject,
        string? EmailMessage,
        string? EmailFilename
    );
}
