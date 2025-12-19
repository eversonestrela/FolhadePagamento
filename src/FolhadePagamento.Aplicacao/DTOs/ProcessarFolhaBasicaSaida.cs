namespace FolhadePagamento.Aplicacao.DTOs;

/// <summary>
/// DTO de saída contendo o resultado do processamento básico de folha.
/// Estrutura plana adequada para respostas de API e serialização.
/// </summary>
public sealed class ProcessarFolhaBasicaSaida
{
    /// <summary>
    /// Identificador único do funcionário.
    /// </summary>
    public Guid FuncionarioId { get; set; }

    /// <summary>
    /// Período de competência no formato "yyyy-MM".
    /// </summary>
    public string Competencia { get; set; } = string.Empty;

    /// <summary>
    /// Salário Bruto.
    /// </summary>
    public decimal SalarioBruto { get; set; }

    /// <summary>
    /// Total de Descontos.
    /// </summary>
    public decimal TotalDescontos { get; set; }

    /// <summary>
    /// Salário Líquido.
    /// </summary>
    public decimal SalarioLiquido { get; set; }

    /// <summary>
    /// Quando o cálculo foi realizado.
    /// </summary>
    public DateTime CalculadoEm { get; set; }

    /// <summary>
    /// Indica se o cálculo foi bem-sucedido.
    /// </summary>
    public bool Sucesso { get; set; }

    /// <summary>
    /// Mensagem de erro se o cálculo falhou.
    /// </summary>
    public string? MensagemErro { get; set; }
}
