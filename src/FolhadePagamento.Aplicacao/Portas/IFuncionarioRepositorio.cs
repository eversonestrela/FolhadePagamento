namespace FolhadePagamento.Aplicacao.Portas;

/// <summary>
/// Interface para persistência de funcionários.
/// </summary>
public interface IFuncionarioRepositorio
{
    // ========================================================================
    // GRAVAÇÃO
    // ========================================================================

    /// <summary>
    /// Salva um novo funcionário.
    /// </summary>
    Task SalvarAsync(
        FuncionarioPersistencia funcionario,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza dados de um funcionário existente.
    /// </summary>
    Task AtualizarAsync(
        Guid funcionarioId,
        FuncionarioAtualizacao atualizacao,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Desativa um funcionário (soft delete).
    /// </summary>
    Task DesativarAsync(
        Guid funcionarioId,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // CONSULTA
    // ========================================================================

    /// <summary>
    /// Obtém um funcionário por ID.
    /// </summary>
    Task<FuncionarioConsulta?> ObterPorIdAsync(
        Guid funcionarioId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista funcionários ativos.
    /// </summary>
    Task<IReadOnlyList<FuncionarioConsulta>> ListarAtivosAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se um funcionário existe e está ativo.
    /// </summary>
    Task<bool> ExisteEAtivoAsync(
        Guid funcionarioId,
        CancellationToken cancellationToken = default);
}

// ============================================================================
// DTOs
// ============================================================================

public record FuncionarioPersistencia
{
    public required Guid FuncionarioId { get; init; }
    public required string Nome { get; init; }
    public required decimal SalarioBase { get; init; }
    public DateTime? DataAdmissao { get; init; }
    public required bool Ativo { get; init; }
    public required DateTime CriadoEm { get; init; }
}

public record FuncionarioAtualizacao
{
    public string? Nome { get; init; }
    public decimal? SalarioBase { get; init; }
    public DateTime? DataAdmissao { get; init; }
    public required DateTime AtualizadoEm { get; init; }
}

public record FuncionarioConsulta
{
    public required Guid FuncionarioId { get; init; }
    public required string Nome { get; init; }
    public required decimal SalarioBase { get; init; }
    public DateTime? DataAdmissao { get; init; }
    public required bool Ativo { get; init; }
    public required DateTime CriadoEm { get; init; }
    public DateTime? AtualizadoEm { get; init; }
}
