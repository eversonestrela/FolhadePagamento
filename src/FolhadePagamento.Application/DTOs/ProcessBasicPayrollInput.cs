namespace FolhadePagamento.Application.DTOs;

/// <summary>
/// Input DTO for basic payroll processing.
/// Contains only the data needed to identify what to calculate.
/// </summary>
public sealed class ProcessBasicPayrollInput
{
    /// <summary>
    /// Unique identifier of the employee.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Employee's name.
    /// </summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>
    /// Base salary amount.
    /// </summary>
    public decimal BaseSalary { get; set; }

    /// <summary>
    /// Competence period in format "yyyy-MM".
    /// </summary>
    public string Competence { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp for the calculation.
    /// Must be provided explicitly (no DateTime.Now usage).
    /// </summary>
    public DateTime CalculationTimestamp { get; set; }
}
