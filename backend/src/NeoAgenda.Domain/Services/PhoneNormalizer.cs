using System.Text.RegularExpressions;
using NeoAgenda.Domain.Common;

namespace NeoAgenda.Domain.Services;

/// <summary>
/// Implementação robusta de normalização de telefones com suporte prioritário ao plano de numeração brasileiro (E.164)
/// e números internacionais genéricos.
/// </summary>
public class PhoneNormalizer : IPhoneNormalizer
{
    // Conjunto de DDDs válidos no Brasil
    private static readonly HashSet<string> ValidBrazilianAreaCodes = new()
    {
        // SP
        "11", "12", "13", "14", "15", "16", "17", "18", "19",
        // RJ / ES
        "21", "22", "24", "27", "28",
        // MG
        "31", "32", "33", "34", "35", "37", "38",
        // PR / SC
        "41", "42", "43", "44", "45", "46", "47", "48", "49",
        // RS
        "51", "53", "54", "55",
        // Centro-Oeste / DF / TO / AC / RO
        "61", "62", "63", "64", "65", "66", "67", "68", "69",
        // BA / SE
        "71", "73", "74", "75", "77", "79",
        // Nordeste (PE, AL, PB, RN, CE, PI)
        "81", "82", "83", "84", "85", "86", "87", "88", "89",
        // Norte (PA, AP, AM, RR, MA)
        "91", "92", "93", "94", "95", "96", "97", "98", "99"
    };

    public string Normalize(string? rawPhone)
    {
        if (!TryNormalize(rawPhone, out var normalizedPhone, out var errorMessage))
        {
            throw new DomainException(errorMessage ?? "Número de telefone inválido.");
        }

        return normalizedPhone;
    }

    public bool TryNormalize(string? rawPhone, out string normalizedPhone, out string? errorMessage)
    {
        normalizedPhone = string.Empty;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            errorMessage = "O telefone não pode ser vazio.";
            return false;
        }

        var trimmed = rawPhone.Trim();
        var hadPlusSign = trimmed.StartsWith('+');

        // Remove tudo que não for dígito numérico
        var digitsOnly = Regex.Replace(trimmed, @"\D", "");

        if (string.IsNullOrWhiteSpace(digitsOnly))
        {
            errorMessage = "O telefone deve conter caracteres numéricos válidos.";
            return false;
        }

        // Caso especial: iniciou com 0 antes do DDD nacional (ex: 011987654321 ou 05511987654321)
        if (digitsOnly.StartsWith("0") && digitsOnly.Length >= 11)
        {
            digitsOnly = digitsOnly.TrimStart('0');
        }

        // Se o número começa com 55 e tem tamanho de número BR com DDI (12 ou 13 dígitos)
        if (digitsOnly.StartsWith("55") && (digitsOnly.Length == 12 || digitsOnly.Length == 13))
        {
            digitsOnly = digitsOnly.Substring(2); // Remove DDI 55 temporariamente para validação de DDD
        }

        // Verificação padrão Brasil (10 dígitos para fixo ou 11 dígitos para celular com DDD)
        if (digitsOnly.Length == 10 || digitsOnly.Length == 11)
        {
            var ddd = digitsOnly.Substring(0, 2);
            var numberPart = digitsOnly.Substring(2);

            if (!ValidBrazilianAreaCodes.Contains(ddd))
            {
                errorMessage = $"O DDD '{ddd}' não é um código de área brasileiro válido.";
                return false;
            }

            // Validação de celular (11 dígitos): nono dígito obrigatório deve ser 9
            if (digitsOnly.Length == 11)
            {
                if (numberPart[0] != '9')
                {
                    errorMessage = "Celulares brasileiros com 11 dígitos devem iniciar com o dígito 9 após o DDD.";
                    return false;
                }
            }
            // Validação de fixo (10 dígitos): primeiro dígito costuma ser de 2 a 5
            else if (digitsOnly.Length == 10)
            {
                char firstDigit = numberPart[0];
                if (firstDigit < '2' || firstDigit > '5')
                {
                    // Caso tenha sido digitado sem o 9º dígito obrigatório de celular
                    errorMessage = "Telefones fixos devem iniciar com dígitos entre 2 e 5. Caso seja celular, informe os 9 dígitos.";
                    return false;
                }
            }

            normalizedPhone = $"+55{digitsOnly}";
            return true;
        }

        // Tratamento para números internacionais explicitamente informados com '+'
        if (hadPlusSign && digitsOnly.Length >= 8 && digitsOnly.Length <= 15)
        {
            normalizedPhone = $"+{digitsOnly}";
            return true;
        }

        errorMessage = $"O número de telefone '{rawPhone}' possui um formato ou quantidade de dígitos inválida ({digitsOnly.Length} dígitos).";
        return false;
    }
}
