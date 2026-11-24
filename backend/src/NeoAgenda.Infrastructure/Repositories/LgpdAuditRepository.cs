using NeoAgenda.Domain.Entities;
using NeoAgenda.Domain.Repositories;
using NeoAgenda.Infrastructure.Data;

namespace NeoAgenda.Infrastructure.Repositories;

public class LgpdAuditRepository : ILgpdAuditRepository
{
    private readonly AppDbContext _context;

    public LgpdAuditRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LgpdComplianceAudit audit, CancellationToken ct = default)
    {
        await _context.LgpdAudits.AddAsync(audit, ct);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
