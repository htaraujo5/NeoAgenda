using FluentAssertions;
using NeoAgenda.Domain.Common;
using NeoAgenda.Domain.Entities;
using Xunit;

namespace NeoAgenda.UnitTests.Domain;

/// <summary>
/// Testes unitários para o ciclo de vida da entidade Contact, cobrindo criação, atualização,
/// soft delete e retenção de 30 dias.
/// </summary>
public class ContactEntityTests
{
    [Fact]
    public void Create_ShouldInstantiateValidContact_WithDefaultActiveState()
    {
        // Arrange & Act
        var contact = Contact.Create("João Silva", "joao@teste.com", "(11) 98765-4321", "+5511987654321");

        // Assert
        contact.Id.Should().NotBeEmpty();
        contact.Name.Should().Be("João Silva");
        contact.Email.Should().Be("joao@teste.com");
        contact.PhoneOriginal.Should().Be("(11) 98765-4321");
        contact.PhoneNormalized.Should().Be("+5511987654321");
        contact.IsDeleted.Should().BeFalse();
        contact.DeletedAt.Should().BeNull();
        contact.RetentionExpiresAt.Should().BeNull();
        contact.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void MarkAsDeleted_ShouldSetSoftDeleteFlags_AndCalculate30DaysRetention()
    {
        // Arrange
        var contact = Contact.Create("Maria Silva", "maria@teste.com", "(11) 98765-4321", "+5511987654321");
        var deletionTime = new DateTime(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);

        // Act
        contact.MarkAsDeleted(deletionTime);

        // Assert
        contact.IsDeleted.Should().BeTrue();
        contact.DeletedAt.Should().Be(deletionTime);
        contact.RetentionExpiresAt.Should().Be(deletionTime.AddDays(30));
    }

    [Fact]
    public void MarkAsDeleted_ShouldThrowDomainException_IfAlreadyDeleted()
    {
        // Arrange
        var contact = Contact.Create("Maria Silva", "maria@teste.com", "(11) 98765-4321", "+5511987654321");
        contact.MarkAsDeleted();

        // Act
        Action act = () => contact.MarkAsDeleted();

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*já se encontra na lixeira*");
    }

    [Fact]
    public void Restore_ShouldReactivateContact_WhenWithin30DaysWindow()
    {
        // Arrange
        var contact = Contact.Create("Carlos Lima", "carlos@teste.com", "(11) 98765-4321", "+5511987654321");
        var deletionTime = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
        contact.MarkAsDeleted(deletionTime);

        var restoreDate = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc); // 19 dias depois (dentro dos 30)

        // Act
        contact.Restore(restoreDate);

        // Assert
        contact.IsDeleted.Should().BeFalse();
        contact.DeletedAt.Should().BeNull();
        contact.RetentionExpiresAt.Should().BeNull();
        contact.UpdatedAt.Should().Be(restoreDate);
    }

    [Fact]
    public void Restore_ShouldThrowDomainException_WhenAfter30DaysRetentionWindow()
    {
        // Arrange
        var contact = Contact.Create("Carlos Lima", "carlos@teste.com", "(11) 98765-4321", "+5511987654321");
        var deletionTime = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        contact.MarkAsDeleted(deletionTime);

        var restoreDate = new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc); // 45 dias depois (expirado!)

        // Act
        Action act = () => contact.Restore(restoreDate);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*prazo de 30 dias para restauração do contato expirou*");
    }

    [Fact]
    public void CanBeRestored_ShouldReturnTrueWithin30Days_AndFalseAfter()
    {
        // Arrange
        var contact = Contact.Create("Ana Paula", "ana@teste.com", "(11) 98765-4321", "+5511987654321");
        var deletionTime = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        contact.MarkAsDeleted(deletionTime);

        var day25 = deletionTime.AddDays(25);
        var day31 = deletionTime.AddDays(31);

        // Act & Assert
        contact.CanBeRestored(day25).Should().BeTrue();
        contact.CanBeRestored(day31).Should().BeFalse();
    }

    [Fact]
    public void GetDaysRemainingInRecycleBin_ShouldCalculateRemainingDaysAccurately()
    {
        // Arrange
        var contact = Contact.Create("Lucas Bento", "lucas@teste.com", "(11) 98765-4321", "+5511987654321");
        var deletionTime = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        contact.MarkAsDeleted(deletionTime);

        var checkTime = deletionTime.AddDays(10); // 10 dias se passaram, restam 20

        // Act
        var daysRemaining = contact.GetDaysRemainingInRecycleBin(checkTime);

        // Assert
        daysRemaining.Should().Be(20);
    }
}
