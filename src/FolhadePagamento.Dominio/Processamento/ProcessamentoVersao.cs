using FolhadePagamento.Dominio.Folha;
using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Processamento;

/// <summary>
/// Representa um resultado de processamento versionado.
/// 
/// IMUTABILIDADE GARANTIDA:
/// - Uma vez finalizado, o resultado NÃO pode ser alterado
/// - Qualquer correção gera uma NOVA versão
/// - Histórico completo é mantido para auditoria
/// 
/// Esta classe é o artefato imutável que representa uma execução
/// específica do cálculo de folha para um funcionário em uma competência.
/// </summary>
public sealed class ProcessamentoVersao
{
    /// <summary>
    /// Identificador único deste processamento.
    /// </summary>
    public ProcessamentoId Id { get; }

    /// <summary>
    /// Identificador do funcionário processado.
    /// </summary>
    public FuncionarioId FuncionarioId { get; }

    /// <summary>
    /// Competência (ano-mês) do processamento.
    /// </summary>
    public Competencia Competencia { get; }

    /// <summary>
    /// Versão deste processamento (V1, V2, V3, ...).
    /// </summary>
    public VersaoProcessamento Versao { get; }

    /// <summary>
    /// Status atual do processamento.
    /// </summary>
    public StatusProcessamento Status { get; private set; }

    /// <summary>
    /// Resultado do cálculo (imutável após finalização).
    /// </summary>
    public ResultadoCalculo? Resultado { get; }

    /// <summary>
    /// Timestamp de quando o processamento foi iniciado.
    /// </summary>
    public DateTime IniciadoEm { get; }

    /// <summary>
    /// Timestamp de quando o processamento foi finalizado.
    /// Null se ainda em processamento ou cancelado.
    /// </summary>
    public DateTime? FinalizadoEm { get; private set; }

    /// <summary>
    /// Timestamp de quando o processamento foi superado.
    /// Null se ainda é a versão atual.
    /// </summary>
    public DateTime? SuperadoEm { get; private set; }

    /// <summary>
    /// Motivo do reprocessamento (para V2, V3, etc.).
    /// Null para a primeira versão.
    /// </summary>
    public MotivoReprocessamento? MotivoReprocessamento { get; }

    /// <summary>
    /// Referência à versão anterior (se houver).
    /// Null para a primeira versão.
    /// </summary>
    public ProcessamentoId? VersaoAnteriorId { get; }

    /// <summary>
    /// Identificador do usuário que executou o processamento.
    /// </summary>
    public string? UsuarioId { get; }

    /// <summary>
    /// Hash do resultado para verificação de integridade.
    /// </summary>
    public string? HashResultado { get; private set; }

    private ProcessamentoVersao(
        ProcessamentoId id,
        FuncionarioId funcionarioId,
        Competencia competencia,
        VersaoProcessamento versao,
        StatusProcessamento status,
        ResultadoCalculo? resultado,
        DateTime iniciadoEm,
        DateTime? finalizadoEm,
        MotivoReprocessamento? motivoReprocessamento,
        ProcessamentoId? versaoAnteriorId,
        string? usuarioId)
    {
        Id = id;
        FuncionarioId = funcionarioId;
        Competencia = competencia;
        Versao = versao;
        Status = status;
        Resultado = resultado;
        IniciadoEm = iniciadoEm;
        FinalizadoEm = finalizadoEm;
        MotivoReprocessamento = motivoReprocessamento;
        VersaoAnteriorId = versaoAnteriorId;
        UsuarioId = usuarioId;
    }

