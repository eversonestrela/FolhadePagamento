using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Inss;

/// <summary>
/// Serviço de Domínio responsável pelo cálculo do INSS.
/// 
/// GARANTIA DE DETERMINISMO:
/// - Mesmas entradas SEMPRE produzem mesmas saídas
/// - Sem uso de DateTime.Now
/// - Sem dependências externas (BD, HTTP, etc.)
/// - Sem efeitos colaterais
/// 
/// O cálculo segue o modelo progressivo brasileiro:
/// - Cada faixa tem sua própria alíquota
/// - O valor em cada faixa é calculado separadamente
/// - A soma das contribuições de todas as faixas é o INSS total
/// - Existe um teto de contribuição (salários acima usam o teto como base)
/// </summary>
public sealed class CalculadoraInss
{
    private readonly IReadOnlyList<TabelaInss> _tabelas;

    /// <summary>
    /// Cria uma calculadora de INSS com as tabelas disponíveis.
    /// </summary>
    /// <param name="tabelas">Tabelas de INSS ordenadas por vigência</param>
    public CalculadoraInss(IEnumerable<TabelaInss> tabelas)
    {
        if (tabelas is null)
            throw new ArgumentNullException(nameof(tabelas));

        _tabelas = tabelas.ToList();

        if (_tabelas.Count == 0)
            throw new ArgumentException("Deve haver pelo menos uma tabela de INSS", nameof(tabelas));
    }

    /// <summary>
    /// Cria uma calculadora com as tabelas padrão (2024 e 2025).
    /// </summary>
    public static CalculadoraInss CriarComTabelasPadrao()
    {
        return new CalculadoraInss(new[]
        {
            TabelaInss.CriarTabela2024(),
            TabelaInss.CriarTabela2025()
        });
    }

    /// <summary>
    /// Calcula o INSS para um salário bruto em uma competência específica.
    /// Seleciona automaticamente a tabela vigente para a competência.
    /// </summary>
    /// <param name="salarioBruto">Salário bruto do funcionário</param>
    /// <param name="competencia">Competência do cálculo</param>
    /// <returns>Resultado do cálculo com detalhamento</returns>
    /// <exception cref="InvalidOperationException">Se não há tabela vigente para a competência</exception>
    public ResultadoCalculoInss Calcular(Dinheiro salarioBruto, Competencia competencia)
    {
        if (salarioBruto is null)
            throw new ArgumentNullException(nameof(salarioBruto));

        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        // Encontrar tabela vigente para a competência
        var tabelaVigente = ObterTabelaVigente(competencia);

        // Calcular usando a tabela vigente
        return tabelaVigente.Calcular(salarioBruto);
    }

    /// <summary>
    /// Obtém a tabela vigente para uma competência.
    /// </summary>
    /// <param name="competencia">Competência desejada</param>
    /// <returns>Tabela vigente</returns>
    /// <exception cref="InvalidOperationException">Se não há tabela vigente</exception>
    public TabelaInss ObterTabelaVigente(Competencia competencia)
    {
        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        var tabelaVigente = _tabelas
            .FirstOrDefault(t => t.EstaVigenteParaCompetencia(competencia));

        if (tabelaVigente is null)
            throw new InvalidOperationException(
                $"Não há tabela de INSS vigente para a competência {competencia}");

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
    public IReadOnlyList<TabelaInss> ObterTodasTabelas() => _tabelas;
}
