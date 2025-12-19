namespace FolhadePagamento.Domain.ValueObjects;

/// <summary>
/// Value Object representing monetary values with precision.
/// Immutable by design - any operation returns a new instance.
/// Represents values in BRL (Brazilian Real) with 2 decimal places.
/// </summary>
public sealed class Money : IEquatable<Money>, IComparable<Money>
{
    public decimal Amount { get; }

    private Money(decimal amount)
    {
        Amount = Math.Round(amount, 2, MidpointRounding.ToEven);
    }

    /// <summary>
    /// Creates a Money instance from a decimal value.
    /// </summary>
    public static Money FromDecimal(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Money cannot be negative", nameof(amount));

        return new Money(amount);
    }

    /// <summary>
    /// Creates a zero Money instance.
    /// </summary>
    public static Money Zero => new Money(0);

    /// <summary>
    /// Adds two Money values, returning a new instance.
    /// </summary>
    public Money Add(Money other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        return new Money(Amount + other.Amount);
    }

    /// <summary>
    /// Subtracts another Money value, returning a new instance.
    /// Throws if result would be negative.
    /// </summary>
    public Money Subtract(Money other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        var result = Amount - other.Amount;
        if (result < 0)
            throw new InvalidOperationException("Subtraction would result in negative money");

        return new Money(result);
    }

    /// <summary>
    /// Multiplies by a factor, returning a new instance.
    /// Useful for percentage calculations.
    /// </summary>
    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new ArgumentException("Factor cannot be negative", nameof(factor));

        return new Money(Amount * factor);
    }

    // Equality
    public bool Equals(Money? other)
    {
        if (other is null) return false;
        return Amount == other.Amount;
    }

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => Amount.GetHashCode();

    public static bool operator ==(Money? left, Money? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Money? left, Money? right) => !(left == right);

    // Comparison
    public int CompareTo(Money? other)
    {
        if (other is null) return 1;
        return Amount.CompareTo(other.Amount);
    }

    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

    public override string ToString() => $"R$ {Amount:N2}";
}
