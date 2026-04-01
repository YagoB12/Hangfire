namespace PdfServer.Models;

public sealed class ReportRequestDto
{
    public int CustomerId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? CorrelationId { get; set; }
}
