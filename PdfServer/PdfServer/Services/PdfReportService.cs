using Microsoft.EntityFrameworkCore;
using PdfServer.Data;
using PdfServer.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Net.Http.Json;

namespace PdfServer.Services;

public interface IPdfReportService
{
    Task<ReportResponseDto> GenerateCustomerSalesReportAsync(ReportRequestDto req, CancellationToken ct = default);
}

public sealed class PdfReportService : IPdfReportService
{
    private readonly AdventureWorksContext _db;
    private readonly ILogger<PdfReportService> _logger;
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;

    public PdfReportService(AdventureWorksContext db, ILogger<PdfReportService> logger, IConfiguration cfg)
    {
        _db = db;
        _logger = logger;
        _cfg = cfg;
        _http = new HttpClient();
    }

    public async Task<ReportResponseDto> GenerateCustomerSalesReportAsync(ReportRequestDto req, CancellationToken ct = default)
    {
        var cid = string.IsNullOrWhiteSpace(req.CorrelationId)
            ? Guid.NewGuid().ToString()
            : req.CorrelationId;

        // Datos
        var q = _db.SalesOrderHeaders.AsNoTracking().Where(h => h.CustomerID == req.CustomerId);
        if (req.From.HasValue) q = q.Where(h => h.OrderDate >= req.From.Value);
        if (req.To.HasValue) q = q.Where(h => h.OrderDate <= req.To.Value);

        var headers = await q
            .OrderBy(h => h.OrderDate)
            .Select(h => new
            {
                h.SalesOrderID,
                h.OrderDate,
                h.TotalDue,
                Details = _db.SalesOrderDetails.AsNoTracking()
                    .Where(d => d.SalesOrderID == h.SalesOrderID)
                    .Select(d => new { d.ProductID, d.OrderQty, d.UnitPrice })
                    .ToList()
            })
            .ToListAsync(ct);

        // Rutas
        var folder = Path.Combine("wwwroot", "reports", DateTime.Now.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(folder);

        var fileName = $"ReporteCliente_{req.CustomerId}_{DateTime.Now:HHmmss}_{cid}.pdf";
        var filePath = Path.Combine(folder, fileName);

        // Generación de PDF
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(36);
                page.Header().Row(row =>
                {
                    row.RelativeItem().Text($"Reporte de Ventas - Cliente {req.CustomerId}").SemiBold().FontSize(16);
                    row.ConstantItem(160).AlignRight().Text(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                });

                page.Content().Column(col =>
                {
                    if (!headers.Any())
                    {
                        col.Item().Text("No hay pedidos para los filtros.").Italic();
                        return;
                    }

                    foreach (var h in headers)
                    {
                        col.Item().Text($"Pedido #{h.SalesOrderID}  Fecha: {h.OrderDate:yyyy-MM-dd}  Total: {h.TotalDue:C}").SemiBold();
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cc =>
                            {
                                cc.ConstantColumn(90);
                                cc.ConstantColumn(70);
                                cc.ConstantColumn(90);
                                cc.RelativeColumn();
                            });

                            t.Header(hd =>
                            {
                                hd.Cell().Text("Producto").SemiBold();
                                hd.Cell().Text("Cant.").SemiBold();
                                hd.Cell().Text("Precio").SemiBold();
                                hd.Cell().Text("Subtotal").SemiBold();
                            });

                            foreach (var d in h.Details)
                            {
                                var subtotal = d.UnitPrice * d.OrderQty;
                                t.Cell().Text(d.ProductID.ToString());
                                t.Cell().Text(d.OrderQty.ToString());
                                t.Cell().Text($"{d.UnitPrice:C}");
                                t.Cell().Text($"{subtotal:C}");
                            }
                        });

                        col.Item().LineHorizontal(0.6f);
                    }
                });

                page.Footer().AlignCenter().Text($"CorrelationId: {cid}");
            });
        }).GeneratePdf(filePath);

        _logger.LogInformation("PDF generado | Archivo={File} | ClienteId={CustomerId} | CID={CID}", fileName, req.CustomerId, cid);

        // Log al Storage
        try
        {
            var storageUrl = "http://localhost:8001/logs";
            var payload = new
            {
                correlationId = cid,
                message = $"PDF generado para Cliente {req.CustomerId}",
                file = fileName,
                createdAt = DateTime.UtcNow
            };

            var response = await _http.PostAsJsonAsync(storageUrl, payload, ct);
            _logger.LogInformation("Log enviado al StorageServer → {Status}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error enviando log al StorageServer: {Error}", ex.Message);
        }

        // Subir PDF al Storage
        try
        {
            var uploadUrl = $"http://localhost:8001/upload?correlationId={cid}";
            using var content = new MultipartFormDataContent();
            await using var stream = File.OpenRead(filePath);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", Path.GetFileName(filePath));

            var uploadResponse = await _http.PostAsync(uploadUrl, content, ct);
            if (uploadResponse.IsSuccessStatusCode)
                _logger.LogInformation("PDF subido exitosamente al StorageServer: {File}", fileName);
            else
                _logger.LogWarning("Falló la subida del PDF: {Status}", uploadResponse.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error al subir PDF al StorageServer: {Error}", ex.Message);
        }

        // Notificar a Hangfire para agendar notificaciones (Discord + Email)
        try
        {
            var hangfireBase = _cfg["HangfireServer:BaseUrl"] ?? "http://localhost:5000";
            var scheduleUrl = $"{hangfireBase}/api/notifications";

           
            var defaultTo = _cfg["Email:DefaultTo"] ?? "cliente@ejemplo.com";

            var schedulePayload = new
            {
                correlationId = cid,
                delaySeconds = 45,
                EmailTo = defaultTo,
                EmailSubject = $"Reporte automático • CID {cid}",
                EmailMessage = "Hola, adjunto el PDF generado.",
                EmailFilename = fileName
            };

            var notifResp = await _http.PostAsJsonAsync(scheduleUrl, schedulePayload, ct);
            if (notifResp.IsSuccessStatusCode)
                _logger.LogInformation("Notificación programada en Hangfire (Discord + Email) | CID={CID}", cid);
            else
                _logger.LogWarning("Falló agendar en Hangfire | CID={CID} → HTTP {Status}", cid, notifResp.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notificando a Hangfire | CID={CID}", cid);
        }

        return new ReportResponseDto
        {
            FilePath = filePath,
            FileName = fileName,
            CorrelationId = cid,
            GeneratedAt = DateTime.Now
        };
    }
}
