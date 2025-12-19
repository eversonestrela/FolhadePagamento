using FolhadePagamento.Dominio.Inss;
using FolhadePagamento.Dominio.Irrf;
using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Folha;

/// <summary>
/// Representa o resultado de um cálculo de folha para um funcionário.
/// Imutável uma vez criado - resultados não podem ser modificados.
/// Este é o output do serviço de domínio CalculadoraFolha.
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
    /// Total de Descontos - soma de todos os descontos (INSS, IRRF, etc.).
    /// </summary>
    public Dinheiro TotalDescontos { get; }

    /// <summary>
    /// Salário Líquido - SalarioBruto menos TotalDescontos.
    /// </summary>
    public Dinheiro SalarioLiquido { get; }

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
        Dinheiro totalDescontos,
        Dinheiro salarioLiquido,
        DateTime calculadoEm)
    {
        FuncionarioId = funcionarioId;
        Competencia = competencia;
        SalarioBruto = salarioBruto;
        ValorInss = valorInss;
        DetalheInss = detalheInss;
        ValorIrrf = valorIrrf;
        DetalheIrrf = detalheIrrf;
        TotalDescontos = totalDescontos;
        SalarioLiquido = salarioLiquido;
        CalculadoEm = calculadoEm;
    }

    /// <summary>
    /// Método fábrica para criar um ResultadoCalculo SEM INSS/IRRF (retrocompatibilidade).
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
            outrosDescontos,
            calculadoEm);
    }

    /// <summary>
    /// Método fábrica completo para criar um ResultadoCalculo COM INSS e IRRF.
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

        if (outrosDescontos is null)
            throw new ArgumentNullException(nameof(outrosDescontos));

        // Total de descontos = INSS + IRRF + outros descontos
        var totalDescontos = valorInss.Somar(valorIrrf).Somar(outrosDescontos);

        // Cálculo determinístico: SalarioLiquido = SalarioBruto - TotalDescontos
        var salarioLiquido = salarioBruto.Subtrair(totalDescontos);

        return new ResultadoCalculo(
            funcionarioId,
            competencia,
            salarioBruto,
            valorInss,
            detalheInss,
            valorIrrf,
            detalheIrrf,
            totalDescontos,
            salarioLiquido,
            calculadoEm);
    }

    public override string ToString() =>
        $"Cálculo para {FuncionarioId} em {Competencia}: Bruto={SalarioBruto}, INSS={ValorInss}, IRRF={ValorIrrf}, Descontos={TotalDescontos}, Líquido={SalarioLiquido}";
}
