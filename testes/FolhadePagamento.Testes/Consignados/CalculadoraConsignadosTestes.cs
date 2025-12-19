using FluentAssertions;
using FolhadePagamento.Dominio.Consignados;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Consignados;

/// <summary>
/// Testes para o Serviço de Domínio CalculadoraConsignados.
/// </summary>
public class CalculadoraConsignadosTestes
{
    private static Vigencia CriarVigenciaPadrao() =>
        Vigencia.Criar(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

    private static Competencia CompetenciaPadrao => Competencia.DeAnoMes(2025, 6);

    #region Criação

    [Fact]
    public void Criar_ComMargemPadrao_DeveUsar35Porcento()
    {
        // Act
        var calculadora = CalculadoraConsignados.CriarComMargemPadrao();

        // Assert
        calculadora.PercentualMargem.Should().Be(35m);
    }

    [Fact]
    public void Criar_ComMargemCustomizada_DeveUsarValorInformado()
    {
        // Act
        var calculadora = new CalculadoraConsignados(40m);

        // Assert
        calculadora.PercentualMargem.Should().Be(40m);
    }

    [Fact]
    public void Criar_ComMargemNegativa_DeveLancarExcecao()
    {
        // Act
        var acao = () => new CalculadoraConsignados(-1m);

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("percentualMargem");
    }

    [Fact]
    public void Criar_ComMargemMaiorQue100_DeveLancarExcecao()
    {
        // Act
        var acao = () => new CalculadoraConsignados(101m);

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("percentualMargem");
    }

    #endregion

    #region Margem Consignável

    [Fact]
    public void CalcularMargem_DeveCalcular35Porcento()
    {
        // Arrange
        var calculadora = CalculadoraConsignados.CriarComMargemPadrao();
        var salario = Dinheiro.DeDecimal(5000m);

        // Act
        var margem = calculadora.CalcularMargem(salario);

        // Assert
        // 5000 × 35% = 1750
        margem.Valor.Should().Be(1750m);
    }

    [Theory]
    [InlineData(1000, 350)]     // 1000 × 35% = 350
    [InlineData(3000, 1050)]    // 3000 × 35% = 1050
    [InlineData(5000, 1750)]    // 5000 × 35% = 1750
    [InlineData(10000, 3500)]   // 10000 × 35% = 3500
    public void CalcularMargem_DiversosSalarios_DeveCalcularCorretamente(decimal salario, decimal margemEsperada)
    {
        // Arrange
        var calculadora = CalculadoraConsignados.CriarComMargemPadrao();

        // Act
        var margem = calculadora.CalcularMargem(Dinheiro.DeDecimal(salario));

        // Assert
        margem.Valor.Should().Be(margemEsperada);
    }

    #endregion

    #region Cálculo Sem Consignados

    [Fact]
    public void Calcular_SemContratos_DeveRetornarResultadoVazio()
    {
        // Arrange
        var calculadora = CalculadoraConsignados.CriarComMargemPadrao();
        var salario = Dinheiro.DeDecimal(5000m);

        // Act
        var resultado = calculadora.Calcular(salario, Array.Empty<ContratoConsignado>(), CompetenciaPadrao);

        // Assert
        resultado.TotalDescontado.Valor.Should().Be(0m);
        resultado.ContratosProcessados.Should().Be(0);
        resultado.MargemConsignavel.Valor.Should().Be(1750m);
        resultado.MargemDisponivel.Valor.Should().Be(1750m);
        resultado.MargemUtilizada.Valor.Should().Be(0m);
    }

    #endregion

    #region Desconto Integral

    [Fact]
    public void Calcular_ContratoUnicoDentroDaMargem_DeveDescontarIntegral()
    {
        // Arrange
        var calculadora = CalculadoraConsignados.CriarComMargemPadrao();
        var salario = Dinheiro.DeDecimal(5000m); // Margem = 1750

        var contratos = new[]
        {
            ContratoConsignado.Criar(
                identificador: "CONS-001",
                descricao: "Empréstimo BB",
                valorParcela: Dinheiro.DeDecimal(500m),
                totalParcelas: 24,
                parcelaAtual: 1,
                vigencia: CriarVigenciaPadrao())
        };

        // Act
        var resultado = calculadora.Calcular(salario, contratos, CompetenciaPadrao);

        // Assert
        resultado.TotalDescontado.Valor.Should().Be(500m);
        resultado.ContratosDescontadosIntegral.Should().Be(1);
        resultado.ContratosDescontadosParcial.Should().Be(0);
        resultado.ContratosBloqueados.Should().Be(0);
        resultado.MargemUtilizada.Valor.Should().Be(500m);
        resultado.MargemDisponivel.Valor.Should().Be(1250m);
    }

    [Fact]
    public void Calcular_MultiplosContratosDentroDaMargem_DeveDescontarTodosIntegral()
    {
        // Arrange
        var calculadora = CalculadoraConsignados.CriarComMargemPadrao();
        var salario = Dinheiro.DeDecimal(5000m); // Margem = 1750

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Empréstimo BB", Dinheiro.DeDecimal(500m), 24, 1, CriarVigenciaPadrao(), 1),
            ContratoConsignado.Criar("CONS-002", "Plano Saúde", Dinheiro.DeDecimal(300m), 12, 5, CriarVigenciaPadrao(), 2),
            ContratoConsignado.Criar("CONS-003", "Previdência", Dinheiro.DeDecimal(200m), 36, 10, CriarVigenciaPadrao(), 3)
        };

        // Act
        var resultado = calculadora.Calcular(salario, contratos, CompetenciaPadrao);

        // Assert
        // 500 + 300 + 200 = 1000 (< 1750)
        resultado.TotalDescontado.Valor.Should().Be(1000m);
        resultado.ContratosDescontadosIntegral.Should().Be(3);
        resultado.ContratosDescontadosParcial.Should().Be(0);
        resultado.ContratosBloqueados.Should().Be(0);
    }

