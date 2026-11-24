namespace NeoAgenda.Application.DTOs;

public record ContactDto(
    Guid Id,
    string Name,
    string Email,
    string PhoneOriginal,
    string PhoneNormalized,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CreatedBy = "system"
);

public record CreateContactDto(
    string Name,
    string Email,
    string Phone
);

public record UpdateContactDto(
    string Name,
    string Email,
    string Phone
);

public record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);

public record RecycleBinContactDto(
    Guid Id,
    string Name,
    string Email,
    string PhoneOriginal,
    string PhoneNormalized,
    DateTime DeletedAt,
    DateTime RetentionExpiresAt,
    int DaysRemaining,
    bool IsExpired,
    string CreatedBy = "system"
);

public record SoftDeleteResultDto(
    Guid Id,
    string Message,
    DateTime DeletedAt,
    DateTime RetentionExpiresAt
);

public record RestoreContactResultDto(
    Guid Id,
    string Message,
    ContactDto Contact
);

public record LgpdPurgeResultDto(
    string Protocol,
    string Message,
    DateTime PurgedAt
);

public record LgpdAuditLogDto(
    Guid Id,
    string ContactHash,
    DateTime ExecutedAt,
    string LegalBasis,
    string RequestedBy
);

public record CsvRowErrorDto(
    int LineNumber,
    string RawLine,
    string? Field,
    string Reason
);

public record CsvImportResultDto(
    int TotalRows,
    int ImportedCount,
    int FailedCount,
    IReadOnlyList<CsvRowErrorDto> Errors,
    IReadOnlyList<ContactDto> ImportedContacts
);
