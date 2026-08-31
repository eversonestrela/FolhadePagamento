namespace FolhadePagamento.Aplicacao.Lotes;

/// <summary>
/// Interface para persistência de lotes de processamento.
/// </summary>
public interface ILoteRepositorio
{
    // ========================================================================
    // GRAVAÇÃO - LOTE
    // ========================================================================

    /// <summary>
    /// Cria um novo lote de processamento.
    /// </summary>
    Task CriarLoteAsync(
        LoteProcessamentoPersistencia lote,
        IEnumerable<ItemLotePersistencia> itens,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza o status do lote.
    /// </summary>
    Task AtualizarStatusLoteAsync(
        Guid loteId,
        StatusLote status,
        DateTime? iniciadoEm = null,
        DateTime? concluidoEm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza contadores do lote.
    /// </summary>
    Task AtualizarContadoresLoteAsync(
        Guid loteId,
        int itensConcluidos,
        int itensComFalha,
        int itensIgnorados,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // GRAVAÇÃO - ITEM
    // ========================================================================

    /// <summary>
    /// Atualiza o status de um item do lote.
    /// </summary>
    Task AtualizarItemAsync(
        Guid itemLoteId,
        StatusItemLote status,
        Guid? processamentoVersaoId = null,
        int? versaoNumero = null,
        string? mensagemErro = null,
        int? tentativas = null,
        DateTime? iniciadoEm = null,
        DateTime? concluidoEm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca item como em processamento.
    /// </summary>
    Task IniciarProcessamentoItemAsync(
        Guid itemLoteId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca item como concluído com sucesso.
    /// </summary>
    Task ConcluirItemComSucessoAsync(
        Guid itemLoteId,
        Guid processamentoVersaoId,
        int versaoNumero,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca item como falha.
    /// </summary>
    Task ConcluirItemComFalhaAsync(
        Guid itemLoteId,
        string mensagemErro,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // CONSULTA
    // ========================================================================

    /// <summary>
    /// Obtém um lote por ID.
    /// </summary>
    Task<LoteProcessamentoConsulta?> ObterLotePorIdAsync(
        Guid loteId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista itens de um lote.
    /// </summary>
    Task<IReadOnlyList<ItemLoteConsulta>> ListarItensDoLoteAsync(
        Guid loteId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém próximo item pendente para processamento.
    /// </summary>
    Task<ItemLotePersistencia?> ObterProximoItemPendenteAsync(
        Guid loteId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista lotes pendentes ou em processamento.
    /// </summary>
    Task<IReadOnlyList<LoteResumoConsulta>> ListarLotesAtivosAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista lotes por competência.
    /// </summary>
    Task<IReadOnlyList<LoteResumoConsulta>> ListarLotesPorCompetenciaAsync(
        int competenciaAno,
        int competenciaMes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se existe lote ativo para a competência.
    /// </summary>
    Task<bool> ExisteLoteAtivoParaCompetenciaAsync(
        int competenciaAno,
        int competenciaMes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Conta itens por status em um lote.
    /// </summary>
    Task<Dictionary<StatusItemLote, int>> ContarItensPorStatusAsync(
        Guid loteId,
        CancellationToken cancellationToken = default);
}
