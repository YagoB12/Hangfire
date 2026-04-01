using Hangfire;
using Hangfire.Server;
using Microsoft.AspNetCore.Mvc;
using HangFireServer.Jobs;
using HangFireServer.Middlewares;
using HangFireServer.Requests;
using HangFireServer.Responses;

namespace HangFireServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ReportsController : ControllerBase
{
    private readonly IBackgroundJobClient _jobs;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IBackgroundJobClient jobs, ILogger<ReportsController> logger)
    {
        _jobs = jobs;
        _logger = logger;
    }

    [HttpPost]
    public ActionResult<EnqueueResult> Enqueue([FromBody] ReportRequest req)
    {
        if (req.CustomerId <= 0)
            return BadRequest("CustomerId inválido.");

        if (req.From == default || req.To == default)
            return BadRequest("Fechas requeridas.");

        if (req.From.Date > req.To.Date)
            return BadRequest("'From' no puede ser mayor que 'To'.");

        var cid = (HttpContext.Items[CorrelationIdMiddleware.HeaderName] as string)
                  ?? Guid.NewGuid().ToString();

        var delay = TimeSpan.FromSeconds(req.DelaySeconds <= 0 ? 60 : req.DelaySeconds);

        _logger.LogInformation(
            "[Solicitud Recibida] CID={CID} | Customer={CustomerId} | From={From} | To={To} | Delay={Delay}s | Timestamp={Time}",
            cid, req.CustomerId, req.From, req.To, req.DelaySeconds, DateTime.UtcNow);

        string jobId = BackgroundJob.Schedule(
            () => new GenerateReportJob(null!).ExecuteAsync(
                req.CustomerId, req.From, req.To, cid, null, JobCancellationToken.Null),
            delay
        );

       // int delayForNotification = req.DelaySeconds + 45; // segundos extra
        //BackgroundJob.Schedule(
         //   () => new SendDiscordNotificationJob(null!).ExecuteAsync(cid, null!),
          //  TimeSpan.FromSeconds(delayForNotification)
        //);

        _logger.LogInformation("[Hangfire] Tarea programada JobId={JobId} CorrelationId={CID}", jobId, cid);

        return Ok(new EnqueueResult
        {
            JobId = jobId,
            CorrelationId = cid,
            ScheduledAtUtc = DateTime.UtcNow,
            ExpectedRunAtUtc = DateTime.UtcNow.Add(delay)
        });
    }
}
