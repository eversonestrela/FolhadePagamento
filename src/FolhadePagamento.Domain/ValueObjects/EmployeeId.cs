namespace FolhadePagamento.Domain.ValueObjects;

/// <summary>
/// Value Object representing a unique Employee identifier.
/// Wraps a Guid to provide strong typing and prevent primitive obsession.
/// </summary>
public sealed class EmployeeId : IEquatable<EmployeeId>
{
    public Guid Value { get; }

    private EmployeeId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new unique EmployeeId.
    /// </summary>
    public static EmployeeId New() => new EmployeeId(Guid.NewGuid());

    /// <summary>
    /// Creates an EmployeeId from an existing Guid.
    /// </summary>
    public static EmployeeId From(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("EmployeeId cannot be empty", nameof(id));

        return new EmployeeId(id);
    }

    // Equality
    public bool Equals(EmployeeId? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as EmployeeId);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(EmployeeId? left, EmployeeId? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(EmployeeId? left, EmployeeId? right) => !(left == right);

    public override string ToString() => Value.ToString();
}
