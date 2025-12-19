using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Processamento;

/// <summary>
/// Agregado que mantém o histórico completo de processamentos de um funcionário em uma competência.
/// 
/// REGRAS:
/// - Mantém todas as versões (V1, V2, V3, ...) para auditoria
/// - A versão atual é sempre a última finalizada
/// - Versões anteriores são marcadas como "Superado"
/// - Nunca remove versões (imutabilidade do histórico)
/// </summary>
public sealed class HistoricoProcessamento
{
    private readonly List<ProcessamentoVersao> _versoes;

    /// <summary>
    /// Identificador do funcionário.
    /// </summary>
    public FuncionarioId FuncionarioId { get; }

    /// <summary>
    /// Competência do processamento.
    /// </summary>
    public Competencia Competencia { get; }

    /// <summary>
    /// Todas as versões de processamento (em ordem cronológica).
    /// </summary>
    public IReadOnlyList<ProcessamentoVersao> Versoes => _versoes.AsReadOnly();

    /// <summary>
    /// Versão atual (última finalizada, não superada).
    /// </summary>
    public ProcessamentoVersao? VersaoAtual => _versoes
        .Where(v => v.Status == StatusProcessamento.Finalizado)
        .OrderByDescending(v => v.Versao.Numero)
        .FirstOrDefault();

    /// <summary>
    /// Última versão (independente do status).
    /// </summary>
    public ProcessamentoVersao? UltimaVersao => _versoes
        .OrderByDescending(v => v.Versao.Numero)
        .FirstOrDefault();

    /// <summary>
    /// Quantidade total de versões.
    /// </summary>
    public int TotalVersoes => _versoes.Count;

    /// <summary>
    /// Indica se houve reprocessamento.
    /// </summary>
    public bool HouveReprocessamento => _versoes.Count > 1;

    private HistoricoProcessamento(FuncionarioId funcionarioId, Competencia competencia)
    {
        FuncionarioId = funcionarioId ?? throw new ArgumentNullException(nameof(funcionarioId));
        Competencia = competencia ?? throw new ArgumentNullException(nameof(competencia));
        _versoes = new List<ProcessamentoVersao>();
    }

    /// <summary>
    /// Cria um novo histórico de processamento.
    /// </summary>
    public static HistoricoProcessamento Criar(FuncionarioId funcionarioId, Competencia competencia)
    {
        return new HistoricoProcessamento(funcionarioId, competencia);
    }

    /// <summary>
    /// Reconstitui um histórico a partir de versões existentes.
    /// </summary>
    public static HistoricoProcessamento Reconstituir(
        FuncionarioId funcionarioId,
        Competencia competencia,
        IEnumerable<ProcessamentoVersao> versoes)
    {
        var historico = new HistoricoProcessamento(funcionarioId, competencia);

        foreach (var versao in versoes.OrderBy(v => v.Versao.Numero))
        {
            if (versao.FuncionarioId != funcionarioId)
                throw new InvalidOperationException("Versão pertence a outro funcionário.");

            if (versao.Competencia != competencia)
                throw new InvalidOperationException("Versão pertence a outra competência.");

            historico._versoes.Add(versao);
        }

        return historico;
    }

    /// <summary>
    /// Inicia o primeiro processamento.
    /// </summary>
    public ProcessamentoVersao IniciarPrimeiroProcessamento(DateTime timestampInicio, string? usuarioId = null)
    {
        if (_versoes.Any())
            throw new InvalidOperationException("Já existe processamento para esta competência. Use IniciarReprocessamento.");

        var processamento = ProcessamentoVersao.IniciarPrimeiro(
            FuncionarioId,
            Competencia,
            timestampInicio,
            usuarioId);

        _versoes.Add(processamento);
        return processamento;
    }

    /// <summary>
    /// Inicia um reprocessamento (nova versão).
    /// </summary>
    public ProcessamentoVersao IniciarReprocessamento(
        MotivoReprocessamento motivo,
        DateTime timestampInicio,
        string? usuarioId = null)
    {
        var versaoAtual = VersaoAtual;

        if (versaoAtual is null)
            throw new InvalidOperationException("Não há versão finalizada para reprocessar.");

        var novoProcessamento = ProcessamentoVersao.IniciarReprocessamento(
            versaoAtual,
            motivo,
            timestampInicio,
            usuarioId);

        _versoes.Add(novoProcessamento);
        return novoProcessamento;
    }

    /// <summary>
    /// Finaliza o processamento em andamento.
    /// </summary>
    public ProcessamentoVersao FinalizarProcessamentoAtual(
        ProcessamentoVersao processamentoFinalizado,
        DateTime timestampFinalizacao)
    {
        if (processamentoFinalizado is null)
            throw new ArgumentNullException(nameof(processamentoFinalizado));

        // Encontra e substitui o processamento na lista
        var index = _versoes.FindIndex(v => v.Id == processamentoFinalizado.Id);
        if (index < 0)
            throw new InvalidOperationException("Processamento não encontrado no histórico.");

        // Marca versões anteriores como superadas
        for (int i = 0; i < _versoes.Count; i++)
        {
            if (i != index && _versoes[i].Status == StatusProcessamento.Finalizado)
            {
                _versoes[i] = _versoes[i].MarcarComoSuperado(timestampFinalizacao);
            }
        }

        // Atualiza a versão finalizada
        _versoes[index] = processamentoFinalizado;

        return processamentoFinalizado;
    }

