namespace FolhadePagamento.Infra.Persistencia.Entidades;

/// <summary>
/// Entidade de persistência para Lote de Processamento.
/// </summary>
public class LoteProcessamentoDb
{
    public Guid LoteId { get; set; }
    public int CompetenciaAno { get; set; }
    public int CompetenciaMes { get; set; }
    
    /// <summary>
    /// Status: Pendente, EmProcessamento, Concluido, ConcluidoComFalhas, Cancelado
    /// </summary>
    public string Status { get; set; } = "Pendente";
    
    public int TotalItens { get; set; }
    public int ItensConcluidos { get; set; }
    public int ItensComFalha { get; set; }
    public int ItensIgnorados { get; set; }
    
    public DateTime CriadoEm { get; set; }
    public DateTime? IniciadoEm { get; set; }
    public DateTime? ConcluidoEm { get; set; }
    
    public string? UsuarioId { get; set; }
    public string? Observacao { get; set; }

    // Navegação
    public virtual ICollection<ItemLoteDb> Itens { get; set; } = new List<ItemLoteDb>();
}

/// <summary>
/// Entidade de persistência para Item do Lote.
/// </summary>
public class ItemLoteDb
{
    public Guid ItemLoteId { get; set; }
    public Guid LoteId { get; set; }
    public Guid FuncionarioId { get; set; }
    
    /// <summary>
    /// Status: Pendente, EmProcessamento, Sucesso, Falha, Ignorado
    /// </summary>
    public string Status { get; set; } = "Pendente";
    
    /// <summary>
    /// ID do processamento criado (se sucesso).
    /// </summary>
    public Guid? ProcessamentoVersaoId { get; set; }
    
    /// <summary>
    /// Número da versão criada.
    /// </summary>
    public int? VersaoNumero { get; set; }
    
    /// <summary>
    /// Mensagem de erro (se falha).
    /// </summary>
    public string? MensagemErro { get; set; }
    
    /// <summary>
    /// Número de tentativas de processamento.
    /// </summary>
    public int Tentativas { get; set; }
    
    public DateTime? IniciadoEm { get; set; }
    public DateTime? ConcluidoEm { get; set; }

    // Navegação
    public virtual LoteProcessamentoDb? Lote { get; set; }
    public virtual FuncionarioDb? Funcionario { get; set; }
    public virtual ProcessamentoVersaoDb? ProcessamentoVersao { get; set; }
}
