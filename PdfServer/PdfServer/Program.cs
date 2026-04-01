using Microsoft.EntityFrameworkCore;
using PdfServer.Data;
using PdfServer.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

// === Controllers y Swagger ===
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// === EF Core ===
var awCs = builder.Configuration.GetConnectionString("AdventureWorks")
    ?? throw new InvalidOperationException("Falta la cadena 'AdventureWorks' en appsettings.json");

builder.Services.AddDbContext<AdventureWorksContext>(opt =>
    opt.UseSqlServer(awCs));

// === DI ===
builder.Services.AddScoped<IPdfReportService, PdfReportService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapControllers();
app.MapGet("/", () => Results.Ok("PDF Server listo y ejecutándose"));

app.Run();
