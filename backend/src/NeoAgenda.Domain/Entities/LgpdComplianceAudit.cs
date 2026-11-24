namespace NeoAgenda.Domain.Entities;

/// <summary>
/// Registro anônimo de auditoria para fins de compliance com o Artigo 18, VI da LGPD (Direito ao Esquecimento).
/// Não armazena nenhum dado pessoal (PII) do titular.
/// </summary>
public class LgpdComplianceAudit
{
    public Guid Id { get; private set; }
    public string ContactHash { get; private set; } = string.Empty;
    public DateTime ExecutedAt { get; private set; }
    public string LegalBasis { get; private set; } = string.Empty;
    public string RequestedBy { get; private set; } = string.Empty;

    protected LgpdComplianceAudit() { }

    public static LgpdComplianceAudit Create(string contactHash, string requestedBy = "sistema/titular", string legalBasis = "Artigo 18, VI da Lei 13.709/2018 (LGPD)")
    {
        return new LgpdComplianceAudit
        {
            Id = Guid.NewGuid(),
            ContactHash = contactHash,
            ExecutedAt = DateTime.UtcNow,
            LegalBasis = legalBasis,
            RequestedBy = requestedBy
        };
    }
}
