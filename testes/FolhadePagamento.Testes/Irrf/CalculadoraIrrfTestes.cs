using FluentAssertions;
using FolhadePagamento.Dominio.Irrf;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Irrf;

/// <summary>
/// Testes para o Serviço de Domínio CalculadoraIrrf.
/// </summary>
public class CalculadoraIrrfTestes
{
    #region Criação

    [Fact]
    public void Criar_SemTabelas_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => new CalculadoraIrrf(Array.Empty<TabelaIrrf>());

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*pelo menos uma tabela*");
    }

    [Fact]
    public void CriarComTabelasPadrao_DeveCriarComTabelas2024E2025()
    {
        // Act
        var calculadora = CalculadoraIrrf.CriarComTabelasPadrao();

        // Assert
        calculadora.ObterTodasTabelas().Should().HaveCount(2);
    }

    #endregion

    #region Seleção de Tabela por Vigência

    [Fact]
    public void ObterTabelaVigente_Competencia2024_DeveRetornarTabela2024()
    {
        // Arrange
        var calculadora = CalculadoraIrrf.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2024, 6);

        // Act
        var tabela = calculadora.ObterTabelaVigente(competencia);

        // Assert
        tabela.Identificador.Should().Be("IRRF-2024");
    }

    [Fact]
    public void ObterTabelaVigente_Competencia2025_DeveRetornarTabela2025()
    {
        // Arrange
        var calculadora = CalculadoraIrrf.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act
        var tabela = calculadora.ObterTabelaVigente(competencia);

        // Assert
        tabela.Identificador.Should().Be("IRRF-2025");
    }

    [Fact]
    public void ObterTabelaVigente_CompetenciaSemTabela_DeveLancarExcecao()
    {
        // Arrange
        var calculadora = CalculadoraIrrf.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2020, 1);

        // Act
        var acao = () => calculadora.ObterTabelaVigente(competencia);

        // Assert
        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*Não há tabela de IRRF vigente*");
    }

    [Fact]
    public void ExisteTabelaVigente_ComTabelaExistente_DeveRetornarTrue()
    {
        // Arrange
        var calculadora = CalculadoraIrrf.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act & Assert
        calculadora.ExisteTabelaVigente(competencia).Should().BeTrue();
    }

    [Fact]
    public void ExisteTabelaVigente_SemTabelaExistente_DeveRetornarFalse()
    {
        // Arrange
        var calculadora = CalculadoraIrrf.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2020, 1);

        // Act & Assert
        calculadora.ExisteTabelaVigente(competencia).Should().BeFalse();
    }

    #endregion

    #region Cálculo com Vigência

    [Fact]
    public void Calcular_Competencia2024_DeveUsarTabela2024()
    {
        // Arrange
        var calculadora = CalculadoraIrrf.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2024, 6);
        var baseCalculo = Dinheiro.DeDecimal(5000m);

        // Act
        var resultado = calculadora.Calcular(baseCalculo, competencia);

        // Assert
        resultado.TabelaUtilizada.Should().Be("IRRF-2024");
    }

    [Fact]
    public void Calcular_Competencia2025_DeveUsarTabela2025()
    {
        // Arrange
        var calculadora = CalculadoraIrrf.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2025, 3);
        var baseCalculo = Dinheiro.DeDecimal(5000m);

        // Act
        var resultado = calculadora.Calcular(baseCalculo, competencia);

        // Assert
        resultado.TabelaUtilizada.Should().Be("IRRF-2025");
    }

    [Fact]
    public void Calcular_BaseIsenta_DeveRetornarZero()
    {
        // Arrange
        var calculadora = CalculadoraIrrf.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2025, 3);
        var baseCalculo = Dinheiro.DeDecimal(2000m);

        // Act
        var resultado = calculadora.Calcular(baseCalculo, competencia);

        // Assert
        resultado.EhIsento.Should().BeTrue();
        resultado.ValorIrrf.Valor.Should().Be(0);
    }

    [Fact]
    public void Calcular_ComDependentes_DeveReduzirBase()
    {
        // Arrange
        var calculadora = CalculadoraIrrf.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2025, 3);
        var baseCalculo = Dinheiro.DeDecimal(2500m);
        int dependentes = 2;

        // Act
        var resultado = calculadora.Calcular(baseCalculo, competencia, dependentes);

        // Assert
        resultado.NumeroDependentes.Should().Be(2);
        resultado.DeducaoPorDependentes.Valor.Should().BeGreaterThan(0);
    }

    #endregion

    #region Determinismo

    [Fact]
    public void Calcular_MesmasEntradas_DeveRetornarMesmoResultado()
    {
        // Arrange
        var calculadora = CalculadoraIrrf.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var baseCalculo = Dinheiro.DeDecimal(5000m);

        // Act
        var resultado1 = calculadora.Calcular(baseCalculo, competencia);
        var resultado2 = calculadora.Calcular(baseCalculo, competencia);
        var resultado3 = calculadora.Calcular(baseCalculo, competencia);

        // Assert - Determinismo: sempre o mesmo resultado
        resultado1.ValorIrrf.Should().Be(resultado2.ValorIrrf);
        resultado2.ValorIrrf.Should().Be(resultado3.ValorIrrf);
    }

    #endregion

    #region Validações

    [Fact]
    public void Calcular_BaseNula_DeveLancarExcecao()
    {
        // Arrange
        var calculadora = CalculadoraIrrf.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2025, 3);

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
        var calculadora = CalculadoraIrrf.CriarComTabelasPadrao();
        var baseCalculo = Dinheiro.DeDecimal(5000m);

        // Act
        var acao = () => calculadora.Calcular(baseCalculo, null!);

        // Assert
        acao.Should().Throw<ArgumentNullException>()
            .WithParameterName("competencia");
    }

    [Fact]
    public void Calcular_DependentesNegativo_DeveLancarExcecao()
    {
        // Arrange
        var calculadora = CalculadoraIrrf.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2025, 3);
        var baseCalculo = Dinheiro.DeDecimal(5000m);

        // Act
        var acao = () => calculadora.Calcular(baseCalculo, competencia, -1);

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("numeroDependentes");
    }

    #endregion
}
