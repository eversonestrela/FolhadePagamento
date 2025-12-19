using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Irrf;

/// <summary>
/// Value Object representando uma tabela completa de IRRF com vigência.
/// 
/// A tabela contém múltiplas faixas progressivas, uma vigência que define
/// quando esta tabela é válida, e o valor de dedução por dependente.
/// 
/// Exemplo: Tabela IRRF 2025
/// - Vigência: 01/01/2025 (indefinida)
/// - Dedução por dependente: R$ 189,59
/// - Faixas progressivas com parcela a deduzir
/// 
/// Imutável por design.
/// </summary>
public sealed class TabelaIrrf
{
    /// <summary>
    /// Identificador da tabela.
    /// </summary>
    public string Identificador { get; }

    /// <summary>
    /// Descrição da tabela (ex: "Tabela IRRF 2025").
    /// </summary>
    public string Descricao { get; }

    /// <summary>
    /// Vigência desta tabela.
    /// </summary>
    public Vigencia Vigencia { get; }

    /// <summary>
    /// Faixas progressivas ordenadas por limite inferior.
    /// </summary>
    public IReadOnlyList<FaixaIrrf> Faixas { get; }

    /// <summary>
    /// Valor de dedução por dependente.
    /// </summary>
    public Dinheiro DeducaoPorDependente { get; }

    private TabelaIrrf(
        string identificador,
        string descricao,
        Vigencia vigencia,
        IReadOnlyList<FaixaIrrf> faixas,
        Dinheiro deducaoPorDependente)
    {
        Identificador = identificador;
        Descricao = descricao;
        Vigencia = vigencia;
        Faixas = faixas;
        DeducaoPorDependente = deducaoPorDependente;
    }

