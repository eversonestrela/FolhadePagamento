using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Inss;

/// <summary>
/// Value Object representando uma faixa da tabela progressiva do INSS.
/// 
/// O INSS brasileiro é calculado de forma progressiva:
/// - Cada faixa tem um limite inferior, limite superior e alíquota
/// - O desconto incide apenas sobre a parte do salário dentro da faixa
/// 
/// Exemplo (valores ilustrativos):
/// - Faixa 1: R$ 0,00 a R$ 1.412,00 → 7,5%
/// - Faixa 2: R$ 1.412,01 a R$ 2.666,68 → 9%
/// - Faixa 3: R$ 2.666,69 a R$ 4.000,03 → 12%
/// - Faixa 4: R$ 4.000,04 a R$ 7.786,02 → 14%
/// 
/// Imutável por design.
/// </summary>
public sealed class FaixaInss : IEquatable<FaixaInss>, IComparable<FaixaInss>
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

    private FaixaInss(Dinheiro limiteInferior, Dinheiro? limiteSuperior, decimal aliquota)
    {
        LimiteInferior = limiteInferior;
        LimiteSuperior = limiteSuperior;
        Aliquota = aliquota;
    }

    /// <summary>
    /// Cria uma faixa de INSS.
    /// </summary>
    /// <param name="limiteInferior">Limite inferior (inclusive)</param>
    /// <param name="limiteSuperior">Limite superior (inclusive). Null para última faixa.</param>
    /// <param name="aliquota">Alíquota em percentual (ex: 7.5 para 7,5%)</param>
    public static FaixaInss Criar(Dinheiro limiteInferior, Dinheiro? limiteSuperior, decimal aliquota)
    {
        if (limiteInferior is null)
            throw new ArgumentNullException(nameof(limiteInferior));

        if (aliquota < 0 || aliquota > 100)
            throw new ArgumentOutOfRangeException(nameof(aliquota), "Alíquota deve estar entre 0 e 100");

        if (limiteSuperior is not null && limiteSuperior < limiteInferior)
            throw new ArgumentException("Limite superior não pode ser menor que limite inferior", nameof(limiteSuperior));

        return new FaixaInss(limiteInferior, limiteSuperior, aliquota);
    }

    /// <summary>
    /// Calcula o valor do INSS para um salário dentro desta faixa.
    /// Retorna apenas a contribuição referente a ESTA faixa.
    /// </summary>
    /// <param name="salarioBruto">Salário bruto total</param>
    /// <returns>Valor do INSS referente a esta faixa</returns>
    public Dinheiro CalcularContribuicaoFaixa(Dinheiro salarioBruto)
    {
        if (salarioBruto is null)
            throw new ArgumentNullException(nameof(salarioBruto));

        // Se salário é menor que o limite inferior, não há contribuição nesta faixa
        if (salarioBruto < LimiteInferior)
            return Dinheiro.Zero;

        // Determinar o valor que está dentro desta faixa
        var valorNaFaixa = CalcularValorNaFaixa(salarioBruto);

        // Aplicar a alíquota
        var contribuicao = valorNaFaixa.Multiplicar(Aliquota / 100m);

        return contribuicao;
    }

    /// <summary>
    /// Calcula quanto do salário está dentro desta faixa.
    /// </summary>
    private Dinheiro CalcularValorNaFaixa(Dinheiro salarioBruto)
    {
        // Valor que excede o limite inferior
        var valorAcimaDoInferior = salarioBruto.Valor - LimiteInferior.Valor;

        if (valorAcimaDoInferior <= 0)
            return Dinheiro.Zero;

        // Se não há limite superior (última faixa) ou salário está abaixo
        if (LimiteSuperior is null)
            return Dinheiro.DeDecimal(valorAcimaDoInferior);

        // Amplitude da faixa
        var amplitudeFaixa = LimiteSuperior.Valor - LimiteInferior.Valor;

        // Se salário ultrapassa o limite superior, usa toda a amplitude
        if (salarioBruto.Valor >= LimiteSuperior.Valor)
            return Dinheiro.DeDecimal(amplitudeFaixa);

        // Caso contrário, usa apenas o que está dentro
        return Dinheiro.DeDecimal(valorAcimaDoInferior);
    }

    /// <summary>
    /// Verifica se um salário está dentro desta faixa.
    /// </summary>
    public bool SalarioEstaNaFaixa(Dinheiro salario)
    {
        if (salario is null)
            throw new ArgumentNullException(nameof(salario));

        if (salario < LimiteInferior)
            return false;

        if (LimiteSuperior is not null && salario > LimiteSuperior)
            return false;

        return true;
    }

    // Igualdade
    public bool Equals(FaixaInss? outra)
    {
        if (outra is null) return false;
        return LimiteInferior == outra.LimiteInferior
            && LimiteSuperior == outra.LimiteSuperior
            && Aliquota == outra.Aliquota;
    }

    public override bool Equals(object? obj) => Equals(obj as FaixaInss);

    public override int GetHashCode() => HashCode.Combine(LimiteInferior, LimiteSuperior, Aliquota);

    public static bool operator ==(FaixaInss? esquerda, FaixaInss? direita)
    {
        if (esquerda is null && direita is null) return true;
        if (esquerda is null || direita is null) return false;
        return esquerda.Equals(direita);
    }

    public static bool operator !=(FaixaInss? esquerda, FaixaInss? direita) => !(esquerda == direita);

    // Comparação (por limite inferior)
    public int CompareTo(FaixaInss? outra)
    {
        if (outra is null) return 1;
        return LimiteInferior.CompareTo(outra.LimiteInferior);
    }

    public override string ToString()
    {
        var limSup = LimiteSuperior?.ToString() ?? "∞";
        return $"{LimiteInferior} a {limSup} → {Aliquota}%";
    }
}
