using NeoAgenda.Domain.Entities;

namespace NeoAgenda.Domain.Repositories;

public interface IAccessLogRepository
{
    Task AddAsync(AccessLog log, CancellationToken ct = default);
    Task<IReadOnlyList<AccessLog>> GetRecentAsync(int count = 100, CancellationToken ct = default);
    Task<IReadOnlyList<AccessLog>> GetByUserAsync(string username, int count = 100, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
