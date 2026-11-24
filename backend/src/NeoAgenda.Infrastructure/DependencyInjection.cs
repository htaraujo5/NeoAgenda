using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeoAgenda.Domain.Repositories;
using NeoAgenda.Infrastructure.Data;
using NeoAgenda.Infrastructure.Repositories;

namespace NeoAgenda.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["DatabaseProvider"] ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
                               ?? "Data Source=neoagenda.db";

        services.AddDbContext<AppDbContext>(options =>
        {
            if (provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<ILgpdAuditRepository, LgpdAuditRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<Application.Interfaces.IAuthService, Services.AuthService>();
        services.AddHostedService<BackgroundServices.RecycleBinPurgeBackgroundService>();

        return services;
    }
}
