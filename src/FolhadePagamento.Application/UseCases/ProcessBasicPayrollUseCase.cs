using FolhadePagamento.Application.DTOs;
using FolhadePagamento.Domain.Entities;
using FolhadePagamento.Domain.Payroll;
using FolhadePagamento.Domain.ValueObjects;

namespace FolhadePagamento.Application.UseCases;

/// <summary>
/// Use Case: Process Basic Payroll for a single employee.
/// 
/// RESPONSIBILITY:
/// - Validate input DTOs
/// - Map DTOs to Domain objects
/// - Delegate calculation to Domain Service
/// - Map results back to output DTOs
/// 
/// DOES NOT CONTAIN:
/// - Business logic (handled by PayrollCalculator)
/// - Database access (will use repositories in future)
/// - Framework dependencies
/// </summary>
public sealed class ProcessBasicPayrollUseCase
{
    private readonly PayrollCalculator _calculator;

    public ProcessBasicPayrollUseCase(PayrollCalculator calculator)
    {
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    /// <summary>
    /// Executes the use case.
    /// </summary>
    /// <param name="input">Input data for payroll calculation</param>
    /// <returns>Output with calculation results</returns>
    public ProcessBasicPayrollOutput Execute(ProcessBasicPayrollInput input)
    {
        try
        {
            // 1. Validate input
            ValidateInput(input);

            // 2. Map DTO to Domain objects
            var employeeId = EmployeeId.From(input.EmployeeId);
            var baseSalary = Money.FromDecimal(input.BaseSalary);
            var competence = Competence.Parse(input.Competence);

            // 3. Create Employee entity
            // In future: will be loaded from IEmployeeRepository
            var employee = Employee.Create(employeeId, input.EmployeeName, baseSalary);

            // 4. Delegate to Domain Service (PayrollCalculator)
            var result = _calculator.Calculate(
                employee,
                competence,
                input.CalculationTimestamp);

            // 5. Map result to output DTO
            return MapToOutput(result);
        }
        catch (Exception ex)
        {
            return new ProcessBasicPayrollOutput
            {
                EmployeeId = input.EmployeeId,
                Competence = input.Competence,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static void ValidateInput(ProcessBasicPayrollInput input)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        if (input.EmployeeId == Guid.Empty)
            throw new ArgumentException("EmployeeId is required");

        if (string.IsNullOrWhiteSpace(input.EmployeeName))
            throw new ArgumentException("EmployeeName is required");

        if (input.BaseSalary <= 0)
            throw new ArgumentException("BaseSalary must be greater than zero");

        if (string.IsNullOrWhiteSpace(input.Competence))
            throw new ArgumentException("Competence is required");

        if (input.CalculationTimestamp == default)
            throw new ArgumentException("CalculationTimestamp is required");
    }

    private static ProcessBasicPayrollOutput MapToOutput(CalculationResult result)
    {
        return new ProcessBasicPayrollOutput
        {
            EmployeeId = result.EmployeeId.Value,
            Competence = result.Competence.ToString(),
            GrossSalary = result.GrossSalary.Amount,
            TotalDeductions = result.TotalDeductions.Amount,
            NetSalary = result.NetSalary.Amount,
            CalculatedAt = result.CalculatedAt,
            Success = true,
            ErrorMessage = null
        };
    }
}
