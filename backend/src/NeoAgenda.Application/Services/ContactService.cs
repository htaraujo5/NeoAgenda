using System.Security.Cryptography;
using System.Text;
using NeoAgenda.Application.DTOs;
using NeoAgenda.Application.Interfaces;
using NeoAgenda.Domain.Common;
using NeoAgenda.Domain.Entities;
using NeoAgenda.Domain.Repositories;
using NeoAgenda.Domain.Services;

namespace NeoAgenda.Application.Services;

public class ContactService : IContactService
{
    private readonly IContactRepository _contactRepository;
    private readonly ILgpdAuditRepository _lgpdAuditRepository;
    private readonly IPhoneNormalizer _phoneNormalizer;
    private readonly ICsvContactParser _csvContactParser;

    public ContactService(
        IContactRepository contactRepository,
        ILgpdAuditRepository lgpdAuditRepository,
        IPhoneNormalizer phoneNormalizer,
        ICsvContactParser csvContactParser)
    {
        _contactRepository = contactRepository;
        _lgpdAuditRepository = lgpdAuditRepository;
        _phoneNormalizer = phoneNormalizer;
        _csvContactParser = csvContactParser;
    }

    public async Task<PagedResultDto<ContactDto>> GetActivePagedAsync(
        string? search, 
        int page, 
        int pageSize, 
        string? sortBy, 
        bool sortDesc, 
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await _contactRepository.GetActivePagedAsync(
            search, page, pageSize, sortBy, sortDesc, ct);

        var dtos = items.Select(MapToDto).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResultDto<ContactDto>(dtos, page, pageSize, totalCount, totalPages);
    }

    public async Task<ContactDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var contact = await _contactRepository.GetByIdAsync(id, includeDeleted: false, ct);
        if (contact == null)
            throw new DomainException($"Contato com ID '{id}' não foi encontrado.");

