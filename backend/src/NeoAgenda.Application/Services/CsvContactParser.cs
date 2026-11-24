using System.Text;
using System.Text.RegularExpressions;
using NeoAgenda.Domain.Services;

namespace NeoAgenda.Application.Services;

/// <summary>
/// Analisador e validador resiliente de arquivos CSV para importação em lote de contatos.
/// Suporta detecção automática de delimitadores (, ou ;), múltiplos encodings, sinônimos de cabeçalho,
/// normalização de telefone e rejeição tolerante linha a linha com detecção de duplicidades intra-arquivo.
/// </summary>
public class CsvContactParser : ICsvContactParser
{
    private readonly IPhoneNormalizer _phoneNormalizer;

    // Regex para validação de formato de e-mail (RFC 5322 simplificado)
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public CsvContactParser(IPhoneNormalizer phoneNormalizer)
    {
        _phoneNormalizer = phoneNormalizer;
    }

    public async Task<CsvParseResult> ParseAsync(Stream csvStream, CancellationToken ct = default)
    {
        var validRecords = new List<CsvContactRecord>();
        var errors = new List<CsvRowError>();
        var seenNormalizedPhones = new Dictionary<string, int>(); // PhoneNormalized -> LineNumber

        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        
        string? headerLine = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return new CsvParseResult(0, validRecords, new List<CsvRowError>
            {
                new(1, string.Empty, "File", "O arquivo CSV está vazio.")
            });
        }

        char delimiter = DetectDelimiter(headerLine);
        var headerColumns = ParseCsvLine(headerLine, delimiter);

        var nameColIndex = FindColumnIndex(headerColumns, "nome", "name", "full_name", "contato");
        var emailColIndex = FindColumnIndex(headerColumns, "email", "e-mail", "mail", "correio");
        var phoneColIndex = FindColumnIndex(headerColumns, "telefone", "phone", "tel", "celular", "whatsapp");

        if (nameColIndex == -1 || emailColIndex == -1 || phoneColIndex == -1)
        {
            var missing = new List<string>();
            if (nameColIndex == -1) missing.Add("Nome (ex: 'nome' ou 'name')");
            if (emailColIndex == -1) missing.Add("E-mail (ex: 'email' ou 'e-mail')");
            if (phoneColIndex == -1) missing.Add("Telefone (ex: 'telefone' ou 'phone')");

            return new CsvParseResult(0, validRecords, new List<CsvRowError>
            {
                new(1, headerLine, "Header", $"Cabeçalho do CSV inválido. Colunas ausentes: {string.Join(", ", missing)}.")
            });
        }

        int currentLineNumber = 1; // 1 é o cabeçalho

        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            currentLineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue; // Linha em branco ignorada silenciosamente
            }

            var columns = ParseCsvLine(line, delimiter);

            // Validação de colunas presentes
            var maxIndex = Math.Max(nameColIndex, Math.Max(emailColIndex, phoneColIndex));
            if (columns.Count <= maxIndex)
            {
                errors.Add(new CsvRowError(currentLineNumber, line, "Columns", "A linha possui menos colunas do que o cabeçalho esperado."));
                continue;
            }

            var name = columns[nameColIndex].Trim();
            var email = columns[emailColIndex].Trim();
            var phoneRaw = columns[phoneColIndex].Trim();

            // Validação do Nome
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add(new CsvRowError(currentLineNumber, line, "Name", "O nome do contato é obrigatório."));
                continue;
            }
            if (name.Length > 150)
            {
                errors.Add(new CsvRowError(currentLineNumber, line, "Name", "O nome do contato excede o limite de 150 caracteres."));
                continue;
            }

            // Validação do E-mail
            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add(new CsvRowError(currentLineNumber, line, "Email", "O e-mail do contato é obrigatório."));
                continue;
            }
            if (!EmailRegex.IsMatch(email) || email.Length > 254)
            {
                errors.Add(new CsvRowError(currentLineNumber, line, "Email", $"O endereço de e-mail '{email}' tem formato inválido."));
                continue;
            }

            // Validação e normalização de telefone
            if (string.IsNullOrWhiteSpace(phoneRaw))
            {
                errors.Add(new CsvRowError(currentLineNumber, line, "Phone", "O telefone do contato é obrigatório."));
                continue;
            }

            if (!_phoneNormalizer.TryNormalize(phoneRaw, out var normalizedPhone, out var phoneError))
            {
                errors.Add(new CsvRowError(currentLineNumber, line, "Phone", phoneError ?? "Telefone com formato inválido."));
                continue;
            }

            // Validação de duplicidade no próprio lote (intra-arquivo)
            if (seenNormalizedPhones.TryGetValue(normalizedPhone, out var previousLine))
            {
                errors.Add(new CsvRowError(currentLineNumber, line, "Phone", 
                    $"Telefone ({phoneRaw}) duplicado com a linha {previousLine} do próprio arquivo CSV."));
                continue;
            }

            seenNormalizedPhones[normalizedPhone] = currentLineNumber;

            validRecords.Add(new CsvContactRecord(
                currentLineNumber,
                name,
                email.ToLowerInvariant(),
                phoneRaw,
                normalizedPhone,
                line
            ));
        }

        int totalDataRows = validRecords.Count + errors.Count;
        return new CsvParseResult(totalDataRows, validRecords, errors);
    }

    private static char DetectDelimiter(string headerLine)
    {
        int semicolonCount = headerLine.Count(c => c == ';');
        int commaCount = headerLine.Count(c => c == ',');
        int tabCount = headerLine.Count(c => c == '\t');

        if (semicolonCount > commaCount && semicolonCount > tabCount) return ';';
        if (tabCount > commaCount && tabCount > semicolonCount) return '\t';
        return ',';
    }

    private static int FindColumnIndex(List<string> headers, params string[] synonyms)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i].Trim().ToLowerInvariant();
            if (synonyms.Any(s => header.Equals(s, StringComparison.OrdinalIgnoreCase)))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Faz o parse seguro de uma linha CSV em conformidade com a RFC 4180 (aspas, escape de aspas, delimitadores).
    /// </summary>
    private static List<string> ParseCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // Tratamento de aspas duplas escapadas ("")
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++; // Pula a aspa escapada
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        result.Add(sb.ToString());
        return result;
    }
}
