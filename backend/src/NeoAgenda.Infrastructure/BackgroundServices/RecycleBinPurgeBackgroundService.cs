using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoAgenda.Domain.Repositories;

namespace NeoAgenda.Infrastructure.BackgroundServices;

/// <summary>
/// Worker de segundo plano que executa periodicamente para expurgar fisicamente contatos
/// cuja janela de retenção de 30 dias na lixeira expirou (RN03).
/// </summary>
public class RecycleBinPurgeBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecycleBinPurgeBackgroundService> _logger;
    private readonly TimeSpan _checkInterval;

    public RecycleBinPurgeBackgroundService(
        IServiceScopeFactory scopeFactory, 
        ILogger<RecycleBinPurgeBackgroundService> logger,
        TimeSpan? checkInterval = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        // Padrão: a cada 1 hora em produção; nos testes ou ambiente inicial roda periodicamente
        _checkInterval = checkInterval ?? TimeSpan.FromHours(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de purga automática da lixeira iniciado. Intervalo de checagem: {Interval}", _checkInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IContactRepository>();

                var purgedCount = await repository.PurgeExpiredRecycleBinAsync(DateTime.UtcNow, stoppingToken);
                if (purgedCount > 0)
                {
                    _logger.LogInformation("Expurgo automático de lixeira concluído: {Count} contatos com retenção expirada (>30 dias) foram removidos permanentemente.", purgedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao executar rotina de expurgo automático da lixeira.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Serviço de purga automática da lixeira finalizado.");
    }
}
