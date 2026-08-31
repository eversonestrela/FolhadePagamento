using FolhadePagamento.Aplicacao.Lotes;
using FolhadePagamento.Aplicacao.Portas;

namespace FolhadePagamento.Jobs.Servicos;

/// <summary>
/// Interface para o serviço de processamento de lotes.
/// </summary>
public interface IProcessadorLote
{
    /// <summary>
    /// Processa todos os itens pendentes de um lote.
    /// </summary>
    Task ProcessarLoteAsync(Guid loteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processa um único item do lote.
    /// </summary>
    Task<ResultadoProcessamentoItem> ProcessarItemAsync(
        ItemLotePersistencia item,
        int competenciaAno,
        int competenciaMes,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado do processamento de um item.
/// </summary>
public record ResultadoProcessamentoItem
{
    public required bool Sucesso { get; init; }
    public Guid? ProcessamentoVersaoId { get; init; }
    public int? VersaoNumero { get; init; }
    public string? MensagemErro { get; init; }
}

/// <summary>
/// Implementação do processador de lotes.
/// Processa cada funcionário isoladamente - falha de um não afeta os outros.
/// </summary>
public class ProcessadorLote : IProcessadorLote
{
    private readonly ILoteRepositorio _loteRepositorio;
    private readonly IFuncionarioRepositorio _funcionarioRepositorio;
    private readonly IProcessamentoRepositorio _processamentoRepositorio;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly ILogger<ProcessadorLote> _logger;

    public ProcessadorLote(
        ILoteRepositorio loteRepositorio,
        IFuncionarioRepositorio funcionarioRepositorio,
        IProcessamentoRepositorio processamentoRepositorio,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        ILogger<ProcessadorLote> logger)
    {
        _loteRepositorio = loteRepositorio;
        _funcionarioRepositorio = funcionarioRepositorio;
        _processamentoRepositorio = processamentoRepositorio;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _logger = logger;
    }

    public async Task ProcessarLoteAsync(Guid loteId, CancellationToken cancellationToken = default)
    {
        var lote = await _loteRepositorio.ObterLotePorIdAsync(loteId, cancellationToken);
        if (lote is null)
        {
            _logger.LogWarning("Lote {LoteId} não encontrado", loteId);
            return;
        }

        _logger.LogInformation(
            "Iniciando processamento do lote {LoteId} - Competência {Ano}/{Mes:D2} - {Total} itens",
            loteId, lote.CompetenciaAno, lote.CompetenciaMes, lote.TotalItens);

        // Marcar lote como em processamento
        await _loteRepositorio.AtualizarStatusLoteAsync(
            loteId,
            StatusLote.EmProcessamento,
            iniciadoEm: DateTime.UtcNow,
            cancellationToken: cancellationToken);

        var concluidos = 0;
        var falhas = 0;
        var ignorados = 0;

        // Processar itens pendentes um a um
        while (!cancellationToken.IsCancellationRequested)
        {
            var item = await _loteRepositorio.ObterProximoItemPendenteAsync(loteId, cancellationToken);
            if (item is null) break;

            var resultado = await ProcessarItemAsync(
                item,
                lote.CompetenciaAno,
                lote.CompetenciaMes,
                cancellationToken);

            if (resultado.Sucesso)
            {
                concluidos++;
            }
            else if (resultado.MensagemErro?.Contains("inativo") == true)
            {
                ignorados++;
            }
            else
            {
                falhas++;
            }

            // Atualizar contadores periodicamente
            await _loteRepositorio.AtualizarContadoresLoteAsync(
                loteId, concluidos, falhas, ignorados, cancellationToken);
        }

        // Determinar status final
        var statusFinal = falhas > 0 ? StatusLote.ConcluidoComFalhas : StatusLote.Concluido;

        await _loteRepositorio.AtualizarStatusLoteAsync(
            loteId,
            statusFinal,
            concluidoEm: DateTime.UtcNow,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Lote {LoteId} concluído - Sucesso: {Concluidos}, Falhas: {Falhas}, Ignorados: {Ignorados}",
            loteId, concluidos, falhas, ignorados);
    }

    public async Task<ResultadoProcessamentoItem> ProcessarItemAsync(
        ItemLotePersistencia item,
        int competenciaAno,
        int competenciaMes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Marcar item como em processamento
            await _loteRepositorio.IniciarProcessamentoItemAsync(item.ItemLoteId, cancellationToken);

            // Verificar se funcionário existe e está ativo
            var funcionario = await _funcionarioRepositorio.ObterPorIdAsync(item.FuncionarioId, cancellationToken);
            if (funcionario is null)
            {
                await _loteRepositorio.ConcluirItemComFalhaAsync(
                    item.ItemLoteId,
                    "Funcionário não encontrado",
                    cancellationToken);

                return new ResultadoProcessamentoItem
                {
                    Sucesso = false,
                    MensagemErro = "Funcionário não encontrado"
                };
            }

            if (!funcionario.Ativo)
            {
                await _loteRepositorio.AtualizarItemAsync(
                    item.ItemLoteId,
                    StatusItemLote.Ignorado,
                    mensagemErro: "Funcionário inativo",
                    concluidoEm: DateTime.UtcNow,
                    cancellationToken: cancellationToken);

                return new ResultadoProcessamentoItem
                {
                    Sucesso = false,
                    MensagemErro = "Funcionário inativo"
                };
            }

            // Verificar e superar versão anterior se existir
            var versaoAtual = await _processamentoRepositorio.ObterVersaoAtualAsync(
                item.FuncionarioId, competenciaAno, competenciaMes, cancellationToken);

            if (versaoAtual is not null)
            {
                await _processamentoRepositorio.MarcarComoSuperadoAsync(
                    versaoAtual.ProcessamentoVersaoId,
                    DateTime.UtcNow,
                    cancellationToken);
            }

            // Obter próximo número de versão
            var numeroVersao = await _processamentoRepositorio.ObterProximoNumeroVersaoAsync(
                item.FuncionarioId, competenciaAno, competenciaMes, cancellationToken);

            // ================================================================
            // AQUI SERIA A CHAMADA AO CORE PARA CALCULAR
            // Em produção: var resultado = _casoDeUso.Calcular(funcionario, ...);
            // 
            // Por enquanto, simulamos valores para demonstrar o fluxo
            // ================================================================
            var salarioBruto = funcionario.SalarioBase;
            var valorInss = Math.Round(salarioBruto * 0.11m, 2);
            var valorIrrf = Math.Round((salarioBruto - valorInss) * 0.075m, 2);
            var valorFgts = Math.Round(salarioBruto * 0.08m, 2);
            var totalDescontos = valorInss + valorIrrf;
            var salarioLiquido = salarioBruto - totalDescontos;

            var processamentoVersaoId = Guid.NewGuid();
            var resultadoCalculoId = Guid.NewGuid();
            var agora = DateTime.UtcNow;

            var processamento = new ProcessamentoPersistencia
            {
                ProcessamentoVersaoId = processamentoVersaoId,
                FuncionarioId = item.FuncionarioId,
                CompetenciaAno = competenciaAno,
                CompetenciaMes = competenciaMes,
                VersaoNumero = numeroVersao,
                VersaoAnteriorId = versaoAtual?.ProcessamentoVersaoId,
                Status = "Finalizado",
                IniciadoEm = agora,
                FinalizadoEm = agora,
                MotivoReprocessamento = versaoAtual is not null ? "Reprocessamento em lote" : null,
                Resultado = new ResultadoPersistencia
                {
                    ResultadoCalculoId = resultadoCalculoId,
                    SalarioBruto = salarioBruto,
                    ValorInss = valorInss,
                    ValorIrrf = valorIrrf,
                    ValorFgts = valorFgts,
                    ValorConsignados = 0,
                    TotalDescontos = totalDescontos,
                    SalarioLiquido = salarioLiquido,
                    TotalEncargosPatronais = valorFgts,
                    CustoTotalEmpregador = salarioBruto + valorFgts,
                    CalculadoEm = agora,
                    DetalheInss = new DetalheInssPersistencia
                    {
                        DetalheInssId = Guid.NewGuid(),
                        BaseCalculo = salarioBruto,
                        TabelaIdUsada = "INSS_2024_LOTE",
                        AliquotaEfetiva = 11m,
                        TetoAplicado = false
                    },
                    DetalheFgts = new DetalheFgtsPersistencia
                    {
                        DetalheFgtsId = Guid.NewGuid(),
                        BaseCalculo = salarioBruto,
                        TabelaIdUsada = "FGTS_2024",
                        AliquotaAplicada = 8m,
                        TipoContribuinte = "Normal"
                    }
                }
            };

            await _processamentoRepositorio.SalvarProcessamentoAsync(processamento, cancellationToken);

            // Marcar item como sucesso
            await _loteRepositorio.ConcluirItemComSucessoAsync(
                item.ItemLoteId,
                processamentoVersaoId,
                numeroVersao,
                cancellationToken);

            _logger.LogDebug(
                "Item {ItemId} processado com sucesso - Funcionário {FuncId} - Versão {Versao}",
                item.ItemLoteId, item.FuncionarioId, numeroVersao);

            return new ResultadoProcessamentoItem
            {
                Sucesso = true,
                ProcessamentoVersaoId = processamentoVersaoId,
                VersaoNumero = numeroVersao
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar item {ItemId}", item.ItemLoteId);

            await _loteRepositorio.ConcluirItemComFalhaAsync(
                item.ItemLoteId,
                ex.Message,
                cancellationToken);

            return new ResultadoProcessamentoItem
            {
                Sucesso = false,
                MensagemErro = ex.Message
            };
        }
    }
}