    /// <summary>
    /// Inicia um novo processamento (primeira versão).
    /// </summary>
    public static ProcessamentoVersao IniciarPrimeiro(
        FuncionarioId funcionarioId,
        Competencia competencia,
        DateTime timestampInicio,
        string? usuarioId = null)
    {
        if (funcionarioId is null)
            throw new ArgumentNullException(nameof(funcionarioId));

        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        return new ProcessamentoVersao(
            id: ProcessamentoId.Novo(),
            funcionarioId: funcionarioId,
            competencia: competencia,
            versao: VersaoProcessamento.Primeira,
            status: StatusProcessamento.EmProcessamento,
            resultado: null,
            iniciadoEm: timestampInicio,
            finalizadoEm: null,
            motivoReprocessamento: null,
            versaoAnteriorId: null,
            usuarioId: usuarioId);
    }

    /// <summary>
    /// Inicia um reprocessamento (nova versão).
    /// </summary>
    public static ProcessamentoVersao IniciarReprocessamento(
        ProcessamentoVersao versaoAnterior,
        MotivoReprocessamento motivo,
        DateTime timestampInicio,
        string? usuarioId = null)
    {
        if (versaoAnterior is null)
            throw new ArgumentNullException(nameof(versaoAnterior));

        if (motivo is null)
            throw new ArgumentNullException(nameof(motivo));

        if (versaoAnterior.Status != StatusProcessamento.Finalizado)
            throw new InvalidOperationException("Só é possível reprocessar versões finalizadas.");

        return new ProcessamentoVersao(
            id: ProcessamentoId.Novo(),
            funcionarioId: versaoAnterior.FuncionarioId,
            competencia: versaoAnterior.Competencia,
            versao: versaoAnterior.Versao.Proxima(),
            status: StatusProcessamento.EmProcessamento,
            resultado: null,
            iniciadoEm: timestampInicio,
            finalizadoEm: null,
            motivoReprocessamento: motivo,
            versaoAnteriorId: versaoAnterior.Id,
            usuarioId: usuarioId);
    }

    /// <summary>
    /// Cria um processamento já finalizado (para reconstrução ou testes).
    /// </summary>
    public static ProcessamentoVersao CriarFinalizado(
        ProcessamentoId id,
        FuncionarioId funcionarioId,
        Competencia competencia,
        VersaoProcessamento versao,
        ResultadoCalculo resultado,
        DateTime iniciadoEm,
        DateTime finalizadoEm,
        MotivoReprocessamento? motivoReprocessamento = null,
        ProcessamentoId? versaoAnteriorId = null,
        string? usuarioId = null)
    {
        if (id is null) throw new ArgumentNullException(nameof(id));
        if (funcionarioId is null) throw new ArgumentNullException(nameof(funcionarioId));
        if (competencia is null) throw new ArgumentNullException(nameof(competencia));
        if (versao is null) throw new ArgumentNullException(nameof(versao));
        if (resultado is null) throw new ArgumentNullException(nameof(resultado));

        var processamento = new ProcessamentoVersao(
            id: id,
            funcionarioId: funcionarioId,
            competencia: competencia,
            versao: versao,
            status: StatusProcessamento.Finalizado,
            resultado: resultado,
            iniciadoEm: iniciadoEm,
            finalizadoEm: finalizadoEm,
            motivoReprocessamento: motivoReprocessamento,
            versaoAnteriorId: versaoAnteriorId,
            usuarioId: usuarioId);

        processamento.HashResultado = processamento.GerarHashResultado();
        return processamento;
    }

    /// <summary>
    /// Finaliza o processamento com o resultado do cálculo.
    /// ATENÇÃO: Após finalizado, o resultado é IMUTÁVEL.
    /// </summary>
    public ProcessamentoVersao Finalizar(ResultadoCalculo resultado, DateTime timestampFinalizacao)
    {
        if (resultado is null)
            throw new ArgumentNullException(nameof(resultado));

        if (Status != StatusProcessamento.EmProcessamento)
            throw new InvalidOperationException($"Não é possível finalizar processamento com status {Status}.");

        // Valida que o resultado pertence ao mesmo funcionário e competência
        if (resultado.FuncionarioId != FuncionarioId)
            throw new InvalidOperationException("Resultado pertence a outro funcionário.");

        if (resultado.Competencia != Competencia)
            throw new InvalidOperationException("Resultado pertence a outra competência.");

        // Retorna nova instância (imutabilidade)
        var finalizado = new ProcessamentoVersao(
            id: Id,
            funcionarioId: FuncionarioId,
            competencia: Competencia,
            versao: Versao,
            status: StatusProcessamento.Finalizado,
            resultado: resultado,
            iniciadoEm: IniciadoEm,
            finalizadoEm: timestampFinalizacao,
            motivoReprocessamento: MotivoReprocessamento,
            versaoAnteriorId: VersaoAnteriorId,
            usuarioId: UsuarioId);

        finalizado.HashResultado = finalizado.GerarHashResultado();
        return finalizado;
    }

