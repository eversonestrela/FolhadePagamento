using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Consignados;

/// <summary>
/// Serviço de Domínio responsável pelo cálculo de consignados.
/// 
/// GARANTIA DE DETERMINISMO:
/// - Mesmas entradas SEMPRE produzem mesmas saídas
/// - Sem uso de DateTime.Now
/// - Sem dependências externas (BD, HTTP, etc.)
/// - Sem efeitos colaterais
/// 
/// REGRAS DE NEGÓCIO:
/// 1. Margem consignável é um percentual do salário líquido (padrão: 35%)
/// 2. Consignados são descontados em ordem de prioridade
/// 3. Se não há margem suficiente, desconto pode ser parcial ou bloqueado
/// 4. Nunca permite salário líquido negativo
/// 5. Respeita vigência dos contratos
/// </summary>
public sealed class CalculadoraConsignados
{
    /// <summary>
    /// Percentual padrão da margem consignável (35% do líquido).
    /// Baseado na legislação brasileira para empréstimos consignados.
    /// </summary>
    public const decimal PERCENTUAL_MARGEM_PADRAO = 35m;

    /// <summary>
    /// Percentual mínimo permitido.
    /// </summary>
    public const decimal PERCENTUAL_MARGEM_MINIMO = 0m;

    /// <summary>
    /// Percentual máximo permitido.
    /// </summary>
    public const decimal PERCENTUAL_MARGEM_MAXIMO = 100m;

    private readonly decimal _percentualMargem;

    /// <summary>
    /// Percentual da margem consignável configurado.
    /// </summary>
    public decimal PercentualMargem => _percentualMargem;

    /// <summary>
    /// Cria uma calculadora de consignados com percentual de margem padrão (35%).
    /// </summary>
    public CalculadoraConsignados() : this(PERCENTUAL_MARGEM_PADRAO)
    {
    }

    /// <summary>
    /// Cria uma calculadora de consignados com percentual de margem customizado.
    /// </summary>
    /// <param name="percentualMargem">Percentual da margem consignável (0-100)</param>
    public CalculadoraConsignados(decimal percentualMargem)
    {
        if (percentualMargem < PERCENTUAL_MARGEM_MINIMO || percentualMargem > PERCENTUAL_MARGEM_MAXIMO)
            throw new ArgumentOutOfRangeException(nameof(percentualMargem),
                $"Percentual de margem deve estar entre {PERCENTUAL_MARGEM_MINIMO}% e {PERCENTUAL_MARGEM_MAXIMO}%.");

        _percentualMargem = percentualMargem;
    }

    /// <summary>
    /// Cria uma calculadora com a margem padrão de 35%.
    /// </summary>
    public static CalculadoraConsignados CriarComMargemPadrao() =>
        new CalculadoraConsignados(PERCENTUAL_MARGEM_PADRAO);

