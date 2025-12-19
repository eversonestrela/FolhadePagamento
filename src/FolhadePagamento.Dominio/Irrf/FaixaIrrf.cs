using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Irrf;

/// <summary>
/// Value Object representando uma faixa da tabela progressiva do IRRF.
/// 
/// O IRRF brasileiro é calculado de forma progressiva com dedução por faixa:
/// - Cada faixa tem um limite inferior, limite superior, alíquota e parcela a deduzir
/// - A parcela a deduzir simplifica o cálculo progressivo
/// 
/// Exemplo (Tabela IRRF 2025 - valores ilustrativos):
/// - Faixa 1: Até R$ 2.259,20 → Isento
/// - Faixa 2: R$ 2.259,21 a R$ 2.826,65 → 7,5% - R$ 169,44
/// - Faixa 3: R$ 2.826,66 a R$ 3.751,05 → 15% - R$ 381,44
/// - Faixa 4: R$ 3.751,06 a R$ 4.664,68 → 22,5% - R$ 662,77
/// - Faixa 5: Acima de R$ 4.664,68 → 27,5% - R$ 896,00
/// 
/// Imutável por design.
/// </summary>
public sealed class FaixaIrrf : IEquatable<FaixaIrrf>, IComparable<FaixaIrrf>
{
    /// <summary>
    /// Limite inferior da faixa (inclusive).
    /// </summary>
    public Dinheiro LimiteInferior { get; }

    /// <summary>
    /// Limite superior da faixa (inclusive).
    /// Null significa sem limite (última faixa).
    /// </summary>
    public Dinheiro? LimiteSuperior { get; }

    /// <summary>
    /// Alíquota da faixa em percentual (ex: 7.5 para 7,5%).
    /// </summary>
    public decimal Aliquota { get; }

    /// <summary>
    /// Parcela a deduzir do IRRF para simplificar cálculo progressivo.
    /// Na faixa isenta, é zero.
    /// </summary>
    public Dinheiro ParcelaADeduzir { get; }

    private FaixaIrrf(Dinheiro limiteInferior, Dinheiro? limiteSuperior, decimal aliquota, Dinheiro parcelaADeduzir)
    {
        LimiteInferior = limiteInferior;
        LimiteSuperior = limiteSuperior;
        Aliquota = aliquota;
        ParcelaADeduzir = parcelaADeduzir;
    }

    /// <summary>
    /// Cria uma faixa de IRRF.
    /// </summary>
    /// <param name="limiteInferior">Limite inferior (inclusive)</param>
    /// <param name="limiteSuperior">Limite superior (inclusive). Null para última faixa.</param>
    /// <param name="aliquota">Alíquota em percentual (ex: 7.5 para 7,5%)</param>
    /// <param name="parcelaADeduzir">Parcela a deduzir do imposto</param>
    public static FaixaIrrf Criar(Dinheiro limiteInferior, Dinheiro? limiteSuperior, decimal aliquota, Dinheiro parcelaADeduzir)
    {
        if (limiteInferior is null)
            throw new ArgumentNullException(nameof(limiteInferior));

        if (parcelaADeduzir is null)
            throw new ArgumentNullException(nameof(parcelaADeduzir));

        if (aliquota < 0 || aliquota > 100)
            throw new ArgumentOutOfRangeException(nameof(aliquota), "Alíquota deve estar entre 0 e 100");

        if (limiteSuperior is not null && limiteSuperior < limiteInferior)
            throw new ArgumentException("Limite superior não pode ser menor que limite inferior", nameof(limiteSuperior));

        return new FaixaIrrf(limiteInferior, limiteSuperior, aliquota, parcelaADeduzir);
    }

    /// <summary>
    /// Cria uma faixa de isenção (alíquota 0%, parcela a deduzir 0).
    /// </summary>
    public static FaixaIrrf CriarFaixaIsenta(Dinheiro limiteInferior, Dinheiro limiteSuperior)
    {
        return Criar(limiteInferior, limiteSuperior, 0m, Dinheiro.Zero);
    }

    /// <summary>
    /// Verifica se esta é a faixa de isenção.
    /// </summary>
    public bool EhFaixaIsenta => Aliquota == 0m;

    /// <summary>
    /// Calcula o valor do IRRF para uma base de cálculo dentro desta faixa.
    /// Fórmula: (BaseCalculo * Aliquota%) - ParcelaADeduzir
    /// </summary>
    /// <param name="baseCalculo">Base de cálculo do IRRF (Bruto - INSS - Deduções)</param>
    /// <returns>Valor do IRRF (nunca negativo)</returns>
    public Dinheiro CalcularImposto(Dinheiro baseCalculo)
    {
        if (baseCalculo is null)
            throw new ArgumentNullException(nameof(baseCalculo));

        // Se faixa isenta, retorna zero
        if (EhFaixaIsenta)
            return Dinheiro.Zero;

        // Aplica alíquota sobre a base
        var impostoSemDeducao = baseCalculo.Multiplicar(Aliquota / 100m);

        // IRRF nunca pode ser negativo
        // Verifica se imposto é menor que a parcela a deduzir
        if (impostoSemDeducao.Valor <= ParcelaADeduzir.Valor)
            return Dinheiro.Zero;

        // Subtrai parcela a deduzir
        return impostoSemDeducao.Subtrair(ParcelaADeduzir);
    }

    /// <summary>
    /// Verifica se uma base de cálculo está dentro desta faixa.
    /// </summary>
    public bool BaseEstaNaFaixa(Dinheiro baseCalculo)
    {
        if (baseCalculo is null)
            throw new ArgumentNullException(nameof(baseCalculo));

        if (baseCalculo < LimiteInferior)
            return false;

        if (LimiteSuperior is not null && baseCalculo > LimiteSuperior)
            return false;

        return true;
    }

    // Igualdade
    public bool Equals(FaixaIrrf? outra)
    {
        if (outra is null) return false;
        return LimiteInferior == outra.LimiteInferior
            && LimiteSuperior == outra.LimiteSuperior
            && Aliquota == outra.Aliquota
            && ParcelaADeduzir == outra.ParcelaADeduzir;
    }

    public override bool Equals(object? obj) => Equals(obj as FaixaIrrf);

    public override int GetHashCode() => HashCode.Combine(LimiteInferior, LimiteSuperior, Aliquota, ParcelaADeduzir);

    public static bool operator ==(FaixaIrrf? esquerda, FaixaIrrf? direita)
    {
        if (esquerda is null && direita is null) return true;
        if (esquerda is null || direita is null) return false;
        return esquerda.Equals(direita);
    }

    public static bool operator !=(FaixaIrrf? esquerda, FaixaIrrf? direita) => !(esquerda == direita);

    // Comparação (por limite inferior)
    public int CompareTo(FaixaIrrf? outra)
    {
        if (outra is null) return 1;
        return LimiteInferior.CompareTo(outra.LimiteInferior);
    }

    public override string ToString()
    {
        var limSup = LimiteSuperior?.ToString() ?? "∞";
        if (EhFaixaIsenta)
            return $"{LimiteInferior} a {limSup} → Isento";
        return $"{LimiteInferior} a {limSup} → {Aliquota}% - {ParcelaADeduzir}";
    }
}
