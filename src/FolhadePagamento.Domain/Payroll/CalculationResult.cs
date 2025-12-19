using FolhadePagamento.Domain.ValueObjects;

namespace FolhadePagamento.Domain.Payroll;

/// <summary>
/// Represents the result of a payroll calculation for one employee.
/// Immutable once created - results cannot be modified.
/// This is the output of the PayrollCalculator domain service.
/// </summary>
public sealed class CalculationResult
{
    /// <summary>
    /// The employee for whom the calculation was performed.
    /// </summary>
    public EmployeeId EmployeeId { get; }

    /// <summary>
    /// The competence (year-month) of this calculation.
    /// </summary>
    public Competence Competence { get; }

    /// <summary>
    /// Gross salary (Salário Bruto) - sum of all earnings.
    /// In this basic version, equals BaseSalary.
    /// </summary>
    public Money GrossSalary { get; }

    /// <summary>
    /// Total deductions (Descontos) - sum of all deductions.
    /// In this basic version, equals zero.
    /// </summary>
    public Money TotalDeductions { get; }

    /// <summary>
    /// Net salary (Salário Líquido) - GrossSalary minus TotalDeductions.
    /// </summary>
    public Money NetSalary { get; }

    /// <summary>
    /// Timestamp when calculation was performed.
    /// Provided externally to ensure determinism (no DateTime.Now).
    /// </summary>
    public DateTime CalculatedAt { get; }

    private CalculationResult(
        EmployeeId employeeId,
        Competence competence,
        Money grossSalary,
        Money totalDeductions,
        Money netSalary,
        DateTime calculatedAt)
    {
        EmployeeId = employeeId;
        Competence = competence;
        GrossSalary = grossSalary;
        TotalDeductions = totalDeductions;
        NetSalary = netSalary;
        CalculatedAt = calculatedAt;
    }

    /// <summary>
    /// Factory method to create a CalculationResult.
    /// Validates invariants (NetSalary = GrossSalary - Deductions).
    /// </summary>
    public static CalculationResult Create(
        EmployeeId employeeId,
        Competence competence,
        Money grossSalary,
        Money totalDeductions,
        DateTime calculatedAt)
    {
        if (employeeId is null)
            throw new ArgumentNullException(nameof(employeeId));

        if (competence is null)
            throw new ArgumentNullException(nameof(competence));

        if (grossSalary is null)
            throw new ArgumentNullException(nameof(grossSalary));

        if (totalDeductions is null)
            throw new ArgumentNullException(nameof(totalDeductions));

        // Deterministic calculation: NetSalary = GrossSalary - TotalDeductions
        var netSalary = grossSalary.Subtract(totalDeductions);

        return new CalculationResult(
            employeeId,
            competence,
            grossSalary,
            totalDeductions,
            netSalary,
            calculatedAt);
    }

    public override string ToString() =>
        $"Calculation for {EmployeeId} on {Competence}: Gross={GrossSalary}, Deductions={TotalDeductions}, Net={NetSalary}";
}
