namespace FolhadePagamento.Dominio.ValueObjects;

/// <summary>
/// Value Object representando valores monetários com precisão.
/// Imutável por design - qualquer operação retorna uma nova instância.
/// Representa valores em BRL (Real Brasileiro) com 2 casas decimais.
/// </summary>
public sealed class Dinheiro : IEquatable<Dinheiro>, IComparable<Dinheiro>
{
    public decimal Valor { get; }

    private Dinheiro(decimal valor)
    {
        Valor = Math.Round(valor, 2, MidpointRounding.ToEven);
    }

    /// <summary>
    /// Cria uma instância de Dinheiro a partir de um valor decimal.
    /// </summary>
    public static Dinheiro DeDecimal(decimal valor)
    {
        if (valor < 0)
            throw new ArgumentException("Valor monetário não pode ser negativo", nameof(valor));

        return new Dinheiro(valor);
    }

    /// <summary>
    /// Cria uma instância de Dinheiro com valor zero.
    /// </summary>
    public static Dinheiro Zero => new Dinheiro(0);

    /// <summary>
    /// Soma dois valores monetários, retornando nova instância.
    /// </summary>
    public Dinheiro Somar(Dinheiro outro)
    {
        if (outro is null)
            throw new ArgumentNullException(nameof(outro));

        return new Dinheiro(Valor + outro.Valor);
    }

    /// <summary>
    /// Subtrai outro valor monetário, retornando nova instância.
    /// Lança exceção se resultado for negativo.
    /// </summary>
    public Dinheiro Subtrair(Dinheiro outro)
    {
        if (outro is null)
            throw new ArgumentNullException(nameof(outro));

        var resultado = Valor - outro.Valor;
        if (resultado < 0)
            throw new InvalidOperationException("Subtração resultaria em valor negativo");

        return new Dinheiro(resultado);
    }

    /// <summary>
    /// Multiplica por um fator, retornando nova instância.
    /// Útil para cálculos de percentual.
    /// </summary>
    public Dinheiro Multiplicar(decimal fator)
    {
        if (fator < 0)
            throw new ArgumentException("Fator não pode ser negativo", nameof(fator));

        return new Dinheiro(Valor * fator);
    }

    // Igualdade
    public bool Equals(Dinheiro? outro)
    {
        if (outro is null) return false;
        return Valor == outro.Valor;
    }

    public override bool Equals(object? obj) => Equals(obj as Dinheiro);

    public override int GetHashCode() => Valor.GetHashCode();

    public static bool operator ==(Dinheiro? esquerda, Dinheiro? direita)
    {
        if (esquerda is null && direita is null) return true;
        if (esquerda is null || direita is null) return false;
        return esquerda.Equals(direita);
    }

    public static bool operator !=(Dinheiro? esquerda, Dinheiro? direita) => !(esquerda == direita);

    // Comparação
    public int CompareTo(Dinheiro? outro)
    {
        if (outro is null) return 1;
        return Valor.CompareTo(outro.Valor);
    }

    public static bool operator >(Dinheiro esquerda, Dinheiro direita) => esquerda.CompareTo(direita) > 0;
    public static bool operator <(Dinheiro esquerda, Dinheiro direita) => esquerda.CompareTo(direita) < 0;
    public static bool operator >=(Dinheiro esquerda, Dinheiro direita) => esquerda.CompareTo(direita) >= 0;
    public static bool operator <=(Dinheiro esquerda, Dinheiro direita) => esquerda.CompareTo(direita) <= 0;

    public override string ToString() => $"R$ {Valor:N2}";
}
