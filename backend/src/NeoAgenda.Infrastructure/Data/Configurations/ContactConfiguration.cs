using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeoAgenda.Domain.Entities;

namespace NeoAgenda.Infrastructure.Data.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    private readonly string? _providerName;

    public ContactConfiguration(string? providerName = null)
    {
        _providerName = providerName;
    }

    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
               .HasMaxLength(150)
               .IsRequired();

        builder.Property(c => c.Email)
               .HasMaxLength(254)
               .IsRequired();

        builder.Property(c => c.PhoneOriginal)
               .HasMaxLength(30)
               .IsRequired();

        builder.Property(c => c.PhoneNormalized)
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(c => c.CreatedAt)
               .IsRequired();

        builder.Property(c => c.UpdatedAt);

        builder.Property(c => c.CreatedBy)
               .HasMaxLength(100)
               .HasDefaultValue("system")
               .IsRequired();

        builder.Property(c => c.IsDeleted)
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(c => c.DeletedAt);
        builder.Property(c => c.RetentionExpiresAt);

        // Filtered Unique Index para unicidade apenas entre contatos ativos
        var index = builder.HasIndex(c => c.PhoneNormalized)
                           .IsUnique();

        if (_providerName != null && _providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            index.HasFilter("IsDeleted = 0");
        }
        else
        {
            index.HasFilter("\"IsDeleted\" = false");
        }

        // Filtro global do EF Core: consultas padrão ignoram contatos na lixeira
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
