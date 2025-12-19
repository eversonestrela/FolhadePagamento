using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Consignados;

/// <summary>
/// Value Object que representa um contrato de consignado.
/// 
/// Um consignado é um empréstimo/desconto fixo mensal que é descontado
/// diretamente da folha de pagamento do funcionário.
/// 
/// REGRAS:
/// - Cada contrato possui um identificador único
/// - Valor da parcela é fixo durante a vigência
/// - Respeita vigência (início e fim)
/// - Prioridade determina ordem de desconto
/// </summary>
public sealed class ContratoConsignado : IEquatable<ContratoConsignado>
{
    /// <summary>
    /// Identificador único do contrato.
    /// </summary>
    public string Identificador { get; }

    /// <summary>
    /// Descrição do consignado (ex: "Empréstimo BB", "Plano Saúde").
    /// </summary>
    public string Descricao { get; }

    /// <summary>
    /// Valor da parcela mensal a ser descontada.
    /// </summary>
    public Dinheiro ValorParcela { get; }

    /// <summary>
    /// Número total de parcelas do contrato.
    /// </summary>
    public int TotalParcelas { get; }

    /// <summary>
    /// Número da parcela atual (para rastreamento).
    /// </summary>
    public int ParcelaAtual { get; }

    /// <summary>
    /// Vigência do contrato (quando o desconto está ativo).
    /// </summary>
    public Vigencia Vigencia { get; }

    /// <summary>
    /// Prioridade de desconto (1 = mais prioritário).
    /// Consignados com menor número são descontados primeiro.
    /// </summary>
    public int Prioridade { get; }

    private ContratoConsignado(
        string identificador,
        string descricao,
        Dinheiro valorParcela,
        int totalParcelas,
        int parcelaAtual,
        Vigencia vigencia,
        int prioridade)
    {
        Identificador = identificador;
        Descricao = descricao;
        ValorParcela = valorParcela;
        TotalParcelas = totalParcelas;
        ParcelaAtual = parcelaAtual;
        Vigencia = vigencia;
        Prioridade = prioridade;
    }

    /// <summary>
    /// Cria um novo contrato de consignado.
    /// </summary>
    /// <param name="identificador">Identificador único do contrato</param>
    /// <param name="descricao">Descrição do consignado</param>
    /// <param name="valorParcela">Valor da parcela mensal</param>
    /// <param name="totalParcelas">Total de parcelas</param>
    /// <param name="parcelaAtual">Parcela atual</param>
    /// <param name="vigencia">Vigência do contrato</param>
    /// <param name="prioridade">Prioridade de desconto (1 = mais prioritário)</param>
    public static ContratoConsignado Criar(
        string identificador,
        string descricao,
        Dinheiro valorParcela,
        int totalParcelas,
        int parcelaAtual,
        Vigencia vigencia,
        int prioridade = 1)
    {
        if (string.IsNullOrWhiteSpace(identificador))
            throw new ArgumentException("Identificador é obrigatório.", nameof(identificador));

        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória.", nameof(descricao));

        if (valorParcela is null)
            throw new ArgumentNullException(nameof(valorParcela));

        if (valorParcela.Valor <= 0)
            throw new ArgumentOutOfRangeException(nameof(valorParcela), "Valor da parcela deve ser positivo.");

        if (totalParcelas <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalParcelas), "Total de parcelas deve ser positivo.");

        if (parcelaAtual <= 0 || parcelaAtual > totalParcelas)
            throw new ArgumentOutOfRangeException(nameof(parcelaAtual), 
                $"Parcela atual deve estar entre 1 e {totalParcelas}.");

        if (vigencia is null)
            throw new ArgumentNullException(nameof(vigencia));

        if (prioridade <= 0)
            throw new ArgumentOutOfRangeException(nameof(prioridade), "Prioridade deve ser positiva.");

        return new ContratoConsignado(
            identificador,
            descricao,
            valorParcela,
            totalParcelas,
            parcelaAtual,
            vigencia,
            prioridade);
    }

    /// <summary>
    /// Verifica se o contrato está vigente para uma determinada competência.
    /// </summary>
    public bool EstaVigenteParaCompetencia(Competencia competencia)
    {
        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        return Vigencia.EstaVigenteParaCompetencia(competencia);
    }

    /// <summary>
    /// Verifica se o contrato já foi quitado (todas as parcelas pagas).
    /// </summary>
    public bool EstaQuitado => ParcelaAtual > TotalParcelas;

    /// <summary>
    /// Parcelas restantes.
    /// </summary>
    public int ParcelasRestantes => Math.Max(0, TotalParcelas - ParcelaAtual + 1);

    #region Igualdade

    public bool Equals(ContratoConsignado? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Identificador == other.Identificador;
    }

    public override bool Equals(object? obj) => Equals(obj as ContratoConsignado);

    public override int GetHashCode() => Identificador.GetHashCode();

    public static bool operator ==(ContratoConsignado? left, ContratoConsignado? right) =>
        Equals(left, right);

    public static bool operator !=(ContratoConsignado? left, ContratoConsignado? right) =>
        !Equals(left, right);

    #endregion

    public override string ToString() =>
        $"{Descricao} ({Identificador}): {ValorParcela} - Parcela {ParcelaAtual}/{TotalParcelas} - Prioridade {Prioridade}";
}

/// <summary>
/// Representa o detalhe de um desconto de consignado aplicado.
/// Usado para memória de cálculo.
/// </summary>
public sealed class DetalheDescontoConsignado
{
    /// <summary>
    /// Identificador do contrato.
    /// </summary>
    public string ContratoId { get; }

