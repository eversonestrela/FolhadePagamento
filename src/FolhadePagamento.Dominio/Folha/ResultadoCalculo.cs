using FolhadePagamento.Dominio.Consignados;
using FolhadePagamento.Dominio.Fgts;
using FolhadePagamento.Dominio.Inss;
using FolhadePagamento.Dominio.Irrf;
using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Folha;

/// <summary>
/// Representa o resultado de um cálculo de folha para um funcionário.
/// Imutável uma vez criado - resultados não podem ser modificados.
/// Este é o output do serviço de domínio CalculadoraFolha.
/// 
/// ENCARGOS:
/// - Descontos do funcionário: INSS, IRRF, Consignados (impactam salário líquido)
/// - Encargos patronais: FGTS (NÃO impactam salário líquido)
/// </summary>
public sealed class ResultadoCalculo
{
    /// <summary>
    /// Identificador do funcionário para quem o cálculo foi realizado.
    /// </summary>
    public FuncionarioId FuncionarioId { get; }

    /// <summary>
    /// Competência (ano-mês) deste cálculo.
    /// </summary>
    public Competencia Competencia { get; }

    /// <summary>
    /// Salário Bruto - soma de todos os proventos.
    /// Na versão básica, igual ao SalarioBase.
    /// </summary>
    public Dinheiro SalarioBruto { get; }

    /// <summary>
    /// Valor do INSS descontado.
    /// Calculado de forma progressiva conforme tabela vigente.
    /// </summary>
    public Dinheiro ValorInss { get; }

    /// <summary>
    /// Detalhamento do cálculo do INSS (opcional).
    /// Contém informações sobre base de cálculo, tabela usada e detalhes por faixa.
    /// </summary>
    public ResultadoCalculoInss? DetalheInss { get; }

    /// <summary>
    /// Valor do IRRF descontado.
    /// Calculado de forma progressiva conforme tabela vigente.
    /// Base de cálculo = SalarioBruto - ValorInss.
    /// </summary>
    public Dinheiro ValorIrrf { get; }

    /// <summary>
    /// Detalhamento do cálculo do IRRF (opcional).
    /// Contém informações sobre base, dependentes, alíquota e faixa aplicada.
    /// </summary>
    public ResultadoCalculoIrrf? DetalheIrrf { get; }

    /// <summary>
    /// Total de Descontos - soma de todos os descontos (INSS, IRRF, Consignados, etc.).
    /// IMPORTANTE: FGTS NÃO entra aqui pois é encargo patronal.
    /// </summary>
    public Dinheiro TotalDescontos { get; }

    /// <summary>
    /// Salário Líquido - SalarioBruto menos TotalDescontos.
    /// </summary>
    public Dinheiro SalarioLiquido { get; }

    /// <summary>
    /// Valor total dos consignados descontados.
    /// </summary>
    public Dinheiro ValorConsignados { get; }

    /// <summary>
    /// Detalhamento do cálculo dos consignados (opcional).
    /// Contém informações sobre margem, contratos e descontos aplicados.
    /// </summary>
    public ResultadoCalculoConsignados? DetalheConsignados { get; }

    /// <summary>
    /// Valor do FGTS (encargo patronal).
    /// IMPORTANTE: NÃO desconta do funcionário, é pago pelo empregador.
    /// </summary>
    public Dinheiro ValorFgts { get; }

    /// <summary>
    /// Detalhamento do cálculo do FGTS (opcional).
    /// Contém informações sobre base, alíquota e tabela usada.
    /// </summary>
    public ResultadoCalculoFgts? DetalheFgts { get; }

    /// <summary>
    /// Total de encargos patronais (FGTS + futuros).
    /// Custo adicional do empregador sobre a folha.
    /// </summary>
    public Dinheiro TotalEncargosPatronais { get; }

    /// <summary>
    /// Custo total do funcionário para o empregador.
    /// = SalarioBruto + TotalEncargosPatronais
    /// </summary>
    public Dinheiro CustoTotalEmpregador { get; }

    /// <summary>
    /// Timestamp de quando o cálculo foi realizado.
    /// Fornecido externamente para garantir determinismo (sem DateTime.Now).
    /// </summary>
    public DateTime CalculadoEm { get; }

