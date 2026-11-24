using Microsoft.EntityFrameworkCore;
using NeoAgenda.Domain.Entities;
using NeoAgenda.Infrastructure.Data.Configurations;

namespace NeoAgenda.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<LgpdComplianceAudit> LgpdAudits => Set<LgpdComplianceAudit>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();
    public DbSet<RequestMetric> RequestMetrics => Set<RequestMetric>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ContactConfiguration(Database.ProviderName));
        modelBuilder.ApplyConfiguration(new LgpdAuditConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new AccessLogConfiguration());
        modelBuilder.ApplyConfiguration(new RequestMetricConfiguration());
    }
}
