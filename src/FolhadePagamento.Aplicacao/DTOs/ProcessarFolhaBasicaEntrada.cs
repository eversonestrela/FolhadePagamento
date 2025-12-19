namespace FolhadePagamento.Aplicacao.DTOs;

/// <summary>
/// DTO de entrada para processamento básico de folha.
/// Contém apenas os dados necessários para identificar o que calcular.
/// </summary>
public sealed class ProcessarFolhaBasicaEntrada
{
    /// <summary>
    /// Identificador único do funcionário.
    /// </summary>
    public Guid FuncionarioId { get; set; }

    /// <summary>
    /// Nome do funcionário.
    /// </summary>
    public string NomeFuncionario { get; set; } = string.Empty;

    /// <summary>
    /// Valor do salário base.
    /// </summary>
    public decimal SalarioBase { get; set; }

    /// <summary>
    /// Período de competência no formato "yyyy-MM".
    /// </summary>
    public string Competencia { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp do cálculo.
    /// Deve ser fornecido explicitamente (sem uso de DateTime.Now).
    /// </summary>
    public DateTime TimestampCalculo { get; set; }
}
