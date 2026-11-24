using NeoAgenda.Domain.Entities;

namespace NeoAgenda.Domain.Repositories;

public interface IMetricsRepository
{
    Task AddAsync(RequestMetric metric, CancellationToken ct = default);
    Task<IReadOnlyList<RequestMetric>> GetRecentAsync(int count = 500, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
