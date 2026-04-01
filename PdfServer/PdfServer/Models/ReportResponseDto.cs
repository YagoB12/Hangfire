namespace PdfServer.Models;

public sealed class ReportResponseDto
{
    public string FileName { get; set; } = default!;
    public string FilePath { get; set; } = default!;
    public string? PublicUrl { get; set; }
    public string CorrelationId { get; set; } = default!;
    public DateTime GeneratedAt { get; set; }
}