    /// <summary>
    /// Descrição do consignado.
    /// </summary>
    public string Descricao { get; }

    /// <summary>
    /// Valor original da parcela.
    /// </summary>
    public Dinheiro ValorOriginal { get; }

    /// <summary>
    /// Valor efetivamente descontado (pode ser menor se houver limite de margem).
    /// </summary>
    public Dinheiro ValorDescontado { get; }

    /// <summary>
    /// Indica se o desconto foi parcial (por limite de margem).
    /// </summary>
    public bool DescontoParcial { get; }

    /// <summary>
    /// Indica se o desconto foi bloqueado (sem margem disponível).
    /// </summary>
    public bool DescontoBloqueado { get; }

    /// <summary>
    /// Parcela descontada (número).
    /// </summary>
    public int NumeroParcela { get; }

    /// <summary>
    /// Total de parcelas do contrato.
    /// </summary>
    public int TotalParcelas { get; }

    /// <summary>
    /// Prioridade do consignado.
    /// </summary>
    public int Prioridade { get; }

    public DetalheDescontoConsignado(
        string contratoId,
        string descricao,
        Dinheiro valorOriginal,
        Dinheiro valorDescontado,
        bool descontoParcial,
        bool descontoBloqueado,
        int numeroParcela,
        int totalParcelas,
        int prioridade)
    {
        ContratoId = contratoId;
        Descricao = descricao;
        ValorOriginal = valorOriginal;
        ValorDescontado = valorDescontado;
        DescontoParcial = descontoParcial;
        DescontoBloqueado = descontoBloqueado;
        NumeroParcela = numeroParcela;
        TotalParcelas = totalParcelas;
        Prioridade = prioridade;
    }

    public override string ToString()
    {
        var status = DescontoBloqueado ? "BLOQUEADO" : DescontoParcial ? "PARCIAL" : "OK";
        return $"{Descricao}: {ValorDescontado} de {ValorOriginal} [{status}]";
    }
}

/// <summary>
/// Resultado do cálculo de consignados para uma folha.
/// Contém a memória completa do cálculo.
/// </summary>
public sealed class ResultadoCalculoConsignados
{
    /// <summary>
    /// Salário líquido disponível antes dos consignados.
    /// </summary>
    public Dinheiro SalarioDisponivelAntes { get; }

    /// <summary>
    /// Margem consignável calculada (percentual do líquido).
    /// </summary>
    public Dinheiro MargemConsignavel { get; }

    /// <summary>
    /// Percentual da margem consignável aplicado.
    /// </summary>
    public decimal PercentualMargem { get; }

    /// <summary>
    /// Margem utilizada pelos consignados.
    /// </summary>
    public Dinheiro MargemUtilizada { get; }

    /// <summary>
    /// Margem disponível restante.
    /// </summary>
    public Dinheiro MargemDisponivel { get; }

    /// <summary>
    /// Total descontado em consignados.
    /// </summary>
    public Dinheiro TotalDescontado { get; }

    /// <summary>
    /// Quantidade de contratos processados.
    /// </summary>
    public int ContratosProcessados { get; }

    /// <summary>
    /// Quantidade de contratos com desconto total aplicado.
    /// </summary>
    public int ContratosDescontadosIntegral { get; }

    /// <summary>
    /// Quantidade de contratos com desconto parcial.
    /// </summary>
    public int ContratosDescontadosParcial { get; }

    /// <summary>
    /// Quantidade de contratos bloqueados (sem margem).
    /// </summary>
    public int ContratosBloqueados { get; }

    /// <summary>
    /// Detalhes de cada consignado processado.
    /// </summary>
    public IReadOnlyList<DetalheDescontoConsignado> Detalhes { get; }

    public ResultadoCalculoConsignados(
        Dinheiro salarioDisponivelAntes,
        Dinheiro margemConsignavel,
        decimal percentualMargem,
        Dinheiro margemUtilizada,
        Dinheiro margemDisponivel,
        Dinheiro totalDescontado,
        IReadOnlyList<DetalheDescontoConsignado> detalhes)
    {
        SalarioDisponivelAntes = salarioDisponivelAntes;
        MargemConsignavel = margemConsignavel;
        PercentualMargem = percentualMargem;
        MargemUtilizada = margemUtilizada;
        MargemDisponivel = margemDisponivel;
        TotalDescontado = totalDescontado;
        Detalhes = detalhes;

        ContratosProcessados = detalhes.Count;
        ContratosDescontadosIntegral = detalhes.Count(d => !d.DescontoParcial && !d.DescontoBloqueado);
        ContratosDescontadosParcial = detalhes.Count(d => d.DescontoParcial);
        ContratosBloqueados = detalhes.Count(d => d.DescontoBloqueado);
    }

    /// <summary>
    /// Cria um resultado vazio (sem consignados).
    /// </summary>
    public static ResultadoCalculoConsignados Vazio(Dinheiro salarioDisponivel, decimal percentualMargem = 35m)
    {
        var margemConsignavel = salarioDisponivel.MultiplicarPorPercentual(percentualMargem);
        return new ResultadoCalculoConsignados(
            salarioDisponivel,
            margemConsignavel,
            percentualMargem,
            Dinheiro.Zero,
            margemConsignavel,
            Dinheiro.Zero,
            Array.Empty<DetalheDescontoConsignado>());
    }

    public override string ToString() =>
        $"Consignados: {TotalDescontado} ({ContratosProcessados} contratos, Margem: {MargemUtilizada}/{MargemConsignavel})";
}
