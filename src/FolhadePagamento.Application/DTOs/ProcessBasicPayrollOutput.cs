namespace FolhadePagamento.Application.DTOs;

/// <summary>
/// Output DTO containing the result of basic payroll processing.
/// Flat structure suitable for API responses and serialization.
/// </summary>
public sealed class ProcessBasicPayrollOutput
{
    /// <summary>
    /// Unique identifier of the employee.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Competence period in format "yyyy-MM".
    /// </summary>
    public string Competence { get; set; } = string.Empty;

    /// <summary>
    /// Gross salary (Salário Bruto).
    /// </summary>
    public decimal GrossSalary { get; set; }

    /// <summary>
    /// Total deductions (Descontos).
    /// </summary>
    public decimal TotalDeductions { get; set; }

    /// <summary>
    /// Net salary (Salário Líquido).
    /// </summary>
    public decimal NetSalary { get; set; }

    /// <summary>
    /// When the calculation was performed.
    /// </summary>
    public DateTime CalculatedAt { get; set; }

    /// <summary>
    /// Indicates if the calculation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if calculation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
