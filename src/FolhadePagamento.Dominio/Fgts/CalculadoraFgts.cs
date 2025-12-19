using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Fgts;

/// <summary>
/// Serviço de Domínio responsável pelo cálculo do FGTS.
/// 
/// GARANTIA DE DETERMINISMO:
/// - Mesmas entradas SEMPRE produzem mesmas saídas
/// - Sem uso de DateTime.Now
/// - Sem dependências externas (BD, HTTP, etc.)
/// - Sem efeitos colaterais
/// 
/// REGRAS DE CÁLCULO:
/// 1. Base de cálculo = Salário Bruto (+ adicionais futuros)
/// 2. Aplicar alíquota conforme tabela vigente (8% padrão, 2% aprendiz)
/// 3. FGTS = Base × Alíquota%
/// 
/// IMPORTANTE:
/// - FGTS é encargo PATRONAL (empregador)
/// - FGTS NÃO desconta do salário do funcionário
/// - FGTS NÃO impacta o salário líquido
/// </summary>
public sealed class CalculadoraFgts
{
    private readonly IReadOnlyList<TabelaFgts> _tabelas;

    /// <summary>
    /// Cria uma calculadora de FGTS com as tabelas disponíveis.
    /// </summary>
    /// <param name="tabelas">Tabelas de FGTS ordenadas por vigência</param>
    public CalculadoraFgts(IEnumerable<TabelaFgts> tabelas)
    {
        if (tabelas is null)
            throw new ArgumentNullException(nameof(tabelas));

        _tabelas = tabelas.ToList();

        if (_tabelas.Count == 0)
            throw new ArgumentException("Deve haver pelo menos uma tabela de FGTS", nameof(tabelas));
    }

    /// <summary>
    /// Cria uma calculadora com a tabela padrão.
    /// </summary>
    public static CalculadoraFgts CriarComTabelaPadrao()
    {
        return new CalculadoraFgts(new[] { TabelaFgts.CriarTabelaPadrao() });
    }

    /// <summary>
    /// Calcula o FGTS para uma base de cálculo em uma competência específica.
    /// </summary>
    /// <param name="baseCalculo">Base de cálculo (salário bruto)</param>
    /// <param name="competencia">Competência do cálculo</param>
    /// <param name="ehAprendiz">Se o funcionário é aprendiz</param>
    /// <returns>Resultado do cálculo com detalhamento</returns>
    public ResultadoCalculoFgts Calcular(Dinheiro baseCalculo, Competencia competencia, bool ehAprendiz = false)
    {
        if (baseCalculo is null)
            throw new ArgumentNullException(nameof(baseCalculo));

        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        var tabelaVigente = ObterTabelaVigente(competencia);
        return tabelaVigente.Calcular(baseCalculo, ehAprendiz);
    }

    /// <summary>
    /// Obtém a tabela vigente para uma competência.
    /// </summary>
    public TabelaFgts ObterTabelaVigente(Competencia competencia)
    {
        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        var tabelaVigente = _tabelas
            .FirstOrDefault(t => t.EstaVigenteParaCompetencia(competencia));

        if (tabelaVigente is null)
            throw new InvalidOperationException(
                $"Não há tabela de FGTS vigente para a competência {competencia}");

        return tabelaVigente;
    }

    /// <summary>
    /// Verifica se há tabela vigente para uma competência.
    /// </summary>
    public bool ExisteTabelaVigente(Competencia competencia)
    {
        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        return _tabelas.Any(t => t.EstaVigenteParaCompetencia(competencia));
    }

    /// <summary>
    /// Obtém todas as tabelas disponíveis.
    /// </summary>
    public IReadOnlyList<TabelaFgts> ObterTodasTabelas() => _tabelas;
}
