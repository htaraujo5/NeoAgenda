using NeoAgenda.Domain.Entities;

namespace NeoAgenda.Domain.Repositories;

/// <summary>
/// Contrato de persistência para registros de auditoria anônima de exclusão LGPD.
/// </summary>
public interface ILgpdAuditRepository
{
    Task AddAsync(LgpdComplianceAudit audit, CancellationToken ct = default);
    Task<IReadOnlyList<LgpdComplianceAudit>> GetAllAsync(CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
