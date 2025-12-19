namespace FolhadePagamento.Infra.Persistencia.Entidades;

/// <summary>
/// Entidade de persistência para Funcionário.
/// Mapeada para a tabela dbo.Funcionario.
/// 
/// IMPORTANTE:
/// - Esta é uma entidade MUTÁVEL (cadastro)
/// - O Core NÃO depende desta entidade
/// - Serve apenas para contexto e vínculo histórico
/// </summary>
public class FuncionarioDb
{
    /// <summary>
    /// Identificador único do funcionário (PK).
    /// </summary>
    public Guid FuncionarioId { get; set; }

    /// <summary>
    /// Nome completo do funcionário.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Salário base contratual (atual).
    /// NOTA: Resultados de cálculo usam snapshot, não este valor.
    /// </summary>
    public decimal SalarioBase { get; set; }

    /// <summary>
    /// Data de admissão.
    /// </summary>
    public DateTime? DataAdmissao { get; set; }

    /// <summary>
    /// Indica se o funcionário está ativo.
    /// </summary>
    public bool Ativo { get; set; } = true;

    /// <summary>
    /// Timestamp de criação do registro.
    /// </summary>
    public DateTime CriadoEm { get; set; }

    /// <summary>
    /// Timestamp da última atualização.
    /// </summary>
    public DateTime? AtualizadoEm { get; set; }

    // ========================================================================
    // NAVEGAÇÃO
    // ========================================================================

    /// <summary>
    /// Processamentos deste funcionário.
    /// </summary>
    public virtual ICollection<ProcessamentoVersaoDb> Processamentos { get; set; } = new List<ProcessamentoVersaoDb>();
}
