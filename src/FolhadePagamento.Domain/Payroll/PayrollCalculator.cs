using FolhadePagamento.Domain.Entities;
using FolhadePagamento.Domain.ValueObjects;

namespace FolhadePagamento.Domain.Payroll;

/// <summary>
/// Domain Service responsible for payroll calculation.
/// 
/// DETERMINISM GUARANTEE:
/// - Same inputs ALWAYS produce same outputs
/// - No DateTime.Now usage
/// - No external dependencies (DB, HTTP, etc.)
/// - No side effects
/// 
/// This is the core calculation engine of the system.
/// All payroll logic must be concentrated here.
/// </summary>
public sealed class PayrollCalculator
{
    /// <summary>
    /// Calculates payroll for a single employee in a given competence.
    /// 
    /// BASIC VERSION: Only considers base salary.
    /// - GrossSalary = BaseSalary
    /// - TotalDeductions = 0
    /// - NetSalary = GrossSalary
    /// 
    /// Future versions will add IRRF, INSS, FGTS, consignados.
    /// </summary>
    /// <param name="employee">The employee to calculate payroll for</param>
    /// <param name="competence">The competence period (year-month)</param>
    /// <param name="calculationTimestamp">Timestamp for the calculation (passed explicitly for determinism)</param>
    /// <returns>Immutable calculation result</returns>
    public CalculationResult Calculate(
        Employee employee,
        Competence competence,
        DateTime calculationTimestamp)
    {
        // Validation
        if (employee is null)
            throw new ArgumentNullException(nameof(employee));

        if (competence is null)
            throw new ArgumentNullException(nameof(competence));

        if (!employee.IsActive)
            throw new InvalidOperationException($"Cannot calculate payroll for inactive employee: {employee.Id}");

        // ===== PIPELINE STAGE 1: Collect Earnings =====
        // In this basic version, gross salary equals base salary
        var grossSalary = employee.BaseSalary;

        // ===== PIPELINE STAGE 2: Calculate Deductions =====
        // In this basic version, no deductions
        var totalDeductions = Money.Zero;

        // ===== PIPELINE STAGE 3: Calculate Net Salary =====
        // NetSalary = GrossSalary - TotalDeductions
        // This is handled inside CalculationResult.Create for validation

        // ===== PIPELINE STAGE 4: Create Immutable Result =====
        var result = CalculationResult.Create(
            employeeId: employee.Id,
            competence: competence,
            grossSalary: grossSalary,
            totalDeductions: totalDeductions,
            calculatedAt: calculationTimestamp);

        return result;
    }
}
