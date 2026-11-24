using System.Diagnostics;
using NeoAgenda.Application.Interfaces;

namespace NeoAgenda.Api.Middlewares;

public class PerformanceTelemetryMiddleware
{
    private readonly RequestDelegate _next;

    public PerformanceTelemetryMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IPerformanceMetricsService metricsService)
    {
        // Ignora endpoints estáticos ou healthchecks para métricas operacionais limpas
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/health") || path.StartsWith("/swagger") || path.Contains('.'))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var durationMs = stopwatch.ElapsedMilliseconds;
            var method = context.Request.Method;
            var statusCode = context.Response.StatusCode;

            try
            {
                await metricsService.RecordRequestMetricAsync(method, path, statusCode, durationMs);
            }
            catch
            {
                // Telemetria não deve quebrar a resposta da requisição do cliente
            }
        }
    }
}
