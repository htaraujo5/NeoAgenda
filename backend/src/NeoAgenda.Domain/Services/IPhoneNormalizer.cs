namespace NeoAgenda.Domain.Services;

/// <summary>
/// Contrato para o serviço de normalização e validação canônica de números de telefone.
/// </summary>
public interface IPhoneNormalizer
{
    /// <summary>
    /// Transforma qualquer formatação válida em um formato canônico E.164 único (ex: +5511987654321).
    /// Dispara DomainException caso o número seja inválido ou inconsistente.
    /// </summary>
    string Normalize(string? rawPhone);

    /// <summary>
    /// Tenta normalizar o número sem disparar exceção.
    /// </summary>
    bool TryNormalize(string? rawPhone, out string normalizedPhone, out string? errorMessage);
}
