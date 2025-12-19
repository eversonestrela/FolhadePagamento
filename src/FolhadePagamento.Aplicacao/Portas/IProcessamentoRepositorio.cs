namespace FolhadePagamento.Aplicacao.Portas;

/// <summary>
/// Interface para persistência de processamentos de folha de pagamento.
/// 
/// Regras:
/// - Esta interface define o contrato para persistência
/// - A implementação fica na camada de Infraestrutura
/// - Nenhuma regra de negócio deve existir aqui
/// - Apenas persistir e consultar dados
/// </summary>
public interface IProcessamentoRepositorio
{
    // ========================================================================
    // GRAVAÇÃO
    // ========================================================================

    /// <summary>
    /// Salva um novo processamento finalizado com todos os seus detalhes.
    /// </summary>
    /// <param name="processamento">Dados do processamento para persistir</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task SalvarProcessamentoAsync(
        ProcessamentoPersistencia processamento,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca um processamento como superado (quando uma nova versão é criada).
    /// </summary>
    /// <param name="processamentoVersaoId">ID do processamento a ser superado</param>
    /// <param name="superadoEm">Data/hora da superação</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task MarcarComoSuperadoAsync(
        Guid processamentoVersaoId,
        DateTime superadoEm,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // CONSULTA
    // ========================================================================

