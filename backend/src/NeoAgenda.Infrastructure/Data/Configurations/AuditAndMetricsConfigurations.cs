using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeoAgenda.Domain.Entities;

namespace NeoAgenda.Infrastructure.Data.Configurations;

public class AccessLogConfiguration : IEntityTypeConfiguration<AccessLog>
{
    public void Configure(EntityTypeBuilder<AccessLog> builder)
    {
        builder.ToTable("AccessLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Username).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Action).IsRequired().HasMaxLength(50);
        builder.Property(l => l.IpAddress).HasMaxLength(50);
        builder.Property(l => l.UserAgent).HasMaxLength(500);
        builder.Property(l => l.Timestamp).IsRequired();
    }
}

public class RequestMetricConfiguration : IEntityTypeConfiguration<RequestMetric>
{
    public void Configure(EntityTypeBuilder<RequestMetric> builder)
    {
        builder.ToTable("RequestMetrics");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Method).IsRequired().HasMaxLength(10);
        builder.Property(m => m.Path).IsRequired().HasMaxLength(250);
        builder.Property(m => m.Timestamp).IsRequired();
    }
}
