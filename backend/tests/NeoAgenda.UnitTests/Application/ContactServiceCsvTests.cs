using System.Text;
using FluentAssertions;
using Moq;
using NeoAgenda.Application.Services;
using NeoAgenda.Domain.Entities;
using NeoAgenda.Domain.Repositories;
using NeoAgenda.Domain.Services;
using Xunit;

namespace NeoAgenda.UnitTests.Application;

public class ContactServiceCsvTests
{
    private readonly Mock<IContactRepository> _contactRepoMock = new();
    private readonly Mock<ILgpdAuditRepository> _lgpdRepoMock = new();
    private readonly ContactService _sut;

    public ContactServiceCsvTests()
    {
        IPhoneNormalizer normalizer = new PhoneNormalizer();
        ICsvContactParser parser = new CsvContactParser(normalizer);
        _sut = new ContactService(_contactRepoMock.Object, _lgpdRepoMock.Object, normalizer, parser);
    }

    [Fact]
    public async Task ImportCsvAsync_ShouldImportValidRows_AndFlagDatabaseDuplicatePhones()
    {
        // Arrange
        var csvContent = """
        nome;email;telefone
        Novo Um;novo1@teste.com;(11) 98765-4321
        Duplicado Banco;banco@teste.com;(11) 91111-2222
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // O primeiro número não existe no banco
        _contactRepoMock.Setup(r => r.ExistsByNormalizedPhoneAsync("+5511987654321", null, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

        // O segundo número já existe ativo no banco de dados
        _contactRepoMock.Setup(r => r.ExistsByNormalizedPhoneAsync("+5511911112222", null, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

        // Act
        var result = await _sut.ImportCsvAsync(stream);

        // Assert
        result.TotalRows.Should().Be(2);
        result.ImportedCount.Should().Be(1);
        result.FailedCount.Should().Be(1);

        result.ImportedContacts.Should().HaveCount(1);
        result.ImportedContacts[0].Name.Should().Be("Novo Um");

        result.Errors.Should().HaveCount(1);
        result.Errors[0].Field.Should().Be("Phone");
        result.Errors[0].Reason.Should().Contain("já está cadastrado no sistema");

        _contactRepoMock.Verify(r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Once);
        _contactRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportCsvAsync_ShouldReturnUtf8CsvBytesWithActiveContacts()
    {
        // Arrange
        var contacts = new List<Contact>
        {
            Contact.Create("Contato A", "a@teste.com", "(11) 98765-4321", "+5511987654321"),
            Contact.Create("Contato B", "b@teste.com", "(21) 98888-7777", "+5521988887777")
        };

        _contactRepoMock.Setup(r => r.GetActivePagedAsync(null, 1, 100000, "name", false, It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((contacts, 2));

        // Act
        var bytes = await _sut.ExportCsvAsync();

        // Assert
        bytes.Should().NotBeNullOrEmpty();
        var content = Encoding.UTF8.GetString(bytes);
        content.Should().Contain("Nome;Email;Telefone;DataCadastro");
        content.Should().Contain("Contato A");
        content.Should().Contain("a@teste.com");
        content.Should().Contain("Contato B");
    }
}
