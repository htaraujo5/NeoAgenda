using NeoAgenda.Application.DTOs;

namespace NeoAgenda.Application.Interfaces;

public interface IPerformanceMetricsService
{
    Task RecordRequestMetricAsync(string method, string path, int statusCode, long durationMs, CancellationToken ct = default);
    Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(CancellationToken ct = default);
}
