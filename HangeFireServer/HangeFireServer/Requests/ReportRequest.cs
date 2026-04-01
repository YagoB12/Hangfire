namespace HangFireServer.Requests;

public sealed class ReportRequest
{
    public int CustomerId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int DelaySeconds { get; set; } = 60; // segundos de retraso
}
