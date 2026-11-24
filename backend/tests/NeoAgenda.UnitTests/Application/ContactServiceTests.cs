using FluentAssertions;
using Moq;
using NeoAgenda.Application.DTOs;
using NeoAgenda.Application.Services;
using NeoAgenda.Domain.Common;
using NeoAgenda.Domain.Entities;
using NeoAgenda.Domain.Repositories;
using NeoAgenda.Domain.Services;
using Xunit;

namespace NeoAgenda.UnitTests.Application;

public class ContactServiceTests
{
    private readonly Mock<IContactRepository> _contactRepoMock = new();
    private readonly Mock<ILgpdAuditRepository> _lgpdRepoMock = new();
    private readonly Mock<IPhoneNormalizer> _normalizerMock = new();
    private readonly ContactService _sut;

    public ContactServiceTests()
    {
        _sut = new ContactService(_contactRepoMock.Object, _lgpdRepoMock.Object, _normalizerMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistContact_WhenDataIsValidAndPhoneIsUnique()
    {
        // Arrange
        var dto = new CreateContactDto("Novo Contato", "novo@teste.com", "(11) 98765-4321");
        var canonicalPhone = "+5511987654321";

        _normalizerMock.Setup(n => n.Normalize(dto.Phone)).Returns(canonicalPhone);
        _contactRepoMock.Setup(r => r.ExistsByNormalizedPhoneAsync(canonicalPhone, null, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(dto.Name);
        result.Email.Should().Be(dto.Email);
        result.PhoneNormalized.Should().Be(canonicalPhone);

        _contactRepoMock.Verify(r => r.AddAsync(It.Is<Contact>(c => c.PhoneNormalized == canonicalPhone), It.IsAny<CancellationToken>()), Times.Once);
        _contactRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowDomainException_WhenPhoneAlreadyExists()
    {
        // Arrange
        var dto = new CreateContactDto("Contato Duplicado", "dupl@teste.com", "(11) 98765-4321");
        var canonicalPhone = "+5511987654321";

        _normalizerMock.Setup(n => n.Normalize(dto.Phone)).Returns(canonicalPhone);
        _contactRepoMock.Setup(r => r.ExistsByNormalizedPhoneAsync(canonicalPhone, null, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
                 .WithMessage("*Já existe um contato ativo cadastrado com o telefone informado*");

        _contactRepoMock.Verify(r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SoftDeleteAsync_ShouldMarkContactAsDeleted_AndPersist()
    {
        // Arrange
        var contact = Contact.Create("Teste Delete", "del@teste.com", "(11) 98765-4321", "+5511987654321");
        _contactRepoMock.Setup(r => r.GetByIdAsync(contact.Id, false, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(contact);

        // Act
        var result = await _sut.SoftDeleteAsync(contact.Id);

        // Assert
        result.Should().NotBeNull();
        contact.IsDeleted.Should().BeTrue();
        _contactRepoMock.Verify(r => r.Update(contact), Times.Once);
        _contactRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PurgeLgpdAsync_ShouldDeletePermanently_AndRecordAnonymousAudit()
    {
        // Arrange
        var contact = Contact.Create("Cliente LGPD", "cliente@privacidade.com", "(11) 98765-4321", "+5511987654321");
        _contactRepoMock.Setup(r => r.GetByIdAsync(contact.Id, true, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(contact);

        // Act
        var result = await _sut.PurgeLgpdAsync(contact.Id, "portal_titular");

        // Assert
        result.Should().NotBeNull();
        result.Protocol.Should().StartWith("LGPD-");

        _lgpdRepoMock.Verify(r => r.AddAsync(It.Is<LgpdComplianceAudit>(a => a.RequestedBy == "portal_titular" && !string.IsNullOrEmpty(a.ContactHash)), It.IsAny<CancellationToken>()), Times.Once);
        _contactRepoMock.Verify(r => r.DeletePermanently(contact), Times.Once);
        _contactRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
