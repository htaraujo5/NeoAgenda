using System.Text;
using FluentAssertions;
using NeoAgenda.Application.Services;
using NeoAgenda.Domain.Services;
using Xunit;

namespace NeoAgenda.UnitTests.Services;

public class CsvContactParserTests
{
    private readonly CsvContactParser _sut;

    public CsvContactParserTests()
    {
        IPhoneNormalizer normalizer = new PhoneNormalizer();
        _sut = new CsvContactParser(normalizer);
    }

    [Fact]
    public async Task ParseAsync_ShouldParseValidCsv_WithSemicolonDelimiterAndPtHeaders()
    {
        // Arrange
        var csvContent = """
        nome;email;telefone
        João Silva;joao@teste.com;(11) 98765-4321
        Maria Souza;maria@teste.com;11912345678
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _sut.ParseAsync(stream);

        // Assert
        result.TotalRows.Should().Be(2);
        result.ValidRecords.Should().HaveCount(2);
        result.Errors.Should().BeEmpty();

        result.ValidRecords[0].Name.Should().Be("João Silva");
        result.ValidRecords[0].Email.Should().Be("joao@teste.com");
        result.ValidRecords[0].PhoneNormalized.Should().Be("+5511987654321");
        result.ValidRecords[0].LineNumber.Should().Be(2);

        result.ValidRecords[1].Name.Should().Be("Maria Souza");
        result.ValidRecords[1].PhoneNormalized.Should().Be("+5511912345678");
    }

    [Fact]
    public async Task ParseAsync_ShouldParseValidCsv_WithCommaDelimiterAndEnHeaders()
    {
        // Arrange
        var csvContent = """
        name,email,phone
        Carlos Drum,carlos@teste.com,+55 (11) 98765-4321
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _sut.ParseAsync(stream);

        // Assert
        result.ValidRecords.Should().HaveCount(1);
        result.Errors.Should().BeEmpty();
        result.ValidRecords[0].Name.Should().Be("Carlos Drum");
        result.ValidRecords[0].PhoneNormalized.Should().Be("+5511987654321");
    }

    [Fact]
    public async Task ParseAsync_ShouldBeResilient_RejectingInvalidRowsAndKeepingValidOnes()
    {
        // Arrange
        var csvContent = """
        nome;email;telefone
        Valido Um;valido1@teste.com;(11) 98765-4321
        ;semnome@teste.com;(11) 91111-2222
        Valido Dois;valido2@teste.com;(21) 98888-7777
        Email Ruim;emailinvalido;(31) 97777-6666
        Tel Ruim;telruim@teste.com;123
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _sut.ParseAsync(stream);

        // Assert
        result.TotalRows.Should().Be(5);
        result.ValidRecords.Should().HaveCount(2);
        result.Errors.Should().HaveCount(3);

        // Linha 3: Sem nome
        var err1 = result.Errors.First(e => e.LineNumber == 3);
        err1.Field.Should().Be("Name");
        err1.Reason.Should().Contain("obrigatório");

        // Linha 5: Email inválido
        var err2 = result.Errors.First(e => e.LineNumber == 5);
        err2.Field.Should().Be("Email");
        err2.Reason.Should().Contain("formato inválido");

        // Linha 6: Telefone com poucos dígitos
        var err3 = result.Errors.First(e => e.LineNumber == 6);
        err3.Field.Should().Be("Phone");
        err3.Reason.Should().Contain("inválida");
    }

    [Fact]
    public async Task ParseAsync_ShouldDetectIntraFileDuplicatePhone_AndRejectSubsequentOccurrence()
    {
        // Arrange: Linha 2 e Linha 4 têm o mesmo telefone em formatos diferentes
        var csvContent = """
        nome;email;telefone
        Primeiro Cadastro;primeiro@teste.com;(11) 98765-4321
        Outro Contato;outro@teste.com;(21) 99999-8888
        Segundo Com Mesmo Tel;segundo@teste.com;+55 11 98765-4321
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _sut.ParseAsync(stream);

        // Assert
        result.ValidRecords.Should().HaveCount(2);
        result.Errors.Should().HaveCount(1);

        var duplicateError = result.Errors.Single();
        duplicateError.LineNumber.Should().Be(4);
        duplicateError.Field.Should().Be("Phone");
        duplicateError.Reason.Should().Contain("duplicado com a linha 2");
    }

    [Fact]
    public async Task ParseAsync_ShouldHandleQuotedValuesAndEscapedQuotes()
    {
        // Arrange
        var csvContent = """
        "nome","email","telefone"
        "Silva, João","joao@teste.com","(11) 98765-4321"
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _sut.ParseAsync(stream);

        // Assert
        result.ValidRecords.Should().HaveCount(1);
        result.ValidRecords[0].Name.Should().Be("Silva, João");
        result.ValidRecords[0].PhoneNormalized.Should().Be("+5511987654321");
    }

    [Fact]
    public async Task ParseAsync_ShouldReturnError_WhenHeaderIsMissingColumns()
    {
        // Arrange
        var csvContent = "nome;email\nJoão;joao@teste.com";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _sut.ParseAsync(stream);

        // Assert
        result.ValidRecords.Should().BeEmpty();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Field.Should().Be("Header");
        result.Errors[0].Reason.Should().Contain("Telefone");
    }
}
