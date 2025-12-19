namespace FolhadePagamento.Infra.Persistencia.Entidades;

/// <summary>
/// Entidade de persistência para Processamento Versionado.
/// Mapeada para a tabela dbo.ProcessamentoVersao.
/// 
/// IMPORTANTE:
/// - Esta entidade é IMUTÁVEL após finalização
/// - Nunca deve sofrer UPDATE (exceto SuperadoEm)
/// - Nunca deve sofrer DELETE
/// </summary>
public class ProcessamentoVersaoDb
{
    /// <summary>
    /// Identificador único do processamento (PK).
    /// </summary>
    public Guid ProcessamentoVersaoId { get; set; }

    /// <summary>
    /// Funcionário processado (FK).
    /// </summary>
    public Guid FuncionarioId { get; set; }

    /// <summary>
    /// Ano da competência (ex: 2025).
    /// </summary>
    public int CompetenciaAno { get; set; }

    /// <summary>
    /// Mês da competência (1-12).
    /// </summary>
    public int CompetenciaMes { get; set; }

    /// <summary>
    /// Número da versão (1, 2, 3...).
    /// </summary>
    public int VersaoNumero { get; set; }

    /// <summary>
    /// Referência à versão anterior (NULL para V1).
    /// </summary>
    public Guid? VersaoAnteriorId { get; set; }

    /// <summary>
    /// Status: EmProcessamento, Finalizado, Cancelado, Superado.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp de início do processamento.
    /// </summary>
    public DateTime IniciadoEm { get; set; }

    /// <summary>
    /// Timestamp de finalização (NULL se não finalizado).
    /// </summary>
    public DateTime? FinalizadoEm { get; set; }

    /// <summary>
    /// Timestamp de quando foi superado (NULL se versão atual).
    /// </summary>
    public DateTime? SuperadoEm { get; set; }

    /// <summary>
    /// Código do motivo de reprocessamento (NULL para V1).
    /// </summary>
    public string? MotivoReprocessamento { get; set; }

    /// <summary>
    /// Descrição detalhada do motivo (NULL para V1).
    /// </summary>
    public string? DescricaoReprocessamento { get; set; }

    /// <summary>
    /// Usuário que executou o processamento.
    /// </summary>
    public string? UsuarioId { get; set; }

    /// <summary>
    /// Hash SHA256 do resultado para verificação de integridade.
    /// </summary>
    public string? HashResultado { get; set; }

    /// <summary>
    /// Timestamp de criação do registro no banco.
    /// </summary>
    public DateTime CriadoEm { get; set; }

    // ========================================================================
    // NAVEGAÇÃO
    // ========================================================================

    /// <summary>
    /// Funcionário (navegação).
    /// </summary>
    public virtual FuncionarioDb? Funcionario { get; set; }

    /// <summary>
    /// Versão anterior (navegação, self-reference).
    /// </summary>
    public virtual ProcessamentoVersaoDb? VersaoAnterior { get; set; }

    /// <summary>
    /// Resultado do cálculo (navegação 1:1).
    /// </summary>
    public virtual ResultadoCalculoDb? Resultado { get; set; }
}