        return MapToDto(contact);
    }

    public async Task<ContactDto> CreateAsync(CreateContactDto dto, CancellationToken ct = default)
    {
        var normalizedPhone = _phoneNormalizer.Normalize(dto.Phone);

        // Valida duplicidade de telefone ativo
        var exists = await _contactRepository.ExistsByNormalizedPhoneAsync(normalizedPhone, null, ct);
        if (exists)
        {
            throw new DomainException($"Já existe um contato ativo cadastrado com o telefone informado ({normalizedPhone}).");
        }

        var contact = Contact.Create(dto.Name, dto.Email, dto.Phone, normalizedPhone);
        await _contactRepository.AddAsync(contact, ct);
        await _contactRepository.SaveChangesAsync(ct);

        return MapToDto(contact);
    }

    public async Task<ContactDto> UpdateAsync(Guid id, UpdateContactDto dto, CancellationToken ct = default)
    {
        var contact = await _contactRepository.GetByIdAsync(id, includeDeleted: false, ct);
        if (contact == null)
            throw new DomainException($"Contato com ID '{id}' não foi encontrado.");

        var normalizedPhone = _phoneNormalizer.Normalize(dto.Phone);

        // Valida se o telefone novo colide com outro contato ativo
        var exists = await _contactRepository.ExistsByNormalizedPhoneAsync(normalizedPhone, excludeContactId: id, ct);
        if (exists)
        {
            throw new DomainException($"Já existe outro contato ativo utilizando o telefone ({normalizedPhone}).");
        }

        contact.Update(dto.Name, dto.Email, dto.Phone, normalizedPhone);
        _contactRepository.Update(contact);
        await _contactRepository.SaveChangesAsync(ct);

        return MapToDto(contact);
    }

    public async Task<SoftDeleteResultDto> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var contact = await _contactRepository.GetByIdAsync(id, includeDeleted: false, ct);
        if (contact == null)
            throw new DomainException($"Contato com ID '{id}' não foi encontrado.");

        contact.MarkAsDeleted();
        _contactRepository.Update(contact);
        await _contactRepository.SaveChangesAsync(ct);

        return new SoftDeleteResultDto(
            contact.Id,
            "Contato movido para a lixeira. Restauração permitida por 30 dias.",
            contact.DeletedAt!.Value,
            contact.RetentionExpiresAt!.Value
        );
    }

    public async Task<RestoreContactResultDto> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        var contact = await _contactRepository.GetByIdAsync(id, includeDeleted: true, ct);
        if (contact == null || !contact.IsDeleted)
            throw new DomainException($"Contato excluído com ID '{id}' não foi encontrado.");

        if (!contact.CanBeRestored())
        {
            throw new DomainException("O prazo de 30 dias para restauração deste contato expirou.");
        }

        // Verifica se durante o período de lixeira o telefone foi ocupado por outro contato ativo
        var exists = await _contactRepository.ExistsByNormalizedPhoneAsync(contact.PhoneNormalized, excludeContactId: id, ct);
        if (exists)
        {
            throw new DomainException($"Não é possível restaurar o contato: o telefone ({contact.PhoneNormalized}) já está em uso por outro contato ativo.");
        }

        contact.Restore();
        _contactRepository.Update(contact);
        await _contactRepository.SaveChangesAsync(ct);

        return new RestoreContactResultDto(
            contact.Id,
            "Contato restaurado com sucesso para a lista ativa.",
            MapToDto(contact)
        );
    }

    public async Task<LgpdPurgeResultDto> PurgeLgpdAsync(Guid id, string? requestedBy = null, CancellationToken ct = default)
    {
        var contact = await _contactRepository.GetByIdAsync(id, includeDeleted: true, ct);
        if (contact == null)
            throw new DomainException($"Contato com ID '{id}' não foi encontrado.");

        // Cria hash irreversível do telefone normalizado para auditoria de compliance
        var hash = ComputeSha256(contact.PhoneNormalized + contact.Id);

        var audit = LgpdComplianceAudit.Create(
            contactHash: hash,
            requestedBy: requestedBy ?? "usuario_solicitante_lgpd"
        );

        await _lgpdAuditRepository.AddAsync(audit, ct);
        _contactRepository.DeletePermanently(contact);
        await _contactRepository.SaveChangesAsync(ct);

        var protocol = $"LGPD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";

        return new LgpdPurgeResultDto(
            protocol,
            "Todos os dados pessoais do titular foram permanentemente excluídos e não podem mais ser recuperados.",
            DateTime.UtcNow
        );
    }

    public async Task<IReadOnlyList<RecycleBinContactDto>> GetRecycleBinAsync(CancellationToken ct = default)
    {
        var items = await _contactRepository.GetRecycleBinAsync(ct);
        var now = DateTime.UtcNow;

        return items.Select(c => new RecycleBinContactDto(
            c.Id,
            c.Name,
            c.Email,
            c.PhoneOriginal,
            c.PhoneNormalized,
            c.DeletedAt ?? now,
            c.RetentionExpiresAt ?? now,
            c.GetDaysRemainingInRecycleBin(now),
            !c.CanBeRestored(now)
        )).ToList();
    }

    public async Task<CsvImportResultDto> ImportCsvAsync(Stream csvStream, CancellationToken ct = default)
    {
        var parseResult = await _csvContactParser.ParseAsync(csvStream, ct);
        var importedContacts = new List<ContactDto>();
        var errors = new List<CsvRowErrorDto>(
            parseResult.Errors.Select(e => new CsvRowErrorDto(e.LineNumber, e.RawLine, e.Field, e.Reason))
        );

        foreach (var record in parseResult.ValidRecords)
        {
            // Valida se o telefone já existe no banco de dados entre os contatos ativos
            var exists = await _contactRepository.ExistsByNormalizedPhoneAsync(record.PhoneNormalized, null, ct);
            if (exists)
            {
                errors.Add(new CsvRowErrorDto(
                    record.LineNumber,
                    record.RawLine,
                    "Phone",
                    $"O telefone ({record.PhoneOriginal}) já está cadastrado no sistema."
                ));
                continue;
            }

            var contact = Contact.Create(record.Name, record.Email, record.PhoneOriginal, record.PhoneNormalized);
            await _contactRepository.AddAsync(contact, ct);
            importedContacts.Add(MapToDto(contact));
        }

        if (importedContacts.Count > 0)
        {
            await _contactRepository.SaveChangesAsync(ct);
        }

        var totalRows = parseResult.TotalRows;
        if (totalRows == 0 && (importedContacts.Count + errors.Count) > 0)
        {
            totalRows = importedContacts.Count + errors.Count;
        }

        return new CsvImportResultDto(
            totalRows,
            importedContacts.Count,
            errors.Count,
            errors.OrderBy(e => e.LineNumber).ToList(),
            importedContacts
        );
    }

    public async Task<byte[]> ExportCsvAsync(CancellationToken ct = default)
    {
        var (items, _) = await _contactRepository.GetActivePagedAsync(null, 1, 100000, "name", false, ct);
        var sb = new StringBuilder();
        sb.AppendLine("Nome;Email;Telefone;DataCadastro");

        foreach (var contact in items)
        {
            var line = $"\"{EscapeCsv(contact.Name)}\";\"{EscapeCsv(contact.Email)}\";\"{EscapeCsv(contact.PhoneOriginal)}\";\"{contact.CreatedAt:yyyy-MM-dd HH:mm:ss}\"";
            sb.AppendLine(line);
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string EscapeCsv(string value)
    {
        return value.Replace("\"", "\"\"");
    }

    private static ContactDto MapToDto(Contact contact)
    {
        return new ContactDto(
            contact.Id,
            contact.Name,
            contact.Email,
            contact.PhoneOriginal,
            contact.PhoneNormalized,
            contact.CreatedAt,
            contact.UpdatedAt
        );
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
