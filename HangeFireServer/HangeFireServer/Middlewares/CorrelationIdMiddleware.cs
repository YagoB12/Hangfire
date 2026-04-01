using System.Diagnostics;

namespace HangFireServer.Middlewares;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var cid = ctx.Request.Headers.TryGetValue(HeaderName, out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.ToString()
            : Guid.NewGuid().ToString();

        ctx.Items[HeaderName] = cid;
        ctx.Response.Headers[HeaderName] = cid;

        Activity.Current?.SetTag(HeaderName, cid);
        await _next(ctx);
    }
}