    /// <summary>
    /// Cancela o processamento.
    /// Só pode ser chamado se ainda está em processamento.
    /// </summary>
    public ProcessamentoVersao Cancelar(DateTime timestampCancelamento)
    {
        if (Status != StatusProcessamento.EmProcessamento)
            throw new InvalidOperationException($"Não é possível cancelar processamento com status {Status}.");

        return new ProcessamentoVersao(
            id: Id,
            funcionarioId: FuncionarioId,
            competencia: Competencia,
            versao: Versao,
            status: StatusProcessamento.Cancelado,
            resultado: null,
            iniciadoEm: IniciadoEm,
            finalizadoEm: timestampCancelamento,
            motivoReprocessamento: MotivoReprocessamento,
            versaoAnteriorId: VersaoAnteriorId,
            usuarioId: UsuarioId);
    }

    /// <summary>
    /// Marca este processamento como superado por uma nova versão.
    /// </summary>
    public ProcessamentoVersao MarcarComoSuperado(DateTime timestampSuperacao)
    {
        if (Status != StatusProcessamento.Finalizado)
            throw new InvalidOperationException("Só é possível superar processamentos finalizados.");

        var superado = new ProcessamentoVersao(
            id: Id,
            funcionarioId: FuncionarioId,
            competencia: Competencia,
            versao: Versao,
            status: StatusProcessamento.Superado,
            resultado: Resultado,
            iniciadoEm: IniciadoEm,
            finalizadoEm: FinalizadoEm,
            motivoReprocessamento: MotivoReprocessamento,
            versaoAnteriorId: VersaoAnteriorId,
            usuarioId: UsuarioId);

        superado.HashResultado = HashResultado;
        superado.SuperadoEm = timestampSuperacao;
        return superado;
    }

    /// <summary>
    /// Verifica se o resultado está íntegro (hash bate).
    /// </summary>
    public bool VerificarIntegridade()
    {
        if (Status != StatusProcessamento.Finalizado && Status != StatusProcessamento.Superado)
            return true; // Não há resultado para verificar

        if (Resultado is null || HashResultado is null)
            return false;

        var hashAtual = GerarHashResultado();
        return hashAtual == HashResultado;
    }

    /// <summary>
    /// Indica se o processamento está finalizado.
    /// </summary>
    public bool EstaFinalizado => Status == StatusProcessamento.Finalizado;

    /// <summary>
    /// Indica se o processamento é a versão atual (não foi superado).
    /// </summary>
    public bool EhVersaoAtual => Status == StatusProcessamento.Finalizado;

    /// <summary>
    /// Indica se é um reprocessamento (não é a primeira versão).
    /// </summary>
    public bool EhReprocessamento => !Versao.EhPrimeira;

    private string GerarHashResultado()
    {
        if (Resultado is null) return string.Empty;

        // Hash simples para demonstração
        // Em produção, usar SHA256 ou similar
        var dados = $"{Resultado.FuncionarioId}|{Resultado.Competencia}|{Resultado.SalarioBruto}|{Resultado.SalarioLiquido}|{Resultado.CalculadoEm:O}";
        return dados.GetHashCode().ToString("X8");
    }

    public override string ToString() =>
        $"Processamento {Id} - {FuncionarioId} - {Competencia} - {Versao} - {Status}";
}
