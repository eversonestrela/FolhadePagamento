using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Inss;

/// <summary>
/// Value Object representando uma tabela completa de INSS com vigência.
/// 
/// A tabela contém múltiplas faixas progressivas e uma vigência que define
/// quando esta tabela é válida.
/// 
/// Exemplo: Tabela INSS 2025
/// - Vigência: 01/01/2025 (indefinida)
/// - Faixas:
///   - R$ 0,00 a R$ 1.518,00 → 7,5%
///   - R$ 1.518,01 a R$ 2.793,88 → 9%
///   - R$ 2.793,89 a R$ 4.190,83 → 12%
///   - R$ 4.190,84 a R$ 8.157,41 → 14%
/// 
/// Imutável por design.
/// </summary>
public sealed class TabelaInss
{
    /// <summary>
    /// Identificador da tabela.
    /// </summary>
    public string Identificador { get; }

    /// <summary>
    /// Descrição da tabela (ex: "Tabela INSS 2025").
    /// </summary>
    public string Descricao { get; }

    /// <summary>
    /// Vigência desta tabela.
    /// </summary>
    public Vigencia Vigencia { get; }

    /// <summary>
    /// Faixas progressivas ordenadas por limite inferior.
    /// </summary>
    public IReadOnlyList<FaixaInss> Faixas { get; }

    /// <summary>
    /// Teto de contribuição do INSS (limite máximo de base de cálculo).
    /// Salários acima deste valor usam o teto como base.
    /// </summary>
    public Dinheiro Teto { get; }

    private TabelaInss(
        string identificador,
        string descricao,
        Vigencia vigencia,
        IReadOnlyList<FaixaInss> faixas,
        Dinheiro teto)
    {
        Identificador = identificador;
        Descricao = descricao;
        Vigencia = vigencia;
        Faixas = faixas;
        Teto = teto;
    }

    /// <summary>
    /// Cria uma tabela de INSS.
    /// </summary>
    /// <param name="identificador">Identificador único da tabela</param>
    /// <param name="descricao">Descrição legível</param>
    /// <param name="vigencia">Período de vigência</param>
    /// <param name="faixas">Faixas progressivas</param>
    /// <param name="teto">Teto de contribuição</param>
    public static TabelaInss Criar(
        string identificador,
        string descricao,
        Vigencia vigencia,
        IEnumerable<FaixaInss> faixas,
        Dinheiro teto)
    {
        if (string.IsNullOrWhiteSpace(identificador))
            throw new ArgumentException("Identificador é obrigatório", nameof(identificador));

        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória", nameof(descricao));

        if (vigencia is null)
            throw new ArgumentNullException(nameof(vigencia));

        if (faixas is null)
            throw new ArgumentNullException(nameof(faixas));

        if (teto is null)
            throw new ArgumentNullException(nameof(teto));

        var listaFaixas = faixas.OrderBy(f => f.LimiteInferior).ToList();

        if (listaFaixas.Count == 0)
            throw new ArgumentException("Tabela deve ter pelo menos uma faixa", nameof(faixas));

        // Validar que a primeira faixa começa em zero
        if (listaFaixas[0].LimiteInferior.Valor != 0)
            throw new ArgumentException("A primeira faixa deve começar em R$ 0,00", nameof(faixas));

        return new TabelaInss(identificador, descricao, vigencia, listaFaixas, teto);
    }

    /// <summary>
    /// Verifica se esta tabela está vigente para uma competência.
    /// </summary>
    public bool EstaVigenteParaCompetencia(Competencia competencia)
    {
        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        return Vigencia.EstaVigenteParaCompetencia(competencia);
    }

    /// <summary>
    /// Calcula o INSS total para um salário bruto.
    /// Aplica cálculo progressivo somando contribuição de cada faixa.
    /// </summary>
    /// <param name="salarioBruto">Salário bruto do funcionário</param>
    /// <returns>Resultado do cálculo com valor e detalhamento</returns>
    public ResultadoCalculoInss Calcular(Dinheiro salarioBruto)
    {
        if (salarioBruto is null)
            throw new ArgumentNullException(nameof(salarioBruto));

        // Aplicar teto: se salário > teto, usar teto como base
        var baseCalculo = salarioBruto.Valor > Teto.Valor
            ? Teto
            : salarioBruto;

        var contribuicaoTotal = Dinheiro.Zero;
        var detalhamentoFaixas = new List<DetalheFaixaInss>();

        foreach (var faixa in Faixas)
        {
            var contribuicaoFaixa = faixa.CalcularContribuicaoFaixa(baseCalculo);

            if (contribuicaoFaixa.Valor > 0)
            {
                contribuicaoTotal = contribuicaoTotal.Somar(contribuicaoFaixa);

                detalhamentoFaixas.Add(new DetalheFaixaInss(
                    faixa.LimiteInferior,
                    faixa.LimiteSuperior,
                    faixa.Aliquota,
                    contribuicaoFaixa));
            }
        }

        return new ResultadoCalculoInss(
            salarioBruto,
            baseCalculo,
            contribuicaoTotal,
            Identificador,
            detalhamentoFaixas);
    }

