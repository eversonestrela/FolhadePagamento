using FolhadePagamento.Aplicacao.Lotes;
using FolhadePagamento.Jobs.Servicos;

namespace FolhadePagamento.Jobs.Workers;

/// <summary>
/// Worker que processa lotes de folha de pagamento em background.
/// Monitora lotes pendentes e os processa sequencialmente.
/// </summary>
public class ProcessadorLoteWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessadorLoteWorker> _logger;
    private readonly TimeSpan _intervaloVerificacao = TimeSpan.FromSeconds(10);

    public ProcessadorLoteWorker(
        IServiceProvider serviceProvider,
        ILogger<ProcessadorLoteWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProcessadorLoteWorker iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessarLotesPendentesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no ciclo de processamento de lotes");
            }

            await Task.Delay(_intervaloVerificacao, stoppingToken);
        }

        _logger.LogInformation("ProcessadorLoteWorker finalizado");
    }

    private async Task ProcessarLotesPendentesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var loteRepositorio = scope.ServiceProvider.GetRequiredService<ILoteRepositorio>();
        var processador = scope.ServiceProvider.GetRequiredService<IProcessadorLote>();

        // Buscar lotes ativos (pendentes ou em processamento)
        var lotesAtivos = await loteRepositorio.ListarLotesAtivosAsync(cancellationToken);

        foreach (var loteResumo in lotesAtivos)
        {
            if (cancellationToken.IsCancellationRequested) break;

            // Processar lotes pendentes
            if (loteResumo.Status == "Pendente")
            {
                _logger.LogInformation("Iniciando processamento do lote {LoteId}", loteResumo.LoteId);
                await processador.ProcessarLoteAsync(loteResumo.LoteId, cancellationToken);
            }
            // Continuar lotes que estavam em processamento (recuperação de falha)
            else if (loteResumo.Status == "EmProcessamento")
            {
                _logger.LogInformation("Retomando processamento do lote {LoteId}", loteResumo.LoteId);
                await processador.ProcessarLoteAsync(loteResumo.LoteId, cancellationToken);
            }
        }
    }
}
