using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Fgts;

/// <summary>
/// Value Object representando uma tabela de FGTS com vigência.
/// 
/// O FGTS (Fundo de Garantia do Tempo de Serviço) é um encargo do empregador,
/// calculado sobre a remuneração bruta do funcionário.
/// 
/// REGRAS DE NEGÓCIO:
/// - Alíquota padrão: 8% sobre o salário bruto
/// - Alíquota para aprendizes: 2%
/// - FGTS é encargo PATRONAL (não desconta do funcionário)
/// - FGTS NÃO impacta o salário líquido
/// - Base de cálculo: salário bruto + adicionais
/// 
/// Imutável por design.
/// </summary>
public sealed class TabelaFgts : IEquatable<TabelaFgts>
{
    /// <summary>
    /// Identificador da tabela (ex: "FGTS-2025").
    /// </summary>
    public string Identificador { get; }

    /// <summary>
    /// Descrição da tabela.
    /// </summary>
    public string Descricao { get; }

    /// <summary>
    /// Vigência desta tabela.
    /// </summary>
    public Vigencia Vigencia { get; }

    /// <summary>
    /// Alíquota padrão do FGTS (geralmente 8%).
    /// </summary>
    public decimal AliquotaPadrao { get; }

    /// <summary>
    /// Alíquota para aprendizes (geralmente 2%).
    /// </summary>
    public decimal AliquotaAprendiz { get; }

    private TabelaFgts(
        string identificador,
        string descricao,
        Vigencia vigencia,
        decimal aliquotaPadrao,
        decimal aliquotaAprendiz)
    {
        Identificador = identificador;
        Descricao = descricao;
        Vigencia = vigencia;
        AliquotaPadrao = aliquotaPadrao;
        AliquotaAprendiz = aliquotaAprendiz;
    }

    /// <summary>
    /// Cria uma tabela de FGTS com validação.
    /// </summary>
    public static TabelaFgts Criar(
        string identificador,
        string descricao,
        Vigencia vigencia,
        decimal aliquotaPadrao,
        decimal aliquotaAprendiz)
    {
        if (string.IsNullOrWhiteSpace(identificador))
            throw new ArgumentException("Identificador é obrigatório", nameof(identificador));

        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória", nameof(descricao));

        if (vigencia is null)
            throw new ArgumentNullException(nameof(vigencia));

        if (aliquotaPadrao < 0 || aliquotaPadrao > 100)
            throw new ArgumentOutOfRangeException(nameof(aliquotaPadrao), "Alíquota deve estar entre 0 e 100");

        if (aliquotaAprendiz < 0 || aliquotaAprendiz > 100)
            throw new ArgumentOutOfRangeException(nameof(aliquotaAprendiz), "Alíquota deve estar entre 0 e 100");

        return new TabelaFgts(identificador, descricao, vigencia, aliquotaPadrao, aliquotaAprendiz);
    }

    /// <summary>
    /// Verifica se esta tabela está vigente para uma competência.
    /// </summary>
    public bool EstaVigenteParaCompetencia(Competencia competencia)
    {
        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        return Vigencia.EstaVigenteEm(competencia.PrimeiroDia);
    }

    /// <summary>
    /// Calcula o FGTS para uma base de cálculo.
    /// </summary>
    /// <param name="baseCalculo">Base de cálculo (salário bruto)</param>
    /// <param name="ehAprendiz">Se o funcionário é aprendiz</param>
    /// <returns>Resultado do cálculo com detalhamento</returns>
    public ResultadoCalculoFgts Calcular(Dinheiro baseCalculo, bool ehAprendiz = false)
    {
        if (baseCalculo is null)
            throw new ArgumentNullException(nameof(baseCalculo));

        var aliquotaAplicada = ehAprendiz ? AliquotaAprendiz : AliquotaPadrao;
        var valorFgts = baseCalculo.Multiplicar(aliquotaAplicada / 100m);

        return new ResultadoCalculoFgts(
            baseCalculo: baseCalculo,
            aliquotaAplicada: aliquotaAplicada,
            valorFgts: valorFgts,
            ehAprendiz: ehAprendiz,
            tabelaUtilizada: Identificador);
    }

    /// <summary>
    /// Cria a tabela FGTS padrão (válida desde 1990).
    /// A alíquota de 8% é constante desde a CF/88.
    /// </summary>
    public static TabelaFgts CriarTabelaPadrao()
    {
        // FGTS tem alíquota fixa desde 1990, então usamos vigência indefinida
        var vigencia = Vigencia.Indefinida(new DateTime(1990, 1, 1));

        return Criar(
            "FGTS-PADRAO",
            "Tabela FGTS Padrão (8%)",
            vigencia,
            aliquotaPadrao: 8m,
            aliquotaAprendiz: 2m);
    }

    #region Igualdade

    public bool Equals(TabelaFgts? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Identificador == other.Identificador &&
               AliquotaPadrao == other.AliquotaPadrao &&
               AliquotaAprendiz == other.AliquotaAprendiz &&
               Vigencia.Equals(other.Vigencia);
    }

    public override bool Equals(object? obj) => Equals(obj as TabelaFgts);

    public override int GetHashCode() => HashCode.Combine(Identificador, AliquotaPadrao, AliquotaAprendiz, Vigencia);

    public static bool operator ==(TabelaFgts? esquerda, TabelaFgts? direita) =>
        esquerda is null ? direita is null : esquerda.Equals(direita);

    public static bool operator !=(TabelaFgts? esquerda, TabelaFgts? direita) => !(esquerda == direita);

    #endregion

    public override string ToString() => $"{Descricao} - Alíquota: {AliquotaPadrao}%";
}

/// <summary>
/// Resultado do cálculo de FGTS com detalhamento completo.
/// Imutável - serve como memória de cálculo.
/// 
/// IMPORTANTE: O FGTS é encargo patronal e NÃO desconta do funcionário.
/// </summary>
public sealed class ResultadoCalculoFgts
{
    /// <summary>
    /// Base de cálculo utilizada (salário bruto).
    /// </summary>
    public Dinheiro BaseCalculo { get; }

    /// <summary>
    /// Alíquota aplicada no cálculo.
    /// </summary>
    public decimal AliquotaAplicada { get; }

    /// <summary>
    /// Valor calculado do FGTS.
    /// </summary>
    public Dinheiro ValorFgts { get; }

    /// <summary>
    /// Indica se foi aplicada alíquota de aprendiz.
    /// </summary>
    public bool EhAprendiz { get; }

    /// <summary>
    /// Identificador da tabela utilizada.
    /// </summary>
    public string TabelaUtilizada { get; }

    public ResultadoCalculoFgts(
        Dinheiro baseCalculo,
        decimal aliquotaAplicada,
        Dinheiro valorFgts,
        bool ehAprendiz,
        string tabelaUtilizada)
    {
        BaseCalculo = baseCalculo;
        AliquotaAplicada = aliquotaAplicada;
        ValorFgts = valorFgts;
        EhAprendiz = ehAprendiz;
        TabelaUtilizada = tabelaUtilizada;
    }

    public override string ToString()
    {
        var tipoContrato = EhAprendiz ? " (Aprendiz)" : "";
        return $"FGTS: {ValorFgts} ({AliquotaAplicada}% sobre {BaseCalculo}){tipoContrato}";
    }
}
