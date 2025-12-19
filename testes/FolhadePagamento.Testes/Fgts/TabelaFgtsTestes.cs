using FluentAssertions;
using FolhadePagamento.Dominio.Fgts;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Fgts;

/// <summary>
/// Testes para o Value Object TabelaFgts.
/// </summary>
public class TabelaFgtsTestes
{
    #region Criação

    [Fact]
    public void Criar_ComValoresValidos_DeveCriarTabela()
    {
        // Arrange
        var vigencia = Vigencia.Indefinida(new DateTime(1990, 1, 1));

        // Act
        var tabela = TabelaFgts.Criar(
            "FGTS-TEST",
            "Tabela Teste",
            vigencia,
            aliquotaPadrao: 8m,
            aliquotaAprendiz: 2m);

        // Assert
        tabela.Identificador.Should().Be("FGTS-TEST");
        tabela.Descricao.Should().Be("Tabela Teste");
        tabela.AliquotaPadrao.Should().Be(8m);
        tabela.AliquotaAprendiz.Should().Be(2m);
    }

    [Fact]
    public void Criar_SemIdentificador_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => TabelaFgts.Criar(
            "",
            "Descrição",
            Vigencia.Indefinida(new DateTime(1990, 1, 1)),
            8m,
            2m);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*Identificador é obrigatório*");
    }

    [Fact]
    public void Criar_ComAliquotaNegativa_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => TabelaFgts.Criar(
            "FGTS-TEST",
            "Descrição",
            Vigencia.Indefinida(new DateTime(1990, 1, 1)),
            -1m,
            2m);

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("aliquotaPadrao");
    }

    [Fact]
    public void Criar_ComAliquotaMaiorQue100_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => TabelaFgts.Criar(
            "FGTS-TEST",
            "Descrição",
            Vigencia.Indefinida(new DateTime(1990, 1, 1)),
            8m,
            101m);

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("aliquotaAprendiz");
    }

    #endregion

    #region Tabela Padrão

    [Fact]
    public void CriarTabelaPadrao_DeveRetornarAliquota8Porcento()
    {
        // Act
        var tabela = TabelaFgts.CriarTabelaPadrao();

        // Assert
        tabela.Identificador.Should().Be("FGTS-PADRAO");
        tabela.AliquotaPadrao.Should().Be(8m);
        tabela.AliquotaAprendiz.Should().Be(2m);
    }

    [Fact]
    public void CriarTabelaPadrao_DeveEstarVigenteDesde1990()
    {
        // Arrange
        var tabela = TabelaFgts.CriarTabelaPadrao();
        var competencia1990 = Competencia.DeAnoMes(1990, 1);
        var competencia2025 = Competencia.DeAnoMes(2025, 12);

        // Act & Assert
        tabela.EstaVigenteParaCompetencia(competencia1990).Should().BeTrue();
        tabela.EstaVigenteParaCompetencia(competencia2025).Should().BeTrue();
    }

    #endregion

    #region Vigência

    [Fact]
    public void EstaVigenteParaCompetencia_CompetenciaVigente_DeveRetornarTrue()
    {
        // Arrange
        var tabela = TabelaFgts.CriarTabelaPadrao();
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act & Assert
        tabela.EstaVigenteParaCompetencia(competencia).Should().BeTrue();
    }

    [Fact]
    public void EstaVigenteParaCompetencia_CompetenciaNula_DeveLancarExcecao()
    {
        // Arrange
        var tabela = TabelaFgts.CriarTabelaPadrao();

        // Act
        var acao = () => tabela.EstaVigenteParaCompetencia(null!);

        // Assert
        acao.Should().Throw<ArgumentNullException>()
            .WithParameterName("competencia");
    }

    #endregion

    #region Cálculo

    [Fact]
    public void Calcular_AliquotaPadrao_DeveCalcular8Porcento()
    {
        // Arrange
        var tabela = TabelaFgts.CriarTabelaPadrao();
        var baseCalculo = Dinheiro.DeDecimal(5000m);

        // Act
        var resultado = tabela.Calcular(baseCalculo);

        // Assert
        // 5000 × 8% = 400
        resultado.ValorFgts.Valor.Should().Be(400m);
        resultado.AliquotaAplicada.Should().Be(8m);
        resultado.EhAprendiz.Should().BeFalse();
    }

    [Fact]
    public void Calcular_Aprendiz_DeveCalcular2Porcento()
    {
        // Arrange
        var tabela = TabelaFgts.CriarTabelaPadrao();
        var baseCalculo = Dinheiro.DeDecimal(1500m);

        // Act
        var resultado = tabela.Calcular(baseCalculo, ehAprendiz: true);

        // Assert
        // 1500 × 2% = 30
        resultado.ValorFgts.Valor.Should().Be(30m);
        resultado.AliquotaAplicada.Should().Be(2m);
        resultado.EhAprendiz.Should().BeTrue();
    }

    [Theory]
    [InlineData(1000, 80)]
    [InlineData(2500, 200)]
    [InlineData(5000, 400)]
    [InlineData(10000, 800)]
    [InlineData(15000, 1200)]
    public void Calcular_DiversosSalarios_DeveCalcularCorretamente(decimal salario, decimal fgtsEsperado)
    {
        // Arrange
        var tabela = TabelaFgts.CriarTabelaPadrao();
        var baseCalculo = Dinheiro.DeDecimal(salario);

        // Act
        var resultado = tabela.Calcular(baseCalculo);

        // Assert
        resultado.ValorFgts.Valor.Should().Be(fgtsEsperado);
    }

    [Fact]
    public void Calcular_BaseNula_DeveLancarExcecao()
    {
        // Arrange
        var tabela = TabelaFgts.CriarTabelaPadrao();

        // Act
        var acao = () => tabela.Calcular(null!);

        // Assert
        acao.Should().Throw<ArgumentNullException>()
            .WithParameterName("baseCalculo");
    }

    #endregion

    #region Memória de Cálculo

    [Fact]
    public void Calcular_DeveRetornarMemoriaCompleta()
    {
        // Arrange
        var tabela = TabelaFgts.CriarTabelaPadrao();
        var baseCalculo = Dinheiro.DeDecimal(3000m);

        // Act
        var resultado = tabela.Calcular(baseCalculo);

        // Assert
        resultado.BaseCalculo.Should().Be(baseCalculo);
        resultado.AliquotaAplicada.Should().Be(8m);
        resultado.ValorFgts.Valor.Should().Be(240m);
        resultado.EhAprendiz.Should().BeFalse();
        resultado.TabelaUtilizada.Should().Be("FGTS-PADRAO");
    }

    #endregion

    #region Igualdade

    [Fact]
    public void Equals_TabelasIguais_DeveRetornarTrue()
    {
        // Arrange
        var tab1 = TabelaFgts.CriarTabelaPadrao();
        var tab2 = TabelaFgts.CriarTabelaPadrao();

        // Act & Assert
        tab1.Should().Be(tab2);
        (tab1 == tab2).Should().BeTrue();
    }

    #endregion
}
