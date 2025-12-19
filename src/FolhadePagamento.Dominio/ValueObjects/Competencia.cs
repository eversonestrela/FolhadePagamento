namespace FolhadePagamento.Dominio.ValueObjects;

/// <summary>
/// Value Object representando um período de competência da folha (ano-mês).
/// Competência é o mês de referência para cálculo da folha (ex: Jan/2025).
/// Imutável por design.
/// </summary>
public sealed class Competencia : IEquatable<Competencia>, IComparable<Competencia>
{
    public int Ano { get; }
    public int Mes { get; }

    private Competencia(int ano, int mes)
    {
        Ano = ano;
        Mes = mes;
    }

    /// <summary>
    /// Cria uma Competência a partir de ano e mês.
    /// </summary>
    public static Competencia DeAnoMes(int ano, int mes)
    {
        if (ano < 1900 || ano > 2100)
            throw new ArgumentOutOfRangeException(nameof(ano), "Ano deve estar entre 1900 e 2100");

        if (mes < 1 || mes > 12)
            throw new ArgumentOutOfRangeException(nameof(mes), "Mês deve estar entre 1 e 12");

        return new Competencia(ano, mes);
    }

    /// <summary>
    /// Cria uma Competência a partir de uma data (usa apenas ano e mês).
    /// NÃO usa DateTime.Now - requer data explícita.
    /// </summary>
    public static Competencia DeData(DateTime data)
    {
        return new Competencia(data.Year, data.Month);
    }

    /// <summary>
    /// Converte uma string no formato "yyyy-MM" para Competência.
    /// </summary>
    public static Competencia Converter(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("String de competência não pode estar vazia", nameof(valor));

        var partes = valor.Split('-');
        if (partes.Length != 2)
            throw new FormatException("Competência deve estar no formato yyyy-MM");

        if (!int.TryParse(partes[0], out var ano) || !int.TryParse(partes[1], out var mes))
            throw new FormatException("Ano ou mês inválido na string de competência");

        return DeAnoMes(ano, mes);
    }

    /// <summary>
    /// Retorna a próxima competência (próximo mês).
    /// </summary>
    public Competencia Proxima()
    {
        if (Mes == 12)
            return new Competencia(Ano + 1, 1);

        return new Competencia(Ano, Mes + 1);
    }

    /// <summary>
    /// Retorna a competência anterior.
    /// </summary>
    public Competencia Anterior()
    {
        if (Mes == 1)
            return new Competencia(Ano - 1, 12);

        return new Competencia(Ano, Mes - 1);
    }

    /// <summary>
    /// Obtém o primeiro dia do mês da competência.
    /// </summary>
    public DateTime PrimeiroDia => new DateTime(Ano, Mes, 1);

    /// <summary>
    /// Obtém o último dia do mês da competência.
    /// </summary>
    public DateTime UltimoDia => new DateTime(Ano, Mes, DateTime.DaysInMonth(Ano, Mes));

    // Igualdade
    public bool Equals(Competencia? outra)
    {
        if (outra is null) return false;
        return Ano == outra.Ano && Mes == outra.Mes;
    }

    public override bool Equals(object? obj) => Equals(obj as Competencia);

    public override int GetHashCode() => HashCode.Combine(Ano, Mes);

    public static bool operator ==(Competencia? esquerda, Competencia? direita)
    {
        if (esquerda is null && direita is null) return true;
        if (esquerda is null || direita is null) return false;
        return esquerda.Equals(direita);
    }

    public static bool operator !=(Competencia? esquerda, Competencia? direita) => !(esquerda == direita);

    // Comparação
    public int CompareTo(Competencia? outra)
    {
        if (outra is null) return 1;

        var comparacaoAno = Ano.CompareTo(outra.Ano);
        if (comparacaoAno != 0) return comparacaoAno;

        return Mes.CompareTo(outra.Mes);
    }

    public static bool operator >(Competencia esquerda, Competencia direita) => esquerda.CompareTo(direita) > 0;
    public static bool operator <(Competencia esquerda, Competencia direita) => esquerda.CompareTo(direita) < 0;
    public static bool operator >=(Competencia esquerda, Competencia direita) => esquerda.CompareTo(direita) >= 0;
    public static bool operator <=(Competencia esquerda, Competencia direita) => esquerda.CompareTo(direita) <= 0;

    public override string ToString() => $"{Ano:D4}-{Mes:D2}";
}
