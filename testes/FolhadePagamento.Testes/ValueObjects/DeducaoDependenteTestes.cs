using FluentAssertions;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.ValueObjects;

/// <summary>
/// Testes para o Value Object DeducaoDependente.
/// </summary>
public class DeducaoDependenteTestes
{
    #region Criação

    [Fact]
    public void Criar_ComValoresValidos_DeveCriarDeducao()
    {
        // Arrange
        var identificador = "DEDUCAO-DEP-2025";
        var descricao = "Dedução por dependente IRRF 2025";
        var valorUnitario = Dinheiro.DeDecimal(189.59m);
        var vigencia = Vigencia.Indefinida(new DateTime(2025, 1, 1));

        // Act
        var deducao = DeducaoDependente.Criar(identificador, descricao, valorUnitario, vigencia);

        // Assert
        deducao.Identificador.Should().Be(identificador);
        deducao.Descricao.Should().Be(descricao);
        deducao.ValorUnitario.Should().Be(valorUnitario);
        deducao.Vigencia.Should().Be(vigencia);
    }

    [Fact]
    public void Criar_SemIdentificador_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => DeducaoDependente.Criar(
            "",
            "Descrição",
            Dinheiro.DeDecimal(100m),
            Vigencia.Indefinida(new DateTime(2025, 1, 1)));

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*Identificador é obrigatório*");
    }

    [Fact]
    public void Criar_SemDescricao_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => DeducaoDependente.Criar(
            "ID-123",
            "",
            Dinheiro.DeDecimal(100m),
            Vigencia.Indefinida(new DateTime(2025, 1, 1)));

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*Descrição é obrigatória*");
    }

    [Fact]
    public void Criar_ComValorNulo_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => DeducaoDependente.Criar(
            "ID-123",
            "Descrição",
            null!,
            Vigencia.Indefinida(new DateTime(2025, 1, 1)));

        // Assert
        acao.Should().Throw<ArgumentNullException>()
            .WithParameterName("valorUnitario");
    }

    [Fact]
    public void Criar_ComVigenciaNula_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => DeducaoDependente.Criar(
            "ID-123",
            "Descrição",
            Dinheiro.DeDecimal(100m),
            null!);

        // Assert
        acao.Should().Throw<ArgumentNullException>()
            .WithParameterName("vigencia");
    }

    #endregion

    #region Cálculo de Dedução Total

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 189.59)]
    [InlineData(2, 379.18)]
    [InlineData(3, 568.77)]
    [InlineData(5, 947.95)]
    public void CalcularDeducaoTotal_DeveRetornarValorCorreto(int numeroDependentes, decimal valorEsperado)
    {
        // Arrange
        var deducao = DeducaoDependente.Criar2025();

        // Act
        var total = deducao.CalcularDeducaoTotal(numeroDependentes);

        // Assert
        total.Valor.Should().Be(valorEsperado);
    }

    [Fact]
    public void CalcularDeducaoTotal_ComDependentesNegativo_DeveLancarExcecao()
    {
        // Arrange
        var deducao = DeducaoDependente.Criar2025();

        // Act
        var acao = () => deducao.CalcularDeducaoTotal(-1);

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("numeroDependentes");
    }

    #endregion

    #region Vigência

    [Fact]
    public void EstaVigenteParaCompetencia_CompetenciaVigente_DeveRetornarTrue()
    {
        // Arrange
        var deducao = DeducaoDependente.Criar2025();
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act & Assert
        deducao.EstaVigenteParaCompetencia(competencia).Should().BeTrue();
    }

    [Fact]
    public void EstaVigenteParaCompetencia_CompetenciaNaoVigente_DeveRetornarFalse()
    {
        // Arrange
        var deducao = DeducaoDependente.Criar2024();
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act & Assert
        deducao.EstaVigenteParaCompetencia(competencia).Should().BeFalse();
    }

    [Fact]
    public void EstaVigenteParaCompetencia_CompetenciaNula_DeveLancarExcecao()
    {
        // Arrange
        var deducao = DeducaoDependente.Criar2025();

        // Act
        var acao = () => deducao.EstaVigenteParaCompetencia(null!);

        // Assert
        acao.Should().Throw<ArgumentNullException>()
            .WithParameterName("competencia");
    }

    #endregion

    #region Fábricas de Tabelas Padrão

    [Fact]
    public void Criar2025_DeveRetornarDeducaoCorreta()
    {
        // Act
        var deducao = DeducaoDependente.Criar2025();

        // Assert
        deducao.Identificador.Should().Be("DEDUCAO-DEP-2025");
        deducao.ValorUnitario.Valor.Should().Be(189.59m);
    }

    [Fact]
    public void Criar2024_DeveRetornarDeducaoCorreta()
    {
        // Act
        var deducao = DeducaoDependente.Criar2024();

        // Assert
        deducao.Identificador.Should().Be("DEDUCAO-DEP-2024");
        deducao.ValorUnitario.Valor.Should().Be(189.59m);
    }

    #endregion

    #region Igualdade

    [Fact]
    public void Equals_DeducoesIguais_DeveRetornarTrue()
    {
        // Arrange
        var ded1 = DeducaoDependente.Criar2025();
        var ded2 = DeducaoDependente.Criar2025();

        // Act & Assert
        ded1.Should().Be(ded2);
        (ded1 == ded2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DeducoesDiferentes_DeveRetornarFalse()
    {
        // Arrange
        var ded1 = DeducaoDependente.Criar2024();
        var ded2 = DeducaoDependente.Criar2025();

        // Act & Assert
        ded1.Should().NotBe(ded2);
        (ded1 != ded2).Should().BeTrue();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_DeveRetornarFormatoCorreto()
    {
        // Arrange
        var deducao = DeducaoDependente.Criar2025();

        // Act
        var texto = deducao.ToString();

        // Assert
        texto.Should().Contain("189,59");
        texto.Should().Contain("por dependente");
    }

    #endregion
}
