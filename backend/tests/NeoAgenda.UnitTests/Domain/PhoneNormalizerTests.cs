using FluentAssertions;
using NeoAgenda.Domain.Common;
using NeoAgenda.Domain.Services;
using Xunit;

namespace NeoAgenda.UnitTests.Domain;

/// <summary>
/// Testes rigorosos cobrindo todas as nuances da Regra RN01 (Unicidade e Normalização Canônica de Telefones).
/// </summary>
public class PhoneNormalizerTests
{
    private readonly PhoneNormalizer _sut = new();

    [Theory]
    [InlineData("(11) 98765-4321")]
    [InlineData("11987654321")]
    [InlineData("+55 11 98765-4321")]
    [InlineData("+55 11 987654321")]
    [InlineData("5511987654321")]
    [InlineData("011 98765 4321")]
    [InlineData("(11) 9.8765-4321")]
    [InlineData("11 9 8765 4321")]
    [InlineData(" +55 (11) 98765-4321 ")]
    public void Normalize_ShouldReturnCanonicalE164_ForEquivalentMobilePhoneFormats(string rawPhone)
    {
        // Act
        var normalized = _sut.Normalize(rawPhone);

        // Assert
        normalized.Should().Be("+5511987654321", "todas as variações de formatação devem normalizar para a mesma representação canônica");
    }

    [Theory]
    [InlineData("(11) 3333-4444")]
    [InlineData("1133334444")]
    [InlineData("+55 11 3333-4444")]
    [InlineData("551133334444")]
    [InlineData("011 3333 4444")]
    public void Normalize_ShouldReturnCanonicalE164_ForEquivalentLandlinePhoneFormats(string rawPhone)
    {
        // Act
        var normalized = _sut.Normalize(rawPhone);

        // Assert
        normalized.Should().Be("+551133334444", "telefones fixos válidos (8 dígitos) com DDD devem ser normalizados");
    }

    [Theory]
    [InlineData("21", "988887777", "+5521988887777")] // Rio de Janeiro
    [InlineData("31", "999998888", "+5531999998888")] // Belo Horizonte
    [InlineData("41", "977776666", "+5541977776666")] // Curitiba
    [InlineData("51", "966665555", "+5551966665555")] // Porto Alegre
    [InlineData("61", "955554444", "+5561955554444")] // Brasília
    [InlineData("71", "944443333", "+5571944443333")] // Salvador
    [InlineData("81", "933332222", "+5581933332222")] // Recife
    [InlineData("91", "922221111", "+5591922221111")] // Belém
    public void Normalize_ShouldAcceptValidBrazilianAreaCodes(string ddd, string number, string expected)
    {
        // Arrange
        var raw = $"({ddd}) {number}";

        // Act
        var normalized = _sut.Normalize(raw);

        // Assert
        normalized.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_ShouldThrowDomainException_WhenPhoneIsNullOrEmpty(string? input)
    {
        // Act
        Action act = () => _sut.Normalize(input);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*não pode ser vazio*");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("telefone-teste")]
    [InlineData("(xx) xxxxx-xxxx")]
    public void Normalize_ShouldThrowDomainException_WhenNoDigitsPresent(string input)
    {
        // Act
        Action act = () => _sut.Normalize(input);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*caracteres numéricos válidos*");
    }

    [Theory]
    [InlineData("12345")] // Poucos dígitos
    [InlineData("987654321")] // 9 dígitos sem DDD
    public void Normalize_ShouldThrowDomainException_WhenPhoneHasInsufficientDigits(string input)
    {
        // Act
        Action act = () => _sut.Normalize(input);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*inválida*");
    }

    [Theory]
    [InlineData("(00) 98765-4321")] // DDD 00 inexistente
    [InlineData("(20) 98765-4321")] // DDD 20 inexistente
    [InlineData("(36) 98765-4321")] // DDD 36 inexistente
    public void Normalize_ShouldThrowDomainException_WhenAreaCodeIsInvalid(string input)
    {
        // Act
        Action act = () => _sut.Normalize(input);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*não é um código de área brasileiro válido*");
    }

    [Fact]
    public void TryNormalize_ShouldReturnFalse_WithoutThrowing_WhenPhoneIsInvalid()
    {
        // Act
        var success = _sut.TryNormalize("invalido", out var normalized, out var error);

        // Assert
        success.Should().BeFalse();
        normalized.Should().BeEmpty();
        error.Should().NotBeNullOrWhiteSpace();
    }
}
