namespace FolhadePagamento.Domain.ValueObjects;

/// <summary>
/// Value Object representing a payroll competence period (year-month).
/// Competence is the reference month for payroll calculation (e.g., Jan/2025).
/// Immutable by design.
/// </summary>
public sealed class Competence : IEquatable<Competence>, IComparable<Competence>
{
    public int Year { get; }
    public int Month { get; }

    private Competence(int year, int month)
    {
        Year = year;
        Month = month;
    }

    /// <summary>
    /// Creates a Competence from year and month.
    /// </summary>
    public static Competence FromYearMonth(int year, int month)
    {
        if (year < 1900 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be between 1900 and 2100");

        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12");

        return new Competence(year, month);
    }

    /// <summary>
    /// Creates a Competence from a DateTime (uses year and month only).
    /// Does NOT use DateTime.Now - requires explicit date.
    /// </summary>
    public static Competence FromDate(DateTime date)
    {
        return new Competence(date.Year, date.Month);
    }

    /// <summary>
    /// Parses a string in format "yyyy-MM" to Competence.
    /// </summary>
    public static Competence Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Competence string cannot be empty", nameof(value));

        var parts = value.Split('-');
        if (parts.Length != 2)
            throw new FormatException("Competence must be in format yyyy-MM");

        if (!int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var month))
            throw new FormatException("Invalid year or month in competence string");

        return FromYearMonth(year, month);
    }

    /// <summary>
    /// Returns the next competence (next month).
    /// </summary>
    public Competence Next()
    {
        if (Month == 12)
            return new Competence(Year + 1, 1);

        return new Competence(Year, Month + 1);
    }

    /// <summary>
    /// Returns the previous competence.
    /// </summary>
    public Competence Previous()
    {
        if (Month == 1)
            return new Competence(Year - 1, 12);

        return new Competence(Year, Month - 1);
    }

    /// <summary>
    /// Gets the first day of the competence month.
    /// </summary>
    public DateTime FirstDay => new DateTime(Year, Month, 1);

    /// <summary>
    /// Gets the last day of the competence month.
    /// </summary>
    public DateTime LastDay => new DateTime(Year, Month, DateTime.DaysInMonth(Year, Month));

    // Equality
    public bool Equals(Competence? other)
    {
        if (other is null) return false;
        return Year == other.Year && Month == other.Month;
    }

    public override bool Equals(object? obj) => Equals(obj as Competence);

    public override int GetHashCode() => HashCode.Combine(Year, Month);

    public static bool operator ==(Competence? left, Competence? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Competence? left, Competence? right) => !(left == right);

    // Comparison
    public int CompareTo(Competence? other)
    {
        if (other is null) return 1;

        var yearComparison = Year.CompareTo(other.Year);
        if (yearComparison != 0) return yearComparison;

        return Month.CompareTo(other.Month);
    }

    public static bool operator >(Competence left, Competence right) => left.CompareTo(right) > 0;
    public static bool operator <(Competence left, Competence right) => left.CompareTo(right) < 0;
    public static bool operator >=(Competence left, Competence right) => left.CompareTo(right) >= 0;
    public static bool operator <=(Competence left, Competence right) => left.CompareTo(right) <= 0;

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
