using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NeoAgenda.Api.Middlewares;
using NeoAgenda.Application.Interfaces;
using NeoAgenda.Application.Services;
using NeoAgenda.Domain.Entities;
using NeoAgenda.Domain.Services;
using NeoAgenda.Infrastructure;
using NeoAgenda.Infrastructure.Data;
using NeoAgenda.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Configuração de Injeção de Dependências
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IPhoneNormalizer, PhoneNormalizer>();
builder.Services.AddScoped<ICsvContactParser, CsvContactParser>();
builder.Services.AddScoped<IContactService, ContactService>();

// Configuração de Autenticação JWT
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SuperSecretKeyForNeoAgendaSecureAuthentication2026!";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "NeoAgenda",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "NeoAgendaClient",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NeoAgenda API",
        Version = "v1",
        Description = "API RESTful em .NET 8 para gestão inteligente de contatos, com garantia de unicidade de telefone, exclusão com retenção de 30 dias, exclusão definitiva por LGPD, importação resiliente de CSV e segurança JWT."
    });

    // Suporte a Bearer Token no Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT gerado no endpoint /api/auth/login. Exemplo: 'Bearer {seu_token}'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configuração de CORS para permitir acesso do frontend Vue
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// Inicialização automática do Banco de Dados e Seed inicial de demonstração
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Seed de Usuário padrão para autenticação
    if (!db.Users.Any())
    {
        var adminPasswordHash = PasswordHasher.HashPassword("Admin@123");
        var adminUser = new User("admin", adminPasswordHash, "Administrador NeoAgenda", "Admin");
        db.Users.Add(adminUser);
        db.SaveChanges();
    }

    if (!db.Contacts.Any())
    {
        db.Contacts.AddRange(
            Contact.Create("Ana Beatriz", "ana.beatriz@exemplo.com", "(11) 98765-4321", "+5511987654321"),
            Contact.Create("Carlos Eduardo", "carlos.eduardo@exemplo.com", "(21) 99888-7766", "+5521998887766"),
            Contact.Create("Mariana Oliveira", "mariana.oliveira@exemplo.com", "(31) 3344-5566", "+553133445566")
        );
        db.SaveChanges();
    }
}

// Middleware de tratamento de exceções global (RFC 7807 ProblemDetails)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseMiddleware<PerformanceTelemetryMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NeoAgenda API v1");
    c.RoutePrefix = string.Empty; // Swagger na raiz da API para acesso direto
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Necessário para testes de integração com WebApplicationFactory
public partial class Program { }
