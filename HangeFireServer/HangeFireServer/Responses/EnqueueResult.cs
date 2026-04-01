namespace HangFireServer.Responses;

public sealed class EnqueueResult
{
    public string JobId { get; init; } = default!;
    public string CorrelationId { get; init; } = default!;
    public DateTime ScheduledAtUtc { get; init; }
    public DateTime? ExpectedRunAtUtc { get; init; }
}