    private ResultadoCalculo(
        FuncionarioId funcionarioId,
        Competencia competencia,
        Dinheiro salarioBruto,
        Dinheiro valorInss,
        ResultadoCalculoInss? detalheInss,
        Dinheiro valorIrrf,
        ResultadoCalculoIrrf? detalheIrrf,
        Dinheiro valorConsignados,
        ResultadoCalculoConsignados? detalheConsignados,
        Dinheiro totalDescontos,
        Dinheiro salarioLiquido,
        Dinheiro valorFgts,
        ResultadoCalculoFgts? detalheFgts,
        Dinheiro totalEncargosPatronais,
        Dinheiro custoTotalEmpregador,
        DateTime calculadoEm)
    {
        FuncionarioId = funcionarioId;
        Competencia = competencia;
        SalarioBruto = salarioBruto;
        ValorInss = valorInss;
        DetalheInss = detalheInss;
        ValorIrrf = valorIrrf;
        DetalheIrrf = detalheIrrf;
        ValorConsignados = valorConsignados;
        DetalheConsignados = detalheConsignados;
        TotalDescontos = totalDescontos;
        SalarioLiquido = salarioLiquido;
        ValorFgts = valorFgts;
        DetalheFgts = detalheFgts;
        TotalEncargosPatronais = totalEncargosPatronais;
        CustoTotalEmpregador = custoTotalEmpregador;
        CalculadoEm = calculadoEm;
    }

    /// <summary>
    /// Método fábrica para criar um ResultadoCalculo SEM INSS/IRRF/FGTS (retrocompatibilidade).
    /// Usado quando não há tabelas disponíveis ou funcionário é isento.
    /// </summary>
    public static ResultadoCalculo Criar(
        FuncionarioId funcionarioId,
        Competencia competencia,
        Dinheiro salarioBruto,
        Dinheiro totalDescontos,
        DateTime calculadoEm)
    {
        return Criar(
            funcionarioId,
            competencia,
            salarioBruto,
            Dinheiro.Zero,
            null,
            Dinheiro.Zero,
            null,
            Dinheiro.Zero,
            null,
            totalDescontos,
            calculadoEm);
    }

    /// <summary>
    /// Método fábrica para criar um ResultadoCalculo COM INSS (retrocompatibilidade v0.3).
    /// Valida invariantes (SalarioLiquido = SalarioBruto - TotalDescontos).
    /// </summary>
    public static ResultadoCalculo Criar(
        FuncionarioId funcionarioId,
        Competencia competencia,
        Dinheiro salarioBruto,
        Dinheiro valorInss,
        ResultadoCalculoInss? detalheInss,
        Dinheiro outrosDescontos,
        DateTime calculadoEm)
    {
        return Criar(
            funcionarioId,
            competencia,
            salarioBruto,
            valorInss,
            detalheInss,
            Dinheiro.Zero,
            null,
            Dinheiro.Zero,
            null,
            outrosDescontos,
            calculadoEm);
    }

    /// <summary>
    /// Método fábrica para criar um ResultadoCalculo COM INSS e IRRF (retrocompatibilidade v0.4).
    /// Valida invariantes (SalarioLiquido = SalarioBruto - TotalDescontos).
    /// </summary>
    public static ResultadoCalculo Criar(
        FuncionarioId funcionarioId,
        Competencia competencia,
        Dinheiro salarioBruto,
        Dinheiro valorInss,
        ResultadoCalculoInss? detalheInss,
        Dinheiro valorIrrf,
        ResultadoCalculoIrrf? detalheIrrf,
        Dinheiro outrosDescontos,
        DateTime calculadoEm)
    {
        return Criar(
            funcionarioId,
            competencia,
            salarioBruto,
            valorInss,
            detalheInss,
            valorIrrf,
            detalheIrrf,
            Dinheiro.Zero,
            null,
            outrosDescontos,
            calculadoEm);
    }

