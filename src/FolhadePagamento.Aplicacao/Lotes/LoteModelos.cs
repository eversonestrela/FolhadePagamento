namespace FolhadePagamento.Aplicacao.Lotes;

// ============================================================================
// STATUS DO LOTE
// ============================================================================

/// <summary>
/// Status do lote de processamento.
/// </summary>
public enum StatusLote
{
    /// <summary>Lote criado, aguardando início.</summary>
    Pendente = 0,
    
    /// <summary>Lote em execução.</summary>
    EmProcessamento = 1,
    
    /// <summary>Todos os itens processados com sucesso.</summary>
    Concluido = 2,
    
    /// <summary>Concluído com alguns itens com falha.</summary>
    ConcluidoComFalhas = 3,
    
    /// <summary>Lote cancelado.</summary>
    Cancelado = 4
}

/// <summary>
/// Status de um item do lote.
/// </summary>
public enum StatusItemLote
{
    /// <summary>Item aguardando processamento.</summary>
    Pendente = 0,
    
    /// <summary>Item em processamento.</summary>
    EmProcessamento = 1,
    
    /// <summary>Processamento concluído com sucesso.</summary>
    Sucesso = 2,
    
    /// <summary>Processamento falhou.</summary>
    Falha = 3,
    
    /// <summary>Item pulado (ex: funcionário inativo).</summary>
    Ignorado = 4
}

// ============================================================================
// DTOs DE PERSISTÊNCIA
// ============================================================================

/// <summary>
/// DTO para persistência de um lote de processamento.
/// </summary>
public record LoteProcessamentoPersistencia
{
    public required Guid LoteId { get; init; }
    public required int CompetenciaAno { get; init; }
    public required int CompetenciaMes { get; init; }
    public required StatusLote Status { get; init; }
    public required int TotalItens { get; init; }
    public int ItensConcluidos { get; init; }
    public int ItensComFalha { get; init; }
    public int ItensIgnorados { get; init; }
    public required DateTime CriadoEm { get; init; }
    public DateTime? IniciadoEm { get; init; }
    public DateTime? ConcluidoEm { get; init; }
    public string? UsuarioId { get; init; }
    public string? Observacao { get; init; }
}

/// <summary>
/// DTO para persistência de um item do lote.
/// </summary>
public record ItemLotePersistencia
{
    public required Guid ItemLoteId { get; init; }
    public required Guid LoteId { get; init; }
    public required Guid FuncionarioId { get; init; }
    public required StatusItemLote Status { get; init; }
    public Guid? ProcessamentoVersaoId { get; init; }
    public int? VersaoNumero { get; init; }
    public string? MensagemErro { get; init; }
    public int Tentativas { get; init; }
    public DateTime? IniciadoEm { get; init; }
    public DateTime? ConcluidoEm { get; init; }
}

// ============================================================================
// DTOs DE CONSULTA
// ============================================================================

/// <summary>
/// DTO de consulta de um lote de processamento.
/// </summary>
public record LoteProcessamentoConsulta
{
    public required Guid LoteId { get; init; }
    public required int CompetenciaAno { get; init; }
    public required int CompetenciaMes { get; init; }
    public required string Status { get; init; }
    public required int TotalItens { get; init; }
    public required int ItensConcluidos { get; init; }
    public required int ItensComFalha { get; init; }
    public required int ItensIgnorados { get; init; }
    public required int ItensPendentes { get; init; }
    public required decimal PercentualConcluido { get; init; }
    public required DateTime CriadoEm { get; init; }
    public DateTime? IniciadoEm { get; init; }
    public DateTime? ConcluidoEm { get; init; }
    public TimeSpan? DuracaoTotal { get; init; }
    public string? UsuarioId { get; init; }
    public string? Observacao { get; init; }
}

/// <summary>
/// DTO de consulta de um item do lote.
/// </summary>
public record ItemLoteConsulta
{
    public required Guid ItemLoteId { get; init; }
    public required Guid LoteId { get; init; }
    public required Guid FuncionarioId { get; init; }
    public required string FuncionarioNome { get; init; }
    public required string Status { get; init; }
    public Guid? ProcessamentoVersaoId { get; init; }
    public int? VersaoNumero { get; init; }
    public decimal? SalarioLiquido { get; init; }
    public string? MensagemErro { get; init; }
    public required int Tentativas { get; init; }
    public DateTime? IniciadoEm { get; init; }
    public DateTime? ConcluidoEm { get; init; }
    public TimeSpan? Duracao { get; init; }
}

/// <summary>
/// DTO resumido para listagem de lotes.
/// </summary>
public record LoteResumoConsulta
{
    public required Guid LoteId { get; init; }
    public required int CompetenciaAno { get; init; }
    public required int CompetenciaMes { get; init; }
    public required string Status { get; init; }
    public required int TotalItens { get; init; }
    public required decimal PercentualConcluido { get; init; }
    public required DateTime CriadoEm { get; init; }
    public DateTime? ConcluidoEm { get; init; }
}
