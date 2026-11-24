using NeoAgenda.Application.DTOs;

namespace NeoAgenda.Application.Interfaces;

/// <summary>
/// Casos de uso de gerenciamento de contatos, ciclo de vida, privacidade e importação/exportação.
/// </summary>
public interface IContactService
{
    Task<PagedResultDto<ContactDto>> GetActivePagedAsync(
        string? search, 
        int page, 
        int pageSize, 
        string? sortBy, 
        bool sortDesc, 
        string? currentUser = null,
        bool isAdmin = true,
        CancellationToken ct = default);

    Task<ContactDto> GetByIdAsync(Guid id, string? currentUser = null, bool isAdmin = true, CancellationToken ct = default);

    Task<ContactDto> CreateAsync(CreateContactDto dto, string? currentUser = null, CancellationToken ct = default);

    Task<ContactDto> UpdateAsync(Guid id, UpdateContactDto dto, string? currentUser = null, bool isAdmin = true, CancellationToken ct = default);

    Task<SoftDeleteResultDto> SoftDeleteAsync(Guid id, string? currentUser = null, bool isAdmin = true, CancellationToken ct = default);

    Task<RestoreContactResultDto> RestoreAsync(Guid id, string? currentUser = null, bool isAdmin = true, CancellationToken ct = default);

    Task<LgpdPurgeResultDto> PurgeLgpdAsync(Guid id, string? requestedBy = null, bool isAdmin = true, CancellationToken ct = default);

    Task<IReadOnlyList<RecycleBinContactDto>> GetRecycleBinAsync(string? currentUser = null, bool isAdmin = true, CancellationToken ct = default);

    Task<IReadOnlyList<LgpdAuditLogDto>> GetLgpdAuditLogsAsync(CancellationToken ct = default);

    Task<CsvImportResultDto> ImportCsvAsync(Stream csvStream, string? currentUser = null, CancellationToken ct = default);

    Task<byte[]> ExportCsvAsync(string? currentUser = null, bool isAdmin = true, CancellationToken ct = default);
}
