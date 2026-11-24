using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeoAgenda.Domain.Entities;

namespace NeoAgenda.Infrastructure.Data.Configurations;

public class LgpdAuditConfiguration : IEntityTypeConfiguration<LgpdComplianceAudit>
{
    public void Configure(EntityTypeBuilder<LgpdComplianceAudit> builder)
    {
        builder.ToTable("LgpdComplianceAudits");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ContactHash)
               .HasMaxLength(64)
               .IsRequired();

        builder.Property(a => a.LegalBasis)
               .HasMaxLength(150)
               .IsRequired();

        builder.Property(a => a.RequestedBy)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(a => a.ExecutedAt)
               .IsRequired();
    }
}
