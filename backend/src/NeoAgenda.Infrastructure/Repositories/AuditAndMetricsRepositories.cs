using Microsoft.EntityFrameworkCore;
using NeoAgenda.Domain.Entities;
using NeoAgenda.Domain.Repositories;
using NeoAgenda.Infrastructure.Data;

namespace NeoAgenda.Infrastructure.Repositories;

public class AccessLogRepository : IAccessLogRepository
{
    private readonly AppDbContext _context;

    public AccessLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AccessLog log, CancellationToken ct = default)
    {
        await _context.AccessLogs.AddAsync(log, ct);
    }

    public async Task<IReadOnlyList<AccessLog>> GetRecentAsync(int count = 100, CancellationToken ct = default)
    {
        return await _context.AccessLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AccessLog>> GetByUserAsync(string username, int count = 100, CancellationToken ct = default)
    {
        var normalized = username.ToLowerInvariant().Trim();
        return await _context.AccessLogs
            .Where(l => l.Username.ToLower() == normalized)
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}

public class MetricsRepository : IMetricsRepository
{
    private readonly AppDbContext _context;

    public MetricsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RequestMetric metric, CancellationToken ct = default)
    {
        await _context.RequestMetrics.AddAsync(metric, ct);
    }

    public async Task<IReadOnlyList<RequestMetric>> GetRecentAsync(int count = 500, CancellationToken ct = default)
    {
        return await _context.RequestMetrics
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