    #endregion

    #region Desconto Parcial

    [Fact]
    public void Calcular_ContratoExcedeMargemParcial_DeveDescontarParcialmente()
    {
        // Arrange
        var calculadora = CalculadoraConsignados.CriarComMargemPadrao();
        var salario = Dinheiro.DeDecimal(1000m); // Margem = 350

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Empréstimo Grande", Dinheiro.DeDecimal(500m), 24, 1, CriarVigenciaPadrao())
        };

        // Act
        var resultado = calculadora.Calcular(salario, contratos, CompetenciaPadrao);

        // Assert
        resultado.TotalDescontado.Valor.Should().Be(350m); // Apenas a margem disponível
        resultado.ContratosDescontadosIntegral.Should().Be(0);
        resultado.ContratosDescontadosParcial.Should().Be(1);
        resultado.Detalhes[0].DescontoParcial.Should().BeTrue();
        resultado.Detalhes[0].ValorOriginal.Valor.Should().Be(500m);
        resultado.Detalhes[0].ValorDescontado.Valor.Should().Be(350m);
    }

    [Fact]
    public void Calcular_MultiplosPrimeiroTotalSegundoParcial_DeveRespeitarOrdem()
    {
        // Arrange
        var calculadora = CalculadoraConsignados.CriarComMargemPadrao();
        var salario = Dinheiro.DeDecimal(2000m); // Margem = 700

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Prioridade 1", Dinheiro.DeDecimal(500m), 24, 1, CriarVigenciaPadrao(), 1),
            ContratoConsignado.Criar("CONS-002", "Prioridade 2", Dinheiro.DeDecimal(400m), 12, 1, CriarVigenciaPadrao(), 2)
        };

        // Act
        var resultado = calculadora.Calcular(salario, contratos, CompetenciaPadrao);

        // Assert
        // Margem = 700
        // CONS-001: 500 integral, sobra 200
        // CONS-002: 200 parcial (de 400)
        resultado.TotalDescontado.Valor.Should().Be(700m);
        resultado.ContratosDescontadosIntegral.Should().Be(1);
        resultado.ContratosDescontadosParcial.Should().Be(1);

        resultado.Detalhes[0].ContratoId.Should().Be("CONS-001");
        resultado.Detalhes[0].ValorDescontado.Valor.Should().Be(500m);
        resultado.Detalhes[0].DescontoParcial.Should().BeFalse();

        resultado.Detalhes[1].ContratoId.Should().Be("CONS-002");
        resultado.Detalhes[1].ValorDescontado.Valor.Should().Be(200m);
        resultado.Detalhes[1].DescontoParcial.Should().BeTrue();
    }

    #endregion

    #region Bloqueio

    [Fact]
    public void Calcular_SemMargemDisponivel_DeveBloquearContrato()
    {
        // Arrange
        var calculadora = CalculadoraConsignados.CriarComMargemPadrao();
        var salario = Dinheiro.DeDecimal(1000m); // Margem = 350

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Prioridade 1", Dinheiro.DeDecimal(350m), 24, 1, CriarVigenciaPadrao(), 1),
            ContratoConsignado.Criar("CONS-002", "Prioridade 2", Dinheiro.DeDecimal(100m), 12, 1, CriarVigenciaPadrao(), 2)
        };

        // Act
        var resultado = calculadora.Calcular(salario, contratos, CompetenciaPadrao);

        // Assert
        // CONS-001: 350 integral, margem esgotada
        // CONS-002: bloqueado
        resultado.TotalDescontado.Valor.Should().Be(350m);
        resultado.ContratosDescontadosIntegral.Should().Be(1);
        resultado.ContratosBloqueados.Should().Be(1);

        resultado.Detalhes[1].ContratoId.Should().Be("CONS-002");
        resultado.Detalhes[1].DescontoBloqueado.Should().BeTrue();
        resultado.Detalhes[1].ValorDescontado.Valor.Should().Be(0m);
    }

    #endregion

    #region Ordem de Prioridade

    [Fact]
    public void Calcular_DevePriorizarPorPrioridade()
    {
        // Arrange
        var calculadora = CalculadoraConsignados.CriarComMargemPadrao();
        var salario = Dinheiro.DeDecimal(3000m); // Margem = 1050

        // Criando em ordem inversa de prioridade
        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-003", "Prioridade 3", Dinheiro.DeDecimal(300m), 24, 1, CriarVigenciaPadrao(), 3),
            ContratoConsignado.Criar("CONS-001", "Prioridade 1", Dinheiro.DeDecimal(500m), 24, 1, CriarVigenciaPadrao(), 1),
            ContratoConsignado.Criar("CONS-002", "Prioridade 2", Dinheiro.DeDecimal(400m), 12, 1, CriarVigenciaPadrao(), 2)
        };

        // Act
        var resultado = calculadora.Calcular(salario, contratos, CompetenciaPadrao);

        // Assert - Deve processar na ordem 1, 2, 3
        resultado.Detalhes[0].ContratoId.Should().Be("CONS-001");
        resultado.Detalhes[1].ContratoId.Should().Be("CONS-002");
        resultado.Detalhes[2].ContratoId.Should().Be("CONS-003");
    }

    [Fact]
    public void Calcular_MesmaPrioridade_DeveDesempatarPorIdentificador()
    {
        // Arrange
        var calculadora = CalculadoraConsignados.CriarComMargemPadrao();
        var salario = Dinheiro.DeDecimal(3000m);

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-C", "Contrato C", Dinheiro.DeDecimal(100m), 12, 1, CriarVigenciaPadrao(), 1),
            ContratoConsignado.Criar("CONS-A", "Contrato A", Dinheiro.DeDecimal(100m), 12, 1, CriarVigenciaPadrao(), 1),
            ContratoConsignado.Criar("CONS-B", "Contrato B", Dinheiro.DeDecimal(100m), 12, 1, CriarVigenciaPadrao(), 1)
        };

        // Act
        var resultado = calculadora.Calcular(salario, contratos, CompetenciaPadrao);

        // Assert - Desempate por identificador (alfabético)
        resultado.Detalhes[0].ContratoId.Should().Be("CONS-A");
        resultado.Detalhes[1].ContratoId.Should().Be("CONS-B");
        resultado.Detalhes[2].ContratoId.Should().Be("CONS-C");
    }

    #endregion

    #region Vigência

    [Fact]
    public void Calcular_ContratoForaDaVigencia_DeveIgnorar()
    {
        // Arrange
        var calculadora = CalculadoraConsignados.CriarComMargemPadrao();
        var salario = Dinheiro.DeDecimal(5000m);

        var vigenciaFutura = Vigencia.Criar(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Vigente", Dinheiro.DeDecimal(500m), 24, 1, CriarVigenciaPadrao()),
            ContratoConsignado.Criar("CONS-002", "Futuro", Dinheiro.DeDecimal(300m), 12, 1, vigenciaFutura)
        };

        // Act
        var resultado = calculadora.Calcular(salario, contratos, CompetenciaPadrao);

        // Assert - Apenas CONS-001 deve ser processado
        resultado.TotalDescontado.Valor.Should().Be(500m);
        resultado.ContratosProcessados.Should().Be(1);
        resultado.Detalhes[0].ContratoId.Should().Be("CONS-001");
    }

    #endregion

    #region Salário Zero/Negativo

    [Fact]
    public void Calcular_SalarioZero_DeveRetornarResultadoVazio()
    {
        // Arrange
        var calculadora = CalculadoraConsignados.CriarComMargemPadrao();

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Teste", Dinheiro.DeDecimal(500m), 24, 1, CriarVigenciaPadrao())
        };

        // Act
        var resultado = calculadora.Calcular(Dinheiro.Zero, contratos, CompetenciaPadrao);

        // Assert
        resultado.TotalDescontado.Valor.Should().Be(0m);
        resultado.MargemConsignavel.Valor.Should().Be(0m);
        resultado.ContratosProcessados.Should().Be(0);
    }

    #endregion

    #region Determinismo

    [Fact]
    public void Calcular_MesmasEntradas_DeveRetornarMesmoResultado()
    {
        // Arrange
        var calculadora = CalculadoraConsignados.CriarComMargemPadrao();
        var salario = Dinheiro.DeDecimal(5000m);

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Empréstimo", Dinheiro.DeDecimal(500m), 24, 1, CriarVigenciaPadrao(), 1),
            ContratoConsignado.Criar("CONS-002", "Plano", Dinheiro.DeDecimal(300m), 12, 5, CriarVigenciaPadrao(), 2)
        };

        // Act
        var resultado1 = calculadora.Calcular(salario, contratos, CompetenciaPadrao);
        var resultado2 = calculadora.Calcular(salario, contratos, CompetenciaPadrao);
        var resultado3 = calculadora.Calcular(salario, contratos, CompetenciaPadrao);

        // Assert - DETERMINISMO
        resultado1.TotalDescontado.Should().Be(resultado2.TotalDescontado);
        resultado2.TotalDescontado.Should().Be(resultado3.TotalDescontado);
        resultado1.ContratosDescontadosIntegral.Should().Be(resultado2.ContratosDescontadosIntegral);
    }

    #endregion

    #region Memória de Cálculo

    [Fact]
    public void Calcular_DeveRetornarMemoriaCompleta()
    {
        // Arrange
        var calculadora = CalculadoraConsignados.CriarComMargemPadrao();
        var salario = Dinheiro.DeDecimal(5000m); // Margem = 1750

        var contratos = new[]
        {
            ContratoConsignado.Criar("CONS-001", "Empréstimo BB", Dinheiro.DeDecimal(500m), 24, 3, CriarVigenciaPadrao(), 1)
        };

        // Act
        var resultado = calculadora.Calcular(salario, contratos, CompetenciaPadrao);

        // Assert
        resultado.SalarioDisponivelAntes.Valor.Should().Be(5000m);
        resultado.MargemConsignavel.Valor.Should().Be(1750m);
        resultado.PercentualMargem.Should().Be(35m);
        resultado.MargemUtilizada.Valor.Should().Be(500m);
        resultado.MargemDisponivel.Valor.Should().Be(1250m);
        resultado.TotalDescontado.Valor.Should().Be(500m);
        resultado.ContratosProcessados.Should().Be(1);

        var detalhe = resultado.Detalhes[0];
        detalhe.ContratoId.Should().Be("CONS-001");
        detalhe.Descricao.Should().Be("Empréstimo BB");
        detalhe.ValorOriginal.Valor.Should().Be(500m);
        detalhe.ValorDescontado.Valor.Should().Be(500m);
        detalhe.DescontoParcial.Should().BeFalse();
        detalhe.DescontoBloqueado.Should().BeFalse();
        detalhe.NumeroParcela.Should().Be(3);
        detalhe.TotalParcelas.Should().Be(24);
        detalhe.Prioridade.Should().Be(1);
    }

    #endregion
}
