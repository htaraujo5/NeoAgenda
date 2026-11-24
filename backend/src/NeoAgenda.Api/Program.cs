using Microsoft.OpenApi.Models;
using NeoAgenda.Api.Middlewares;
using NeoAgenda.Application.Interfaces;
using NeoAgenda.Application.Services;
using NeoAgenda.Domain.Entities;
using NeoAgenda.Domain.Services;
using NeoAgenda.Infrastructure;
using NeoAgenda.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Configuração de Injeção de Dependências
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IPhoneNormalizer, PhoneNormalizer>();
builder.Services.AddScoped<ICsvContactParser, CsvContactParser>();
builder.Services.AddScoped<IContactService, ContactService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NeoAgenda API",
        Version = "v1",
        Description = "API RESTful em .NET 8 para gestão inteligente de contatos, com garantia de unicidade de telefone, exclusão com retenção de 30 dias, exclusão definitiva por LGPD e importação resiliente de CSV."
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

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NeoAgenda API v1");
    c.RoutePrefix = string.Empty; // Swagger na raiz da API para acesso direto
});

app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Necessário para testes de integração com WebApplicationFactory
public partial class Program { }
