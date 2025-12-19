namespace FolhadePagamento.Dominio.ValueObjects;

/// <summary>
/// Value Object representando o valor de dedução por dependente com vigência.
/// 
/// O valor de dedução por dependente é definido pela legislação e pode
/// mudar a cada ano. Este Value Object encapsula:
/// - O valor unitário da dedução
/// - A vigência em que este valor é aplicável
/// 
/// REGRAS DE NEGÓCIO:
/// - O valor é aplicado POR DEPENDENTE no cálculo do IRRF
/// - A dedução é subtraída da base de cálculo do IRRF ANTES da aplicação da alíquota
/// - Fórmula: BaseAjustada = BaseOriginal - (ValorDeducao × NumeroDependentes)
/// 
/// VALORES HISTÓRICOS:
/// - 2024: R$ 189,59 por dependente
/// - 2025: R$ 189,59 por dependente (mantido)
/// 
/// Imutável por design.
/// </summary>
public sealed class DeducaoDependente : IEquatable<DeducaoDependente>
{
    /// <summary>
    /// Identificador da dedução (ex: "DEDUCAO-DEP-2025").
    /// </summary>
    public string Identificador { get; }

    /// <summary>
    /// Descrição legível (ex: "Dedução por dependente IRRF 2025").
    /// </summary>
    public string Descricao { get; }

    /// <summary>
    /// Valor unitário da dedução por cada dependente.
    /// </summary>
    public Dinheiro ValorUnitario { get; }

    /// <summary>
    /// Vigência em que este valor de dedução é aplicável.
    /// </summary>
    public Vigencia Vigencia { get; }

    private DeducaoDependente(string identificador, string descricao, Dinheiro valorUnitario, Vigencia vigencia)
    {
        Identificador = identificador;
        Descricao = descricao;
        ValorUnitario = valorUnitario;
        Vigencia = vigencia;
    }

    /// <summary>
    /// Cria uma dedução por dependente com validação.
    /// </summary>
    /// <param name="identificador">Identificador único</param>
    /// <param name="descricao">Descrição legível</param>
    /// <param name="valorUnitario">Valor por cada dependente</param>
    /// <param name="vigencia">Período de vigência</param>
    public static DeducaoDependente Criar(
        string identificador,
        string descricao,
        Dinheiro valorUnitario,
        Vigencia vigencia)
    {
        if (string.IsNullOrWhiteSpace(identificador))
            throw new ArgumentException("Identificador é obrigatório", nameof(identificador));

        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória", nameof(descricao));

        if (valorUnitario is null)
            throw new ArgumentNullException(nameof(valorUnitario));

        if (valorUnitario.Valor < 0)
            throw new ArgumentException("Valor da dedução não pode ser negativo", nameof(valorUnitario));

        if (vigencia is null)
            throw new ArgumentNullException(nameof(vigencia));

        return new DeducaoDependente(identificador, descricao, valorUnitario, vigencia);
    }

    /// <summary>
    /// Calcula o valor total de dedução para um número de dependentes.
    /// </summary>
    /// <param name="numeroDependentes">Quantidade de dependentes</param>
    /// <returns>Valor total da dedução</returns>
    public Dinheiro CalcularDeducaoTotal(int numeroDependentes)
    {
        if (numeroDependentes < 0)
            throw new ArgumentOutOfRangeException(nameof(numeroDependentes), "Número de dependentes não pode ser negativo");

        if (numeroDependentes == 0)
            return Dinheiro.Zero;

        return ValorUnitario.Multiplicar(numeroDependentes);
    }

    /// <summary>
    /// Verifica se esta dedução está vigente para uma competência.
    /// </summary>
    /// <param name="competencia">Competência a verificar</param>
    /// <returns>True se vigente</returns>
    public bool EstaVigenteParaCompetencia(Competencia competencia)
    {
        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        return Vigencia.EstaVigenteEm(competencia.PrimeiroDia);
    }

    /// <summary>
    /// Cria a dedução por dependente padrão para 2025.
    /// </summary>
    public static DeducaoDependente Criar2025()
    {
        return Criar(
            "DEDUCAO-DEP-2025",
            "Dedução por dependente IRRF 2025",
            Dinheiro.DeDecimal(189.59m),
            Vigencia.Indefinida(new DateTime(2025, 1, 1)));
    }

    /// <summary>
    /// Cria a dedução por dependente padrão para 2024.
    /// </summary>
    public static DeducaoDependente Criar2024()
    {
        return Criar(
            "DEDUCAO-DEP-2024",
            "Dedução por dependente IRRF 2024",
            Dinheiro.DeDecimal(189.59m),
            Vigencia.Criar(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));
    }

    #region Igualdade

    public bool Equals(DeducaoDependente? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Identificador == other.Identificador &&
               ValorUnitario.Equals(other.ValorUnitario) &&
               Vigencia.Equals(other.Vigencia);
    }

    public override bool Equals(object? obj) => Equals(obj as DeducaoDependente);

    public override int GetHashCode() => HashCode.Combine(Identificador, ValorUnitario, Vigencia);

    public static bool operator ==(DeducaoDependente? esquerda, DeducaoDependente? direita) =>
        esquerda is null ? direita is null : esquerda.Equals(direita);

    public static bool operator !=(DeducaoDependente? esquerda, DeducaoDependente? direita) => !(esquerda == direita);

    #endregion

    public override string ToString() => $"{Descricao}: {ValorUnitario} por dependente";
}
