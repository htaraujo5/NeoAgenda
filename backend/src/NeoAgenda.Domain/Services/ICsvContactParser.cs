namespace NeoAgenda.Domain.Services;

public record CsvContactRecord(
    int LineNumber,
    string Name,
    string Email,
    string PhoneOriginal,
    string PhoneNormalized,
    string RawLine
);

public record CsvRowError(
    int LineNumber,
    string RawLine,
    string? Field,
    string Reason
);

public record CsvParseResult(
    int TotalRows,
    IReadOnlyList<CsvContactRecord> ValidRecords,
    IReadOnlyList<CsvRowError> Errors
);

/// <summary>
/// Contrato para o analisador resiliente de arquivos CSV de contatos.
/// </summary>
public interface ICsvContactParser
{
    /// <summary>
    /// Analisa o conteúdo de um arquivo CSV em stream, identificando automaticamente delimitadores e colunas,
    /// validando campos e prevenindo duplicidades no lote.
    /// </summary>
    Task<CsvParseResult> ParseAsync(Stream csvStream, CancellationToken ct = default);
}