    /// <summary>
    /// Cria a tabela INSS 2025 (valores oficiais).
    /// </summary>
    public static TabelaInss CriarTabela2025()
    {
        var vigencia = Vigencia.Indefinida(new DateTime(2025, 1, 1));
        var teto = Dinheiro.DeDecimal(8157.41m);

        var faixas = new[]
        {
            FaixaInss.Criar(Dinheiro.DeDecimal(0m), Dinheiro.DeDecimal(1518.00m), 7.5m),
            FaixaInss.Criar(Dinheiro.DeDecimal(1518.00m), Dinheiro.DeDecimal(2793.88m), 9m),
            FaixaInss.Criar(Dinheiro.DeDecimal(2793.88m), Dinheiro.DeDecimal(4190.83m), 12m),
            FaixaInss.Criar(Dinheiro.DeDecimal(4190.83m), Dinheiro.DeDecimal(8157.41m), 14m),
        };

        return Criar("INSS-2025", "Tabela INSS 2025", vigencia, faixas, teto);
    }

    /// <summary>
    /// Cria a tabela INSS 2024 (para testes de vigência).
    /// </summary>
    public static TabelaInss CriarTabela2024()
    {
        var vigencia = Vigencia.Criar(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        var teto = Dinheiro.DeDecimal(7786.02m);

        var faixas = new[]
        {
            FaixaInss.Criar(Dinheiro.DeDecimal(0m), Dinheiro.DeDecimal(1412.00m), 7.5m),
            FaixaInss.Criar(Dinheiro.DeDecimal(1412.00m), Dinheiro.DeDecimal(2666.68m), 9m),
            FaixaInss.Criar(Dinheiro.DeDecimal(2666.68m), Dinheiro.DeDecimal(4000.03m), 12m),
            FaixaInss.Criar(Dinheiro.DeDecimal(4000.03m), Dinheiro.DeDecimal(7786.02m), 14m),
        };

        return Criar("INSS-2024", "Tabela INSS 2024", vigencia, faixas, teto);
    }

    public override string ToString() => $"{Descricao} - Vigência: {Vigencia}";
}

/// <summary>
/// Resultado do cálculo de INSS com detalhamento por faixa.
/// Imutável - serve como memória de cálculo.
/// </summary>
public sealed class ResultadoCalculoInss
{
    /// <summary>
    /// Salário bruto original.
    /// </summary>
    public Dinheiro SalarioBruto { get; }

    /// <summary>
    /// Base de cálculo (pode ser menor que salário bruto se aplicou teto).
    /// </summary>
    public Dinheiro BaseCalculo { get; }

    /// <summary>
    /// Valor total do INSS calculado.
    /// </summary>
    public Dinheiro ValorInss { get; }

    /// <summary>
    /// Identificador da tabela usada.
    /// </summary>
    public string TabelaUtilizada { get; }

    /// <summary>
    /// Detalhamento do cálculo por faixa.
    /// </summary>
    public IReadOnlyList<DetalheFaixaInss> DetalhamentoPorFaixa { get; }

    public ResultadoCalculoInss(
        Dinheiro salarioBruto,
        Dinheiro baseCalculo,
        Dinheiro valorInss,
        string tabelaUtilizada,
        IReadOnlyList<DetalheFaixaInss> detalhamentoPorFaixa)
    {
        SalarioBruto = salarioBruto;
        BaseCalculo = baseCalculo;
        ValorInss = valorInss;
        TabelaUtilizada = tabelaUtilizada;
        DetalhamentoPorFaixa = detalhamentoPorFaixa;
    }

    public override string ToString() =>
        $"INSS: {ValorInss} (Base: {BaseCalculo}, Tabela: {TabelaUtilizada})";
}

/// <summary>
/// Detalhe do cálculo de uma faixa específica.
/// </summary>
public sealed class DetalheFaixaInss
{
    public Dinheiro LimiteInferior { get; }
    public Dinheiro? LimiteSuperior { get; }
    public decimal Aliquota { get; }
    public Dinheiro ValorContribuicao { get; }

    public DetalheFaixaInss(
        Dinheiro limiteInferior,
        Dinheiro? limiteSuperior,
        decimal aliquota,
        Dinheiro valorContribuicao)
    {
        LimiteInferior = limiteInferior;
        LimiteSuperior = limiteSuperior;
        Aliquota = aliquota;
        ValorContribuicao = valorContribuicao;
    }

    public override string ToString()
    {
        var limSup = LimiteSuperior?.ToString() ?? "∞";
        return $"{LimiteInferior} a {limSup} ({Aliquota}%) = {ValorContribuicao}";
    }
}