    /// <summary>
    /// Calcula os descontos de consignados para uma folha.
    /// 
    /// ALGORITMO:
    /// 1. Calcula a margem consignável (percentual do líquido disponível)
    /// 2. Filtra contratos vigentes para a competência
    /// 3. Ordena por prioridade (menor número = maior prioridade)
    /// 4. Para cada contrato, tenta descontar respeitando a margem
    /// 5. Se margem insuficiente: desconto parcial ou bloqueado
    /// </summary>
    /// <param name="salarioDisponivelParaConsignado">Salário líquido disponível (após INSS/IRRF)</param>
    /// <param name="contratos">Lista de contratos de consignados ativos</param>
    /// <param name="competencia">Competência para verificar vigência</param>
    /// <returns>Resultado detalhado do cálculo de consignados</returns>
    public ResultadoCalculoConsignados Calcular(
        Dinheiro salarioDisponivelParaConsignado,
        IEnumerable<ContratoConsignado> contratos,
        Competencia competencia)
    {
        if (salarioDisponivelParaConsignado is null)
            throw new ArgumentNullException(nameof(salarioDisponivelParaConsignado));

        if (contratos is null)
            throw new ArgumentNullException(nameof(contratos));

        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        // Se salário disponível é zero ou negativo, não há margem
        if (salarioDisponivelParaConsignado.Valor <= 0)
        {
            return ResultadoCalculoConsignados.Vazio(Dinheiro.Zero, _percentualMargem);
        }

        // Calcula margem consignável
        var margemConsignavel = salarioDisponivelParaConsignado.MultiplicarPorPercentual(_percentualMargem);
        var margemDisponivel = margemConsignavel;

        // Filtra contratos vigentes e ordena por prioridade
        var contratosVigentes = contratos
            .Where(c => c.EstaVigenteParaCompetencia(competencia) && !c.EstaQuitado)
            .OrderBy(c => c.Prioridade)
            .ThenBy(c => c.Identificador) // Desempate determinístico
            .ToList();

        if (contratosVigentes.Count == 0)
        {
            return ResultadoCalculoConsignados.Vazio(salarioDisponivelParaConsignado, _percentualMargem);
        }

        var detalhes = new List<DetalheDescontoConsignado>();
        var totalDescontado = Dinheiro.Zero;

        foreach (var contrato in contratosVigentes)
        {
            DetalheDescontoConsignado detalhe;

            if (margemDisponivel.Valor <= 0)
            {
                // Sem margem disponível - bloqueia desconto
                detalhe = new DetalheDescontoConsignado(
                    contratoId: contrato.Identificador,
                    descricao: contrato.Descricao,
                    valorOriginal: contrato.ValorParcela,
                    valorDescontado: Dinheiro.Zero,
                    descontoParcial: false,
                    descontoBloqueado: true,
                    numeroParcela: contrato.ParcelaAtual,
                    totalParcelas: contrato.TotalParcelas,
                    prioridade: contrato.Prioridade);
            }
            else if (margemDisponivel.Valor >= contrato.ValorParcela.Valor)
            {
                // Margem suficiente - desconto integral
                detalhe = new DetalheDescontoConsignado(
                    contratoId: contrato.Identificador,
                    descricao: contrato.Descricao,
                    valorOriginal: contrato.ValorParcela,
                    valorDescontado: contrato.ValorParcela,
                    descontoParcial: false,
                    descontoBloqueado: false,
                    numeroParcela: contrato.ParcelaAtual,
                    totalParcelas: contrato.TotalParcelas,
                    prioridade: contrato.Prioridade);

                margemDisponivel = margemDisponivel.Subtrair(contrato.ValorParcela);
                totalDescontado = totalDescontado.Somar(contrato.ValorParcela);
            }
            else
            {
                // Margem insuficiente - desconto parcial
                var valorParcial = margemDisponivel;

                detalhe = new DetalheDescontoConsignado(
                    contratoId: contrato.Identificador,
                    descricao: contrato.Descricao,
                    valorOriginal: contrato.ValorParcela,
                    valorDescontado: valorParcial,
                    descontoParcial: true,
                    descontoBloqueado: false,
                    numeroParcela: contrato.ParcelaAtual,
                    totalParcelas: contrato.TotalParcelas,
                    prioridade: contrato.Prioridade);

                totalDescontado = totalDescontado.Somar(valorParcial);
                margemDisponivel = Dinheiro.Zero;
            }

            detalhes.Add(detalhe);
        }

        var margemUtilizada = margemConsignavel.Subtrair(margemDisponivel);

        return new ResultadoCalculoConsignados(
            salarioDisponivelAntes: salarioDisponivelParaConsignado,
            margemConsignavel: margemConsignavel,
            percentualMargem: _percentualMargem,
            margemUtilizada: margemUtilizada,
            margemDisponivel: margemDisponivel,
            totalDescontado: totalDescontado,
            detalhes: detalhes.AsReadOnly());
    }

    /// <summary>
    /// Calcula apenas a margem consignável para um salário.
    /// </summary>
    public Dinheiro CalcularMargem(Dinheiro salarioDisponivel)
    {
        if (salarioDisponivel is null)
            throw new ArgumentNullException(nameof(salarioDisponivel));

        if (salarioDisponivel.Valor <= 0)
            return Dinheiro.Zero;

        return salarioDisponivel.MultiplicarPorPercentual(_percentualMargem);
    }
}