    /// <summary>
    /// Cria uma tabela de IRRF.
    /// </summary>
    public static TabelaIrrf Criar(
        string identificador,
        string descricao,
        Vigencia vigencia,
        IEnumerable<FaixaIrrf> faixas,
        Dinheiro deducaoPorDependente)
    {
        if (string.IsNullOrWhiteSpace(identificador))
            throw new ArgumentException("Identificador é obrigatório", nameof(identificador));

        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória", nameof(descricao));

        if (vigencia is null)
            throw new ArgumentNullException(nameof(vigencia));

        if (faixas is null)
            throw new ArgumentNullException(nameof(faixas));

        if (deducaoPorDependente is null)
            throw new ArgumentNullException(nameof(deducaoPorDependente));

        var listaFaixas = faixas.OrderBy(f => f.LimiteInferior).ToList();

        if (listaFaixas.Count == 0)
            throw new ArgumentException("Tabela deve ter pelo menos uma faixa", nameof(faixas));

        // Validar que a primeira faixa começa em zero
        if (listaFaixas[0].LimiteInferior.Valor != 0)
            throw new ArgumentException("A primeira faixa deve começar em R$ 0,00", nameof(faixas));

        return new TabelaIrrf(identificador, descricao, vigencia, listaFaixas, deducaoPorDependente);
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
    /// Encontra a faixa correspondente a uma base de cálculo.
    /// </summary>
    public FaixaIrrf EncontrarFaixa(Dinheiro baseCalculo)
    {
        if (baseCalculo is null)
            throw new ArgumentNullException(nameof(baseCalculo));

        // Percorre do maior para o menor para encontrar a faixa correta
        for (int i = Faixas.Count - 1; i >= 0; i--)
        {
            if (Faixas[i].BaseEstaNaFaixa(baseCalculo))
                return Faixas[i];
        }

        // Se não encontrou, retorna a primeira faixa (isenta)
        return Faixas[0];
    }

    /// <summary>
    /// Calcula o IRRF para uma base de cálculo.
    /// </summary>
    /// <param name="baseCalculo">Base de cálculo (Bruto - INSS - Deduções)</param>
    /// <param name="numeroDependentes">Número de dependentes para dedução</param>
    /// <returns>Resultado do cálculo com detalhamento</returns>
    public ResultadoCalculoIrrf Calcular(Dinheiro baseCalculo, int numeroDependentes = 0)
    {
        if (baseCalculo is null)
            throw new ArgumentNullException(nameof(baseCalculo));

        if (numeroDependentes < 0)
            throw new ArgumentOutOfRangeException(nameof(numeroDependentes), "Número de dependentes não pode ser negativo");

        // Calcular dedução por dependentes
        var deducaoDependentes = DeducaoPorDependente.Multiplicar(numeroDependentes);

        // Base de cálculo ajustada = Base - Dedução por dependentes
        Dinheiro baseAjustada;
        if (deducaoDependentes.Valor >= baseCalculo.Valor)
        {
            // Se dedução for maior que a base, considera zero
            baseAjustada = Dinheiro.Zero;
        }
        else
        {
            baseAjustada = baseCalculo.Subtrair(deducaoDependentes);
        }

        // Encontrar a faixa correspondente
        var faixaAplicavel = EncontrarFaixa(baseAjustada);

        // Calcular o imposto
        var valorIrrf = faixaAplicavel.CalcularImposto(baseAjustada);

        return new ResultadoCalculoIrrf(
            baseOriginal: baseCalculo,
            numeroDependentes: numeroDependentes,
            valorUnitarioPorDependente: DeducaoPorDependente,
            deducaoPorDependentes: deducaoDependentes,
            baseAjustada: baseAjustada,
            faixaAplicada: faixaAplicavel,
            aliquotaEfetiva: faixaAplicavel.Aliquota,
            parcelaADeduzir: faixaAplicavel.ParcelaADeduzir,
            valorIrrf: valorIrrf,
            tabelaUtilizada: Identificador);
    }

    /// <summary>
    /// Cria a tabela IRRF 2025 (valores oficiais).
    /// </summary>
    public static TabelaIrrf CriarTabela2025()
    {
        var vigencia = Vigencia.Indefinida(new DateTime(2025, 1, 1));
        var deducaoPorDependente = Dinheiro.DeDecimal(189.59m);

        var faixas = new[]
        {
            FaixaIrrf.CriarFaixaIsenta(Dinheiro.Zero, Dinheiro.DeDecimal(2259.20m)),
            FaixaIrrf.Criar(Dinheiro.DeDecimal(2259.20m), Dinheiro.DeDecimal(2826.65m), 7.5m, Dinheiro.DeDecimal(169.44m)),
            FaixaIrrf.Criar(Dinheiro.DeDecimal(2826.65m), Dinheiro.DeDecimal(3751.05m), 15m, Dinheiro.DeDecimal(381.44m)),
            FaixaIrrf.Criar(Dinheiro.DeDecimal(3751.05m), Dinheiro.DeDecimal(4664.68m), 22.5m, Dinheiro.DeDecimal(662.77m)),
            FaixaIrrf.Criar(Dinheiro.DeDecimal(4664.68m), null, 27.5m, Dinheiro.DeDecimal(896.00m)),
        };

        return Criar("IRRF-2025", "Tabela IRRF 2025", vigencia, faixas, deducaoPorDependente);
    }

    /// <summary>
    /// Cria a tabela IRRF 2024 (para testes de vigência).
    /// </summary>
    public static TabelaIrrf CriarTabela2024()
    {
        var vigencia = Vigencia.Criar(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        var deducaoPorDependente = Dinheiro.DeDecimal(189.59m);

        var faixas = new[]
        {
            FaixaIrrf.CriarFaixaIsenta(Dinheiro.Zero, Dinheiro.DeDecimal(2112.00m)),
            FaixaIrrf.Criar(Dinheiro.DeDecimal(2112.00m), Dinheiro.DeDecimal(2826.65m), 7.5m, Dinheiro.DeDecimal(158.40m)),
            FaixaIrrf.Criar(Dinheiro.DeDecimal(2826.65m), Dinheiro.DeDecimal(3751.05m), 15m, Dinheiro.DeDecimal(370.40m)),
            FaixaIrrf.Criar(Dinheiro.DeDecimal(3751.05m), Dinheiro.DeDecimal(4664.68m), 22.5m, Dinheiro.DeDecimal(651.73m)),
            FaixaIrrf.Criar(Dinheiro.DeDecimal(4664.68m), null, 27.5m, Dinheiro.DeDecimal(884.96m)),
        };

        return Criar("IRRF-2024", "Tabela IRRF 2024", vigencia, faixas, deducaoPorDependente);
    }

    public override string ToString() => $"{Descricao} - Vigência: {Vigencia}";
}

/// <summary>
/// Resultado do cálculo de IRRF com detalhamento completo.
/// Imutável - serve como memória de cálculo.
/// 
/// MEMÓRIA DE CÁLCULO:
/// Esta classe registra todos os passos do cálculo do IRRF,
/// permitindo auditoria e rastreabilidade completa.
/// 
/// Fórmula: IRRF = (BaseAjustada × Alíquota%) - ParcelaADeduzir
/// Onde: BaseAjustada = BaseOriginal - DeducaoPorDependentes
/// E: DeducaoPorDependentes = ValorUnitarioPorDependente × NumeroDependentes
/// </summary>
public sealed class ResultadoCalculoIrrf
{
    /// <summary>
    /// Base de cálculo original (Bruto - INSS).
    /// </summary>
    public Dinheiro BaseOriginal { get; }

    /// <summary>
    /// Número de dependentes informados.
    /// </summary>
    public int NumeroDependentes { get; }

    /// <summary>
    /// Valor unitário da dedução por cada dependente.
    /// Este valor vem da tabela vigente.
    /// </summary>
    public Dinheiro ValorUnitarioPorDependente { get; }

    /// <summary>
    /// Valor total deduzido por dependentes.
    /// Fórmula: ValorUnitarioPorDependente × NumeroDependentes
    /// </summary>
    public Dinheiro DeducaoPorDependentes { get; }

    /// <summary>
    /// Base de cálculo ajustada (após dedução de dependentes).
    /// Fórmula: BaseOriginal - DeducaoPorDependentes
    /// </summary>
    public Dinheiro BaseAjustada { get; }

    /// <summary>
    /// Faixa da tabela que foi aplicada.
    /// </summary>
    public FaixaIrrf FaixaAplicada { get; }

    /// <summary>
    /// Alíquota efetiva aplicada.
    /// </summary>
    public decimal AliquotaEfetiva { get; }

    /// <summary>
    /// Parcela a deduzir da faixa.
    /// </summary>
    public Dinheiro ParcelaADeduzir { get; }

    /// <summary>
    /// Valor final do IRRF calculado.
    /// Fórmula: (BaseAjustada × AlíquotaEfetiva%) - ParcelaADeduzir
    /// </summary>
    public Dinheiro ValorIrrf { get; }

    /// <summary>
    /// Identificador da tabela usada.
    /// </summary>
    public string TabelaUtilizada { get; }

    /// <summary>
    /// Indica se o resultado está isento de IRRF.
    /// </summary>
    public bool EhIsento => ValorIrrf.Valor == 0;

    public ResultadoCalculoIrrf(
        Dinheiro baseOriginal,
        int numeroDependentes,
        Dinheiro valorUnitarioPorDependente,
        Dinheiro deducaoPorDependentes,
        Dinheiro baseAjustada,
        FaixaIrrf faixaAplicada,
        decimal aliquotaEfetiva,
        Dinheiro parcelaADeduzir,
        Dinheiro valorIrrf,
        string tabelaUtilizada)
    {
        BaseOriginal = baseOriginal;
        NumeroDependentes = numeroDependentes;
        ValorUnitarioPorDependente = valorUnitarioPorDependente;
        DeducaoPorDependentes = deducaoPorDependentes;
        BaseAjustada = baseAjustada;
        FaixaAplicada = faixaAplicada;
        AliquotaEfetiva = aliquotaEfetiva;
        ParcelaADeduzir = parcelaADeduzir;
        ValorIrrf = valorIrrf;
        TabelaUtilizada = tabelaUtilizada;
    }

    public override string ToString()
    {
        var dependentesInfo = NumeroDependentes > 0 
            ? $", Dependentes: {NumeroDependentes} × {ValorUnitarioPorDependente} = {DeducaoPorDependentes}"
            : "";
        
        if (EhIsento)
            return $"IRRF: Isento (Base: {BaseAjustada}{dependentesInfo}, Tabela: {TabelaUtilizada})";
        
        return $"IRRF: {ValorIrrf} ({AliquotaEfetiva}% - {ParcelaADeduzir}) Base: {BaseAjustada}{dependentesInfo}, Tabela: {TabelaUtilizada}";
    }
}
