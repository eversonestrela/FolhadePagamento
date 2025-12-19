using FolhadePagamento.Domain.ValueObjects;

namespace FolhadePagamento.Domain.Entities;

/// <summary>
/// Entity representing an Employee (Funcionário).
/// Contains master data used for payroll calculations.
/// This is a simplified version for the initial implementation.
/// </summary>
public class Employee
{
    public EmployeeId Id { get; private set; }
    public string Name { get; private set; }
    public Money BaseSalary { get; private set; }
    public bool IsActive { get; private set; }

    // Private constructor for controlled creation
    private Employee(EmployeeId id, string name, Money baseSalary, bool isActive)
    {
        Id = id;
        Name = name;
        BaseSalary = baseSalary;
        IsActive = isActive;
    }

    /// <summary>
    /// Factory method to create a new Employee.
    /// Ensures all invariants are satisfied.
    /// </summary>
    public static Employee Create(EmployeeId id, string name, Money baseSalary)
    {
        if (id is null)
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Employee name cannot be empty", nameof(name));

        if (baseSalary is null)
            throw new ArgumentNullException(nameof(baseSalary));

        return new Employee(id, name.Trim(), baseSalary, isActive: true);
    }

    /// <summary>
    /// Deactivates the employee (for termination scenarios).
    /// Deactivated employees should not be included in new payroll calculations.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Updates the base salary.
    /// Should only be done before processing a new competence.
    /// </summary>
    public void UpdateBaseSalary(Money newSalary)
    {
        if (newSalary is null)
            throw new ArgumentNullException(nameof(newSalary));

        BaseSalary = newSalary;
    }

    public override string ToString() => $"Employee {Name} (ID: {Id})";
}
