using NeoAgenda.Domain.Common;

namespace NeoAgenda.Domain.Entities;

/// <summary>
/// Representa a entidade central de Contato da agenda.
/// </summary>
public class Contact
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneOriginal { get; private set; } = string.Empty;
    public string PhoneNormalized { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string CreatedBy { get; private set; } = "system";

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime? RetentionExpiresAt { get; private set; }

    // Construtor protegido para uso do EF Core
    protected Contact() { }

    /// <summary>
    /// Factory method para criação de um novo contato validado.
    /// </summary>
    public static Contact Create(string name, string email, string phoneOriginal, string phoneNormalized, string createdBy = "system")
    {
        ValidateRequiredFields(name, email, phoneOriginal, phoneNormalized);

        return new Contact
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PhoneOriginal = phoneOriginal.Trim(),
            PhoneNormalized = phoneNormalized.Trim(),
            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "system" : createdBy.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    /// <summary>
    /// Atualiza os dados cadastrais do contato.
    /// </summary>
    public void Update(string name, string email, string phoneOriginal, string phoneNormalized)
    {
        if (IsDeleted)
            throw new DomainException("Não é possível alterar um contato que está na lixeira.");

        ValidateRequiredFields(name, email, phoneOriginal, phoneNormalized);

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        PhoneOriginal = phoneOriginal.Trim();
        PhoneNormalized = phoneNormalized.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Executa o Soft Delete com prazo de retenção de 30 dias para possível restauração.
    /// </summary>
    public void MarkAsDeleted(DateTime? deletedAt = null)
    {
        if (IsDeleted)
            throw new DomainException("O contato já se encontra na lixeira.");

        var referenceDate = deletedAt ?? DateTime.UtcNow;
        IsDeleted = true;
        DeletedAt = referenceDate;
        RetentionExpiresAt = referenceDate.AddDays(30);
    }

    /// <summary>
    /// Restaura um contato excluído caso esteja dentro do prazo de retenção de 30 dias.
    /// </summary>
    public void Restore(DateTime? checkDate = null)
    {
        if (!IsDeleted)
            throw new DomainException("O contato não está na lixeira.");

        var referenceDate = checkDate ?? DateTime.UtcNow;
        if (RetentionExpiresAt.HasValue && referenceDate > RetentionExpiresAt.Value)
        {
            throw new DomainException("O prazo de 30 dias para restauração do contato expirou.");
        }

        IsDeleted = false;
        DeletedAt = null;
        RetentionExpiresAt = null;
        UpdatedAt = referenceDate;
    }

    /// <summary>
    /// Verifica se o contato pode ser restaurado no momento atual.
    /// </summary>
    public bool CanBeRestored(DateTime? checkDate = null)
    {
        if (!IsDeleted) return false;
        var referenceDate = checkDate ?? DateTime.UtcNow;
        return RetentionExpiresAt.HasValue && referenceDate <= RetentionExpiresAt.Value;
    }

    /// <summary>
    /// Retorna quantos dias inteiros restam antes da exclusão permanente pela expiração da lixeira.
    /// </summary>
    public int GetDaysRemainingInRecycleBin(DateTime? checkDate = null)
    {
        if (!IsDeleted || !RetentionExpiresAt.HasValue) return 0;
        var referenceDate = checkDate ?? DateTime.UtcNow;
        var diff = RetentionExpiresAt.Value - referenceDate;
        return Math.Max(0, (int)Math.Ceiling(diff.TotalDays));
    }

    private static void ValidateRequiredFields(string name, string email, string phoneOriginal, string phoneNormalized)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do contato é obrigatório.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("O e-mail do contato é obrigatório.");

        if (string.IsNullOrWhiteSpace(phoneOriginal))
            throw new DomainException("O telefone do contato é obrigatório.");

        if (string.IsNullOrWhiteSpace(phoneNormalized))
            throw new DomainException("O telefone normalizado é obrigatório.");
    }
}
