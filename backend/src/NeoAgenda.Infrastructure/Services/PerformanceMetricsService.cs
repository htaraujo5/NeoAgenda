using NeoAgenda.Application.DTOs;
using NeoAgenda.Application.Interfaces;
using NeoAgenda.Domain.Entities;
using NeoAgenda.Domain.Repositories;

namespace NeoAgenda.Infrastructure.Services;

public class PerformanceMetricsService : IPerformanceMetricsService
{
    private readonly IMetricsRepository _metricsRepository;

    public PerformanceMetricsService(IMetricsRepository metricsRepository)
    {
        _metricsRepository = metricsRepository;
    }

    public async Task RecordRequestMetricAsync(string method, string path, int statusCode, long durationMs, CancellationToken ct = default)
    {
        var metric = new RequestMetric(method, path, statusCode, durationMs);
        await _metricsRepository.AddAsync(metric, ct);
        await _metricsRepository.SaveChangesAsync(ct);
    }

    public async Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(CancellationToken ct = default)
    {
        var recent = await _metricsRepository.GetRecentAsync(500, ct);
        if (!recent.Any())
        {
            return new PerformanceSummaryDto(
                TotalRequests: 0,
                AverageDurationMs: 0,
                P95DurationMs: 0,
                MaxDurationMs: 0,
                ErrorCount: 0,
                ErrorRatePercentage: 0,
                TopSlowestEndpoints: Array.Empty<EndpointMetricDto>(),
                StatusCodeDistribution: Array.Empty<StatusCodeDistributionDto>()
            );
        }

        var total = recent.Count;
        var avg = Math.Round(recent.Average(m => m.DurationMs), 1);
        var max = recent.Max(m => m.DurationMs);

        // P95 calculation
        var sortedDurations = recent.Select(m => m.DurationMs).OrderBy(d => d).ToList();
        var p95Index = (int)Math.Ceiling(total * 0.95) - 1;
        var p95 = sortedDurations[Math.Max(0, Math.Min(p95Index, total - 1))];

        var errors = recent.Count(m => m.StatusCode >= 400);
        var errorRate = Math.Round(((double)errors / total) * 100, 1);

        var topSlowest = recent
            .GroupBy(m => new { m.Method, m.Path })
            .Select(g => new EndpointMetricDto(
                g.Key.Method,
                g.Key.Path,
                g.Count(),
                Math.Round(g.Average(m => m.DurationMs), 1),
                g.Max(m => m.DurationMs)
            ))
            .OrderByDescending(e => e.AverageDurationMs)
            .Take(5)
            .ToList();

        var statusCodes = recent
            .GroupBy(m => m.StatusCode)
            .Select(g => new StatusCodeDistributionDto(g.Key, g.Count()))
            .OrderBy(s => s.StatusCode)
            .ToList();

        return new PerformanceSummaryDto(
            TotalRequests: total,
            AverageDurationMs: avg,
            P95DurationMs: p95,
            MaxDurationMs: max,
            ErrorCount: errors,
            ErrorRatePercentage: errorRate,
            TopSlowestEndpoints: topSlowest,
            StatusCodeDistribution: statusCodes
        );
    }
}