    /// <summary>
    /// Obtém o processamento pela ID.
    /// </summary>
    Task<ProcessamentoConsulta?> ObterPorIdAsync(
        Guid processamentoVersaoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a versão atual (mais recente finalizada) para um funcionário e competência.
    /// </summary>
    Task<ProcessamentoConsulta?> ObterVersaoAtualAsync(
        Guid funcionarioId,
        int competenciaAno,
        int competenciaMes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o histórico de versões para um funcionário e competência.
    /// Ordenado da mais recente para a mais antiga.
    /// </summary>
    Task<IReadOnlyList<ProcessamentoResumoConsulta>> ObterHistoricoVersoesAsync(
        Guid funcionarioId,
        int competenciaAno,
        int competenciaMes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o próximo número de versão para um funcionário e competência.
    /// Retorna 1 se não existir nenhuma versão.
    /// </summary>
    Task<int> ObterProximoNumeroVersaoAsync(
        Guid funcionarioId,
        int competenciaAno,
        int competenciaMes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se existe algum processamento para funcionário e competência.
    /// </summary>
    Task<bool> ExisteProcessamentoAsync(
        Guid funcionarioId,
        int competenciaAno,
        int competenciaMes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista processamentos de uma competência.
    /// </summary>
    Task<IReadOnlyList<ProcessamentoResumoConsulta>> ListarPorCompetenciaAsync(
        int competenciaAno,
        int competenciaMes,
        bool apenasAtual = true,
        CancellationToken cancellationToken = default);
}

// ============================================================================
// DTOs DE PERSISTÊNCIA (para gravar)
// ============================================================================

/// <summary>
/// DTO para persistência de um processamento completo.
/// </summary>
public record ProcessamentoPersistencia
{
    public required Guid ProcessamentoVersaoId { get; init; }
    public required Guid FuncionarioId { get; init; }
    public required int CompetenciaAno { get; init; }
    public required int CompetenciaMes { get; init; }
    public required int VersaoNumero { get; init; }
    public Guid? VersaoAnteriorId { get; init; }
    public required string Status { get; init; }
    public required DateTime IniciadoEm { get; init; }
    public DateTime? FinalizadoEm { get; init; }
    public string? MotivoReprocessamento { get; init; }
    public string? DescricaoReprocessamento { get; init; }
    public string? UsuarioId { get; init; }
    public string? HashResultado { get; init; }

    public required ResultadoPersistencia Resultado { get; init; }
}

public record ResultadoPersistencia
{
    public required Guid ResultadoCalculoId { get; init; }
    public required decimal SalarioBruto { get; init; }
    public required decimal ValorInss { get; init; }
    public required decimal ValorIrrf { get; init; }
    public required decimal ValorFgts { get; init; }
    public required decimal ValorConsignados { get; init; }
    public required decimal TotalDescontos { get; init; }
    public required decimal SalarioLiquido { get; init; }
    public required decimal TotalEncargosPatronais { get; init; }
    public required decimal CustoTotalEmpregador { get; init; }
    public required DateTime CalculadoEm { get; init; }

    public DetalheInssPersistencia? DetalheInss { get; init; }
    public DetalheIrrfPersistencia? DetalheIrrf { get; init; }
    public DetalheFgtsPersistencia? DetalheFgts { get; init; }
    public DetalheConsignadosPersistencia? DetalheConsignados { get; init; }
}

public record DetalheInssPersistencia
{
    public required Guid DetalheInssId { get; init; }
    public required decimal BaseCalculo { get; init; }
    public required string TabelaIdUsada { get; init; }
    public required decimal AliquotaEfetiva { get; init; }
    public required bool TetoAplicado { get; init; }
    public string? ContribuicaoPorFaixaJson { get; init; }
}

public record DetalheIrrfPersistencia
{
    public required Guid DetalheIrrfId { get; init; }
    public required decimal BaseCalculo { get; init; }
    public required decimal DeducaoInss { get; init; }
    public required int NumeroDependentes { get; init; }
    public required decimal DeducaoPorDependente { get; init; }
    public required string TabelaIdUsada { get; init; }
    public string? FaixaAplicada { get; init; }
    public required decimal AliquotaAplicada { get; init; }
    public required decimal ParcelaDedutivelUsada { get; init; }
    public required bool Isento { get; init; }
}

public record DetalheFgtsPersistencia
{
    public required Guid DetalheFgtsId { get; init; }
    public required decimal BaseCalculo { get; init; }
    public required string TabelaIdUsada { get; init; }
    public required decimal AliquotaAplicada { get; init; }
    public required string TipoContribuinte { get; init; }
}

public record DetalheConsignadosPersistencia
{
    public required Guid DetalheConsignadosId { get; init; }
    public required decimal SalarioBaseConsiderado { get; init; }
    public required decimal PercentualMargem { get; init; }
    public required decimal MargemTotal { get; init; }
    public required decimal MargemUtilizada { get; init; }
    public required decimal MargemDisponivel { get; init; }
    public required int TotalContratosAtivos { get; init; }
    public string? DescontosJson { get; init; }
}

// ============================================================================
// DTOs DE CONSULTA (para leitura)
// ============================================================================

/// <summary>
/// DTO de consulta completa de um processamento.
/// </summary>
public record ProcessamentoConsulta
{
    public required Guid ProcessamentoVersaoId { get; init; }
    public required Guid FuncionarioId { get; init; }
    public required string FuncionarioNome { get; init; }
    public required int CompetenciaAno { get; init; }
    public required int CompetenciaMes { get; init; }
    public required int VersaoNumero { get; init; }
    public Guid? VersaoAnteriorId { get; init; }
    public required string Status { get; init; }
    public required DateTime IniciadoEm { get; init; }
    public DateTime? FinalizadoEm { get; init; }
    public DateTime? SuperadoEm { get; init; }
    public string? MotivoReprocessamento { get; init; }
    public string? DescricaoReprocessamento { get; init; }

    public required ResultadoConsulta Resultado { get; init; }
}

public record ResultadoConsulta
{
    public required decimal SalarioBruto { get; init; }
    public required decimal ValorInss { get; init; }
    public required decimal ValorIrrf { get; init; }
    public required decimal ValorFgts { get; init; }
    public required decimal ValorConsignados { get; init; }
    public required decimal TotalDescontos { get; init; }
    public required decimal SalarioLiquido { get; init; }
    public required decimal TotalEncargosPatronais { get; init; }
    public required decimal CustoTotalEmpregador { get; init; }
    public required DateTime CalculadoEm { get; init; }
}

/// <summary>
/// DTO de resumo para listagens.
/// </summary>
public record ProcessamentoResumoConsulta
{
    public required Guid ProcessamentoVersaoId { get; init; }
    public required Guid FuncionarioId { get; init; }
    public required string FuncionarioNome { get; init; }
    public required int CompetenciaAno { get; init; }
    public required int CompetenciaMes { get; init; }
    public required int VersaoNumero { get; init; }
    public required string Status { get; init; }
    public required DateTime IniciadoEm { get; init; }
    public DateTime? FinalizadoEm { get; init; }
    public string? MotivoReprocessamento { get; init; }
    public required decimal SalarioLiquido { get; init; }
}
