using FluentAssertions;
using FolhadePagamento.Dominio.Fgts;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Fgts;

/// <summary>
/// Testes para o Serviço de Domínio CalculadoraFgts.
/// </summary>
public class CalculadoraFgtsTestes
{
    #region Criação

    [Fact]
    public void Criar_SemTabelas_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => new CalculadoraFgts(Array.Empty<TabelaFgts>());

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*pelo menos uma tabela*");
    }

    [Fact]
    public void CriarComTabelaPadrao_DeveCriarCalculadora()
    {
        // Act
        var calculadora = CalculadoraFgts.CriarComTabelaPadrao();

        // Assert
        calculadora.ObterTodasTabelas().Should().HaveCount(1);
    }

    #endregion

    #region Cálculo

    [Fact]
    public void Calcular_FuncionarioNormal_DeveCalcular8Porcento()
    {
        // Arrange
        var calculadora = CalculadoraFgts.CriarComTabelaPadrao();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var baseCalculo = Dinheiro.DeDecimal(5000m);

        // Act
        var resultado = calculadora.Calcular(baseCalculo, competencia);

        // Assert
        resultado.ValorFgts.Valor.Should().Be(400m);
        resultado.AliquotaAplicada.Should().Be(8m);
    }

    [Fact]
    public void Calcular_Aprendiz_DeveCalcular2Porcento()
    {
        // Arrange
        var calculadora = CalculadoraFgts.CriarComTabelaPadrao();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var baseCalculo = Dinheiro.DeDecimal(1500m);

        // Act
        var resultado = calculadora.Calcular(baseCalculo, competencia, ehAprendiz: true);

        // Assert
        resultado.ValorFgts.Valor.Should().Be(30m);
        resultado.AliquotaAplicada.Should().Be(2m);
        resultado.EhAprendiz.Should().BeTrue();
    }

    [Fact]
    public void Calcular_BaseNula_DeveLancarExcecao()
    {
        // Arrange
        var calculadora = CalculadoraFgts.CriarComTabelaPadrao();
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act
        var acao = () => calculadora.Calcular(null!, competencia);

        // Assert
        acao.Should().Throw<ArgumentNullException>()
            .WithParameterName("baseCalculo");
    }

    [Fact]
    public void Calcular_CompetenciaNula_DeveLancarExcecao()
    {
        // Arrange
        var calculadora = CalculadoraFgts.CriarComTabelaPadrao();
        var baseCalculo = Dinheiro.DeDecimal(5000m);

        // Act
        var acao = () => calculadora.Calcular(baseCalculo, null!);

        // Assert
        acao.Should().Throw<ArgumentNullException>()
            .WithParameterName("competencia");
    }

    #endregion

    #region Seleção de Tabela por Vigência

    [Fact]
    public void ObterTabelaVigente_CompetenciaValida_DeveRetornarTabela()
    {
        // Arrange
        var calculadora = CalculadoraFgts.CriarComTabelaPadrao();
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act
        var tabela = calculadora.ObterTabelaVigente(competencia);

        // Assert
        tabela.Identificador.Should().Be("FGTS-PADRAO");
    }

    [Fact]
    public void ExisteTabelaVigente_CompetenciaValida_DeveRetornarTrue()
    {
        // Arrange
        var calculadora = CalculadoraFgts.CriarComTabelaPadrao();
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act & Assert
        calculadora.ExisteTabelaVigente(competencia).Should().BeTrue();
    }

    [Fact]
    public void ExisteTabelaVigente_CompetenciaAntesDaVigencia_DeveRetornarFalse()
    {
        // Arrange
        var calculadora = CalculadoraFgts.CriarComTabelaPadrao();
        var competencia = Competencia.DeAnoMes(1989, 12); // Antes de 1990

        // Act & Assert
        calculadora.ExisteTabelaVigente(competencia).Should().BeFalse();
    }

    [Fact]
    public void ObterTabelaVigente_CompetenciaSemTabela_DeveLancarExcecao()
    {
        // Arrange
        var calculadora = CalculadoraFgts.CriarComTabelaPadrao();
        var competencia = Competencia.DeAnoMes(1989, 1);

        // Act
        var acao = () => calculadora.ObterTabelaVigente(competencia);

        // Assert
        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*Não há tabela de FGTS vigente*");
    }

    #endregion

    #region Determinismo

    [Fact]
    public void Calcular_MesmasEntradas_DeveRetornarMesmoResultado()
    {
        // Arrange
        var calculadora = CalculadoraFgts.CriarComTabelaPadrao();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var baseCalculo = Dinheiro.DeDecimal(7500m);

        // Act
        var resultado1 = calculadora.Calcular(baseCalculo, competencia);
        var resultado2 = calculadora.Calcular(baseCalculo, competencia);
        var resultado3 = calculadora.Calcular(baseCalculo, competencia);

        // Assert - DETERMINISMO
        resultado1.ValorFgts.Should().Be(resultado2.ValorFgts);
        resultado2.ValorFgts.Should().Be(resultado3.ValorFgts);
    }

    #endregion
}