    /// <summary>
    /// Método fábrica completo para criar um ResultadoCalculo COM INSS, IRRF e FGTS.
    /// Valida invariantes:
    /// - SalarioLiquido = SalarioBruto - TotalDescontos
    /// - CustoTotalEmpregador = SalarioBruto + TotalEncargosPatronais
    /// IMPORTANTE: FGTS NÃO impacta salário líquido (é encargo patronal).
    /// </summary>
    public static ResultadoCalculo Criar(
        FuncionarioId funcionarioId,
        Competencia competencia,
        Dinheiro salarioBruto,
        Dinheiro valorInss,
        ResultadoCalculoInss? detalheInss,
        Dinheiro valorIrrf,
        ResultadoCalculoIrrf? detalheIrrf,
        Dinheiro valorFgts,
        ResultadoCalculoFgts? detalheFgts,
        Dinheiro outrosDescontos,
        DateTime calculadoEm)
    {
        // Retrocompatibilidade: chama versão completa sem consignados
        return Criar(
            funcionarioId,
            competencia,
            salarioBruto,
            valorInss,
            detalheInss,
            valorIrrf,
            detalheIrrf,
            Dinheiro.Zero,
            null,
            valorFgts,
            detalheFgts,
            outrosDescontos,
            calculadoEm);
    }

    /// <summary>
    /// Método fábrica completo para criar um ResultadoCalculo COM INSS, IRRF, FGTS e Consignados.
    /// Valida invariantes:
    /// - SalarioLiquido = SalarioBruto - TotalDescontos
    /// - CustoTotalEmpregador = SalarioBruto + TotalEncargosPatronais
    /// IMPORTANTE: FGTS NÃO impacta salário líquido (é encargo patronal).
    /// </summary>
    public static ResultadoCalculo Criar(
        FuncionarioId funcionarioId,
        Competencia competencia,
        Dinheiro salarioBruto,
        Dinheiro valorInss,
        ResultadoCalculoInss? detalheInss,
        Dinheiro valorIrrf,
        ResultadoCalculoIrrf? detalheIrrf,
        Dinheiro valorConsignados,
        ResultadoCalculoConsignados? detalheConsignados,
        Dinheiro valorFgts,
        ResultadoCalculoFgts? detalheFgts,
        Dinheiro outrosDescontos,
        DateTime calculadoEm)
    {
        if (funcionarioId is null)
            throw new ArgumentNullException(nameof(funcionarioId));

        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        if (salarioBruto is null)
            throw new ArgumentNullException(nameof(salarioBruto));

        if (valorInss is null)
            throw new ArgumentNullException(nameof(valorInss));

        if (valorIrrf is null)
            throw new ArgumentNullException(nameof(valorIrrf));

        if (valorConsignados is null)
            throw new ArgumentNullException(nameof(valorConsignados));

        if (valorFgts is null)
            throw new ArgumentNullException(nameof(valorFgts));

        if (outrosDescontos is null)
            throw new ArgumentNullException(nameof(outrosDescontos));

        // Total de descontos = INSS + IRRF + Consignados + outros descontos
        // IMPORTANTE: FGTS NÃO entra nos descontos (é encargo patronal)
        var totalDescontos = valorInss.Somar(valorIrrf).Somar(valorConsignados).Somar(outrosDescontos);

        // Cálculo determinístico: SalarioLiquido = SalarioBruto - TotalDescontos
        var salarioLiquido = salarioBruto.Subtrair(totalDescontos);

        // Total de encargos patronais (atualmente só FGTS)
        var totalEncargosPatronais = valorFgts;

        // Custo total do empregador = Salário Bruto + Encargos Patronais
        var custoTotalEmpregador = salarioBruto.Somar(totalEncargosPatronais);

        return new ResultadoCalculo(
            funcionarioId,
            competencia,
            salarioBruto,
            valorInss,
            detalheInss,
            valorIrrf,
            detalheIrrf,
            valorConsignados,
            detalheConsignados,
            totalDescontos,
            salarioLiquido,
            valorFgts,
            detalheFgts,
            totalEncargosPatronais,
            custoTotalEmpregador,
            calculadoEm);
    }

    public override string ToString() =>
        $"Cálculo para {FuncionarioId} em {Competencia}: Bruto={SalarioBruto}, INSS={ValorInss}, IRRF={ValorIrrf}, Consignados={ValorConsignados}, Descontos={TotalDescontos}, Líquido={SalarioLiquido}, FGTS={ValorFgts}, CustoEmpregador={CustoTotalEmpregador}";
}
