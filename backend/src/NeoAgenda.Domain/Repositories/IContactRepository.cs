using NeoAgenda.Domain.Entities;

namespace NeoAgenda.Domain.Repositories;

/// <summary>
/// Contrato de persistência e consulta para a entidade Contact.
/// </summary>
public interface IContactRepository
{
    Task<Contact?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);
    Task<Contact?> GetByNormalizedPhoneAsync(string normalizedPhone, CancellationToken ct = default);
    Task<bool> ExistsByNormalizedPhoneAsync(string normalizedPhone, Guid? excludeContactId = null, CancellationToken ct = default);
    Task AddAsync(Contact contact, CancellationToken ct = default);
    void Update(Contact contact);
    void DeletePermanently(Contact contact);
    Task<(IReadOnlyList<Contact> Items, int TotalCount)> GetActivePagedAsync(
        string? search, 
        int page, 
        int pageSize, 
        string? sortBy, 
        bool sortDesc, 
        CancellationToken ct = default);
    Task<IReadOnlyList<Contact>> GetRecycleBinAsync(CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
