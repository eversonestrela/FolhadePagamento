using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Irrf;

/// <summary>
/// Serviço de Domínio responsável pelo cálculo do IRRF.
/// 
/// GARANTIA DE DETERMINISMO:
/// - Mesmas entradas SEMPRE produzem mesmas saídas
/// - Sem uso de DateTime.Now
/// - Sem dependências externas (BD, HTTP, etc.)
/// - Sem efeitos colaterais
/// 
/// REGRAS DE CÁLCULO:
/// 1. Base de cálculo = Salário Bruto - INSS - Outras deduções legais
/// 2. Deduzir valor por dependente (quantidade × valor por dependente)
/// 3. Encontrar faixa correspondente na tabela
/// 4. Aplicar fórmula: (Base Ajustada × Alíquota%) - Parcela a Deduzir
/// 
/// IMPORTANTE:
/// - Este serviço NÃO calcula INSS. O INSS deve ser calculado antes.
/// - A base de cálculo já deve vir com o INSS descontado.
/// </summary>
public sealed class CalculadoraIrrf
{
    private readonly IReadOnlyList<TabelaIrrf> _tabelas;

    /// <summary>
    /// Cria uma calculadora de IRRF com as tabelas disponíveis.
    /// </summary>
    /// <param name="tabelas">Tabelas de IRRF ordenadas por vigência</param>
    public CalculadoraIrrf(IEnumerable<TabelaIrrf> tabelas)
    {
        if (tabelas is null)
            throw new ArgumentNullException(nameof(tabelas));

        _tabelas = tabelas.ToList();

        if (_tabelas.Count == 0)
            throw new ArgumentException("Deve haver pelo menos uma tabela de IRRF", nameof(tabelas));
    }

    /// <summary>
    /// Cria uma calculadora com as tabelas padrão (2024 e 2025).
    /// </summary>
    public static CalculadoraIrrf CriarComTabelasPadrao()
    {
        return new CalculadoraIrrf(new[]
        {
            TabelaIrrf.CriarTabela2024(),
            TabelaIrrf.CriarTabela2025()
        });
    }

    /// <summary>
    /// Calcula o IRRF para uma base de cálculo em uma competência específica.
    /// Seleciona automaticamente a tabela vigente para a competência.
    /// 
    /// IMPORTANTE: A baseCalculo deve ser o valor APÓS descontar o INSS.
    /// Base = Salário Bruto - INSS
    /// </summary>
    /// <param name="baseCalculo">Base de cálculo (Bruto - INSS)</param>
    /// <param name="competencia">Competência do cálculo</param>
    /// <param name="numeroDependentes">Número de dependentes para dedução</param>
    /// <returns>Resultado do cálculo com detalhamento</returns>
    /// <exception cref="InvalidOperationException">Se não há tabela vigente para a competência</exception>
    public ResultadoCalculoIrrf Calcular(Dinheiro baseCalculo, Competencia competencia, int numeroDependentes = 0)
    {
        if (baseCalculo is null)
            throw new ArgumentNullException(nameof(baseCalculo));

        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        if (numeroDependentes < 0)
            throw new ArgumentOutOfRangeException(nameof(numeroDependentes), "Número de dependentes não pode ser negativo");

        // Encontrar tabela vigente para a competência
        var tabelaVigente = ObterTabelaVigente(competencia);

        // Calcular usando a tabela vigente
        return tabelaVigente.Calcular(baseCalculo, numeroDependentes);
    }

    /// <summary>
    /// Obtém a tabela vigente para uma competência.
    /// </summary>
    /// <param name="competencia">Competência desejada</param>
    /// <returns>Tabela vigente</returns>
    /// <exception cref="InvalidOperationException">Se não há tabela vigente</exception>
    public TabelaIrrf ObterTabelaVigente(Competencia competencia)
    {
        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        var tabelaVigente = _tabelas
            .FirstOrDefault(t => t.EstaVigenteParaCompetencia(competencia));

        if (tabelaVigente is null)
            throw new InvalidOperationException(
                $"Não há tabela de IRRF vigente para a competência {competencia}");

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
    public IReadOnlyList<TabelaIrrf> ObterTodasTabelas() => _tabelas;
}
