using FluentAssertions;
using FolhadePagamento.Dominio.Consignados;
using FolhadePagamento.Dominio.Entidades;
using FolhadePagamento.Dominio.Fgts;
using FolhadePagamento.Dominio.Folha;
using FolhadePagamento.Dominio.Inss;
using FolhadePagamento.Dominio.Irrf;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Folha;

/// <summary>
/// Testes de integração da CalculadoraFolha com Consignados.
/// 
/// IMPORTANTE: Consignados impactam salário líquido.
/// Margem é calculada sobre líquido (após INSS/IRRF).
/// </summary>
public class CalculadoraFolhaComConsignadosTestes
{
    private readonly DateTime _timestampFixo = new(2025, 6, 15, 10, 0, 0);

    private static Funcionario CriarFuncionario(string nome, decimal salarioBase)
    {
        var id = FuncionarioId.Novo();
        return Funcionario.Criar(id, nome, Dinheiro.DeDecimal(salarioBase));
    }

    private static Vigencia CriarVigenciaPadrao() =>
        Vigencia.Criar(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

    private static CalculadoraFolha CriarCalculadoraCompleta()
    {
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadoraFgts = CalculadoraFgts.CriarComTabelaPadrao();
        var calculadoraConsignados = CalculadoraConsignados.CriarComMargemPadrao();

        return new CalculadoraFolha(calculadoraInss, calculadoraIrrf, calculadoraFgts, calculadoraConsignados);
    }

    #region Retrocompatibilidade

    [Fact]
    public void Calcular_SemCalculadoraConsignados_DeveTerConsignadosZero()
    {
        // Arrange - Sem Consignados
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadoraFgts = CalculadoraFgts.CriarComTabelaPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf, calculadoraFgts);
        var funcionario = CriarFuncionario("Maria da Silva", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.ValorConsignados.Valor.Should().Be(0m);
        resultado.DetalheConsignados.Should().BeNull();
    }

    [Fact]
    public void Calcular_SemContratosPassados_DeveTerConsignadosZero()
    {
        // Arrange
        var calculadora = CriarCalculadoraCompleta();
        var funcionario = CriarFuncionario("João Silva", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act - Usando sobrecarga sem contratos
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.ValorConsignados.Valor.Should().Be(0m);
    }

    #endregion

    #region Cálculo Básico

    [Fact]
    public void Calcular_ComConsignado_DeveDescontarDoLiquido()
    {
        // Arrange
        var calculadora = CriarCalculadoraCompleta();
        var funcionario = CriarFuncionario("João Silva", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Empréstimo BB", Dinheiro.DeDecimal(500m), 24, 1, CriarVigenciaPadrao())
        };

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo, contratos);

        // Assert
        resultado.ValorConsignados.Valor.Should().Be(500m);
        resultado.DetalheConsignados.Should().NotBeNull();
        resultado.DetalheConsignados!.ContratosDescontadosIntegral.Should().Be(1);
    }

    [Fact]
    public void Calcular_ConsignadoDeveImpactarLiquido()
    {
        // Arrange
        var calculadora = CriarCalculadoraCompleta();
        var funcionario = CriarFuncionario("Teste Líquido", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Empréstimo", Dinheiro.DeDecimal(500m), 24, 1, CriarVigenciaPadrao())
        };

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo, contratos);

        // Assert
        // Líquido = Bruto - INSS - IRRF - Consignados
        var liquidoEsperado = resultado.SalarioBruto
            .Subtrair(resultado.ValorInss)
            .Subtrair(resultado.ValorIrrf)
            .Subtrair(resultado.ValorConsignados);

        resultado.SalarioLiquido.Should().Be(liquidoEsperado);
    }

    [Fact]
    public void Calcular_ConsignadoDeveEstarNosDescontos()
    {
        // Arrange
        var calculadora = CriarCalculadoraCompleta();
        var funcionario = CriarFuncionario("Teste Descontos", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Empréstimo", Dinheiro.DeDecimal(500m), 24, 1, CriarVigenciaPadrao())
        };

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo, contratos);

