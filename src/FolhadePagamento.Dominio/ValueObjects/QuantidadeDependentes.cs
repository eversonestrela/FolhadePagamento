namespace FolhadePagamento.Dominio.ValueObjects;

/// <summary>
/// Value Object representando a quantidade de dependentes para fins de IRRF.
/// 
/// Este Value Object encapsula APENAS o impacto financeiro dos dependentes
/// no cálculo do IRRF, NÃO contendo dados cadastrais (CPF, Nome, etc.).
/// 
/// DESIGN ARQUITETURAL:
/// - O Core de cálculo conhece apenas a QUANTIDADE de dependentes
/// - Dados pessoais (CPF, Nome, DataNascimento) ficam na camada de Cadastro/Infra
/// - A validação de elegibilidade de dependentes é feita ANTES de chegar ao Core
/// 
/// REGRAS DE NEGÓCIO:
/// - Cada dependente gera uma dedução fixa na base de cálculo do IRRF
/// - Dependentes NÃO impactam o cálculo do INSS
/// - O valor da dedução por dependente é definido pela tabela vigente
/// 
/// Imutável por design.
/// </summary>
public readonly struct QuantidadeDependentes : IEquatable<QuantidadeDependentes>
{
    /// <summary>
    /// Quantidade de dependentes.
    /// </summary>
    public int Valor { get; }

    private QuantidadeDependentes(int valor)
    {
        Valor = valor;
    }

    /// <summary>
    /// Cria uma quantidade de dependentes com validação.
    /// </summary>
    /// <param name="quantidade">Número de dependentes (>= 0)</param>
    /// <exception cref="ArgumentOutOfRangeException">Se quantidade for negativa</exception>
    public static QuantidadeDependentes De(int quantidade)
    {
        if (quantidade < 0)
            throw new ArgumentOutOfRangeException(nameof(quantidade), "Quantidade de dependentes não pode ser negativa");

        return new QuantidadeDependentes(quantidade);
    }

    /// <summary>
    /// Quantidade zero (sem dependentes).
    /// </summary>
    public static QuantidadeDependentes Zero => new(0);

    /// <summary>
    /// Indica se há dependentes.
    /// </summary>
    public bool TemDependentes => Valor > 0;

    /// <summary>
    /// Indica se não há dependentes.
    /// </summary>
    public bool SemDependentes => Valor == 0;

    /// <summary>
    /// Calcula o valor total da dedução para esta quantidade.
    /// </summary>
    /// <param name="valorUnitario">Valor de dedução por dependente</param>
    /// <returns>Valor total da dedução</returns>
    public Dinheiro CalcularDeducaoTotal(Dinheiro valorUnitario)
    {
        if (valorUnitario is null)
            throw new ArgumentNullException(nameof(valorUnitario));

        if (SemDependentes)
            return Dinheiro.Zero;

        return valorUnitario.Multiplicar(Valor);
    }

    #region Conversão Implícita

    /// <summary>
    /// Conversão implícita de int para QuantidadeDependentes.
    /// </summary>
    public static implicit operator QuantidadeDependentes(int quantidade) => De(quantidade);

    /// <summary>
    /// Conversão implícita de QuantidadeDependentes para int.
    /// </summary>
    public static implicit operator int(QuantidadeDependentes quantidade) => quantidade.Valor;

    #endregion

    #region Igualdade

    public bool Equals(QuantidadeDependentes other) => Valor == other.Valor;

    public override bool Equals(object? obj) => obj is QuantidadeDependentes other && Equals(other);

    public override int GetHashCode() => Valor.GetHashCode();

    public static bool operator ==(QuantidadeDependentes esquerda, QuantidadeDependentes direita) => esquerda.Equals(direita);

    public static bool operator !=(QuantidadeDependentes esquerda, QuantidadeDependentes direita) => !esquerda.Equals(direita);

    #endregion

    public override string ToString() => Valor == 1 ? "1 dependente" : $"{Valor} dependentes";
}
