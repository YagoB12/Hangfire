using Microsoft.AspNetCore.Mvc;
using PdfServer.Models;
using PdfServer.Services;

namespace PdfServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private readonly IPdfReportService _pdfService;
    private readonly ILogger<PdfController> _logger;

    public PdfController(IPdfReportService pdfService, ILogger<PdfController> logger)
    {
        _pdfService = pdfService;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] ReportRequestDto req, CancellationToken ct)
    {
        if (req.CustomerId <= 0)
            return BadRequest("CustomerId inválido.");

        _logger.LogInformation("Solicitud recibida para generar PDF | Cliente={CustomerId} | CID={CID}", req.CustomerId, req.CorrelationId);

        var result = await _pdfService.GenerateCustomerSalesReportAsync(req, ct);

        return Ok(result.FilePath);
    }
}