    /// <summary>
    /// Obtém uma versão específica pelo número.
    /// </summary>
    public ProcessamentoVersao? ObterVersao(VersaoProcessamento versao)
    {
        if (versao is null)
            throw new ArgumentNullException(nameof(versao));

        return _versoes.FirstOrDefault(v => v.Versao == versao);
    }

    /// <summary>
    /// Obtém uma versão específica pelo número.
    /// </summary>
    public ProcessamentoVersao? ObterVersao(int numeroVersao)
    {
        return _versoes.FirstOrDefault(v => v.Versao.Numero == numeroVersao);
    }

    /// <summary>
    /// Obtém todas as versões finalizadas.
    /// </summary>
    public IReadOnlyList<ProcessamentoVersao> ObterVersoesFinalizadas()
    {
        return _versoes
            .Where(v => v.Status == StatusProcessamento.Finalizado || v.Status == StatusProcessamento.Superado)
            .OrderBy(v => v.Versao.Numero)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Verifica se existe processamento em andamento.
    /// </summary>
    public bool TemProcessamentoEmAndamento()
    {
        return _versoes.Any(v => v.Status == StatusProcessamento.EmProcessamento);
    }

    /// <summary>
    /// Obtém o processamento em andamento (se houver).
    /// </summary>
    public ProcessamentoVersao? ObterProcessamentoEmAndamento()
    {
        return _versoes.FirstOrDefault(v => v.Status == StatusProcessamento.EmProcessamento);
    }

    /// <summary>
    /// Compara duas versões e retorna as diferenças.
    /// </summary>
    public DiferencaVersoes? CompararVersoes(VersaoProcessamento versaoA, VersaoProcessamento versaoB)
    {
        var procA = ObterVersao(versaoA);
        var procB = ObterVersao(versaoB);

        if (procA?.Resultado is null || procB?.Resultado is null)
            return null;

        return DiferencaVersoes.Criar(procA, procB);
    }

    public override string ToString() =>
        $"Histórico {FuncionarioId} - {Competencia}: {TotalVersoes} versão(ões)";
}

/// <summary>
/// Representa as diferenças entre duas versões de processamento.
/// </summary>
public sealed class DiferencaVersoes
{
    public ProcessamentoVersao VersaoAnterior { get; }
    public ProcessamentoVersao VersaoNova { get; }
    public decimal DiferencaBruto { get; }
    public decimal DiferencaInss { get; }
    public decimal DiferencaIrrf { get; }
    public decimal DiferencaFgts { get; }
    public decimal DiferencaConsignados { get; }
    public decimal DiferencaLiquido { get; }

    private DiferencaVersoes(
        ProcessamentoVersao versaoAnterior,
        ProcessamentoVersao versaoNova)
    {
        VersaoAnterior = versaoAnterior;
        VersaoNova = versaoNova;

        var resA = versaoAnterior.Resultado!;
        var resB = versaoNova.Resultado!;

        DiferencaBruto = resB.SalarioBruto.Valor - resA.SalarioBruto.Valor;
        DiferencaInss = resB.ValorInss.Valor - resA.ValorInss.Valor;
        DiferencaIrrf = resB.ValorIrrf.Valor - resA.ValorIrrf.Valor;
        DiferencaFgts = resB.ValorFgts.Valor - resA.ValorFgts.Valor;
        DiferencaConsignados = resB.ValorConsignados.Valor - resA.ValorConsignados.Valor;
        DiferencaLiquido = resB.SalarioLiquido.Valor - resA.SalarioLiquido.Valor;
    }

    public static DiferencaVersoes Criar(ProcessamentoVersao versaoAnterior, ProcessamentoVersao versaoNova)
    {
        if (versaoAnterior?.Resultado is null)
            throw new ArgumentException("Versão anterior não tem resultado.", nameof(versaoAnterior));

        if (versaoNova?.Resultado is null)
            throw new ArgumentException("Versão nova não tem resultado.", nameof(versaoNova));

        return new DiferencaVersoes(versaoAnterior, versaoNova);
    }

    public bool HouveMudanca =>
        DiferencaBruto != 0 ||
        DiferencaInss != 0 ||
        DiferencaIrrf != 0 ||
        DiferencaFgts != 0 ||
        DiferencaConsignados != 0 ||
        DiferencaLiquido != 0;

    public override string ToString() =>
        $"Diferença {VersaoAnterior.Versao} → {VersaoNova.Versao}: Líquido {DiferencaLiquido:+0.00;-0.00;0}";
}