        // Assert
        // TotalDescontos = INSS + IRRF + Consignados
        var descontosEsperados = resultado.ValorInss
            .Somar(resultado.ValorIrrf)
            .Somar(resultado.ValorConsignados);

        resultado.TotalDescontos.Should().Be(descontosEsperados);
    }

    #endregion

    #region Margem Sobre Líquido

    [Fact]
    public void Calcular_MargemDeveSerSobreLiquidoAntesConsignados()
    {
        // Arrange
        var calculadora = CriarCalculadoraCompleta();
        var funcionario = CriarFuncionario("Teste Margem", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Empréstimo", Dinheiro.DeDecimal(500m), 24, 1, CriarVigenciaPadrao())
        };

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo, contratos);

        // Assert
        // Margem é calculada sobre (Bruto - INSS - IRRF) × 35%
        var liquidoAntesConsignados = resultado.SalarioBruto
            .Subtrair(resultado.ValorInss)
            .Subtrair(resultado.ValorIrrf);
        var margemEsperada = liquidoAntesConsignados.MultiplicarPorPercentual(35m);

        resultado.DetalheConsignados!.SalarioDisponivelAntes.Should().Be(liquidoAntesConsignados);
        resultado.DetalheConsignados!.MargemConsignavel.Should().Be(margemEsperada);
    }

    #endregion

    #region Nunca Líquido Negativo

    [Fact]
    public void Calcular_ConsignadoGrandeNaoDeveResultarLiquidoNegativo()
    {
        // Arrange
        var calculadora = CriarCalculadoraCompleta();
        var funcionario = CriarFuncionario("Teste Limite", 3000m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Consignado maior que o líquido
        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Empréstimo Grande", Dinheiro.DeDecimal(5000m), 24, 1, CriarVigenciaPadrao())
        };

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo, contratos);

        // Assert
        resultado.SalarioLiquido.Valor.Should().BeGreaterOrEqualTo(0m);
        resultado.DetalheConsignados!.ContratosDescontadosParcial.Should().Be(1);
    }

    #endregion

    #region Prioridade e Ordem

    [Fact]
    public void Calcular_ConsignadosDevemRespeitarPrioridade()
    {
        // Arrange
        var calculadora = CriarCalculadoraCompleta();
        var funcionario = CriarFuncionario("Teste Prioridade", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Contratos em ordem inversa de prioridade
        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-003", "Terceiro", Dinheiro.DeDecimal(200m), 12, 1, CriarVigenciaPadrao(), 3),
            ContratoConsignado.Criar("CONS-001", "Primeiro", Dinheiro.DeDecimal(400m), 24, 1, CriarVigenciaPadrao(), 1),
            ContratoConsignado.Criar("CONS-002", "Segundo", Dinheiro.DeDecimal(300m), 18, 1, CriarVigenciaPadrao(), 2)
        };

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo, contratos);

        // Assert - Deve processar em ordem de prioridade
        var detalhes = resultado.DetalheConsignados!.Detalhes;
        detalhes[0].ContratoId.Should().Be("CONS-001");
        detalhes[1].ContratoId.Should().Be("CONS-002");
        detalhes[2].ContratoId.Should().Be("CONS-003");
    }

    #endregion

    #region Vigência

    [Fact]
    public void Calcular_ContratoForaDaVigenciaNaoDeveSerConsiderado()
    {
        // Arrange
        var calculadora = CriarCalculadoraCompleta();
        var funcionario = CriarFuncionario("Teste Vigência", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        var vigenciaFutura = Vigencia.Criar(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Vigente", Dinheiro.DeDecimal(500m), 24, 1, CriarVigenciaPadrao()),
            ContratoConsignado.Criar("CONS-002", "Futuro", Dinheiro.DeDecimal(300m), 12, 1, vigenciaFutura)
        };

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo, contratos);

        // Assert - Apenas CONS-001 deve ser descontado
        resultado.ValorConsignados.Valor.Should().Be(500m);
        resultado.DetalheConsignados!.ContratosProcessados.Should().Be(1);
    }

    #endregion

    #region Pipeline Completo

    [Fact]
    public void Calcular_PipelineCompleto_DeveCalcularNaOrdemCorreta()
    {
        // Arrange
        var calculadora = CriarCalculadoraCompleta();
        var funcionario = CriarFuncionario("Pipeline Completo", 8000m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Empréstimo", Dinheiro.DeDecimal(500m), 24, 1, CriarVigenciaPadrao())
        };

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo, contratos);

        // Assert - Pipeline: INSS → IRRF → FGTS → Consignados
        resultado.DetalheInss.Should().NotBeNull();
        resultado.DetalheIrrf.Should().NotBeNull();
        resultado.DetalheFgts.Should().NotBeNull();
        resultado.DetalheConsignados.Should().NotBeNull();

        // FGTS NÃO afeta líquido
        resultado.TotalEncargosPatronais.Should().Be(resultado.ValorFgts);

        // Consignados SIM afetam líquido
        resultado.TotalDescontos.Should().Be(
            resultado.ValorInss.Somar(resultado.ValorIrrf).Somar(resultado.ValorConsignados));

        // Líquido = Bruto - Descontos (sem FGTS)
        resultado.SalarioLiquido.Should().Be(resultado.SalarioBruto.Subtrair(resultado.TotalDescontos));
    }

    #endregion

    #region Determinismo

    [Fact]
    public void Calcular_MesmasEntradas_DeveRetornarMesmoResultado()
    {
        // Arrange
        var calculadora = CriarCalculadoraCompleta();
        var funcionario = CriarFuncionario("Determinismo", 6500m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Empréstimo", Dinheiro.DeDecimal(500m), 24, 1, CriarVigenciaPadrao())
        };

        // Act
        var resultado1 = calculadora.Calcular(funcionario, competencia, _timestampFixo, contratos);
        var resultado2 = calculadora.Calcular(funcionario, competencia, _timestampFixo, contratos);
        var resultado3 = calculadora.Calcular(funcionario, competencia, _timestampFixo, contratos);

        // Assert - DETERMINISMO
        resultado1.ValorConsignados.Should().Be(resultado2.ValorConsignados);
        resultado2.ValorConsignados.Should().Be(resultado3.ValorConsignados);
        resultado1.SalarioLiquido.Should().Be(resultado2.SalarioLiquido);
        resultado2.SalarioLiquido.Should().Be(resultado3.SalarioLiquido);
    }

    #endregion

    #region Estrutura do Resultado

    [Fact]
    public void Calcular_DeveManterEstruturaCorreta()
    {
        // Arrange
        var calculadora = CriarCalculadoraCompleta();
        var funcionario = CriarFuncionario("Estrutura", 7500m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Empréstimo BB", Dinheiro.DeDecimal(600m), 24, 5, CriarVigenciaPadrao())
        };

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo, contratos, 1);

        // Assert
        resultado.FuncionarioId.Should().Be(funcionario.Id);
        resultado.Competencia.Should().Be(competencia);
        resultado.SalarioBruto.Should().Be(funcionario.SalarioBase);
        resultado.ValorInss.Valor.Should().BeGreaterThan(0);
        resultado.ValorIrrf.Valor.Should().BeGreaterThan(0);
        resultado.ValorFgts.Valor.Should().BeGreaterThan(0);
        resultado.ValorConsignados.Valor.Should().Be(600m);

        // Descontos incluem Consignados
        resultado.TotalDescontos.Should().Be(
            resultado.ValorInss.Somar(resultado.ValorIrrf).Somar(resultado.ValorConsignados));

        // Custo empregador NÃO inclui consignados (é desconto do funcionário)
        resultado.CustoTotalEmpregador.Should().Be(
            resultado.SalarioBruto.Somar(resultado.TotalEncargosPatronais));

        resultado.CalculadoEm.Should().Be(_timestampFixo);
    }

    #endregion
}
