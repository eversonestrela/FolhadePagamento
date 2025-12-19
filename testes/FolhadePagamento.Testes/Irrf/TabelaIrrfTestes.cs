using FluentAssertions;
using FolhadePagamento.Dominio.Irrf;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Irrf;

/// <summary>
/// Testes para o Value Object TabelaIrrf.
/// </summary>
public class TabelaIrrfTestes
{
    #region Criação

    [Fact]
    public void Criar_ComValoresValidos_DeveCriarTabela()
    {
        // Arrange
        var vigencia = Vigencia.Indefinida(new DateTime(2025, 1, 1));
        var deducaoPorDependente = Dinheiro.DeDecimal(189.59m);
        var faixas = new[]
        {
            FaixaIrrf.CriarFaixaIsenta(Dinheiro.Zero, Dinheiro.DeDecimal(2259.20m))
        };

        // Act
        var tabela = TabelaIrrf.Criar("IRRF-TEST", "Tabela Teste", vigencia, faixas, deducaoPorDependente);

        // Assert
        tabela.Identificador.Should().Be("IRRF-TEST");
        tabela.Descricao.Should().Be("Tabela Teste");
        tabela.Vigencia.Should().Be(vigencia);
        tabela.Faixas.Should().HaveCount(1);
        tabela.DeducaoPorDependente.Should().Be(deducaoPorDependente);
    }

    [Fact]
    public void Criar_SemFaixas_DeveLancarExcecao()
    {
        // Arrange
        var vigencia = Vigencia.Indefinida(new DateTime(2025, 1, 1));
        var faixas = Array.Empty<FaixaIrrf>();

        // Act
        var acao = () => TabelaIrrf.Criar(
            "IRRF-TEST",
            "Tabela Teste",
            vigencia,
            faixas,
            Dinheiro.DeDecimal(189.59m));

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*pelo menos uma faixa*");
    }

    [Fact]
    public void Criar_ComPrimeiraFaixaNaoComeçandoEmZero_DeveLancarExcecao()
    {
        // Arrange
        var vigencia = Vigencia.Indefinida(new DateTime(2025, 1, 1));
        var faixas = new[]
        {
            FaixaIrrf.Criar(Dinheiro.DeDecimal(100m), Dinheiro.DeDecimal(1000m), 7.5m, Dinheiro.DeDecimal(100m))
        };

        // Act
        var acao = () => TabelaIrrf.Criar(
            "IRRF-TEST",
            "Tabela Teste",
            vigencia,
            faixas,
            Dinheiro.DeDecimal(189.59m));

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*primeira faixa deve começar em R$ 0,00*");
    }

    #endregion

    #region Tabelas Padrão

    [Fact]
    public void CriarTabela2025_DeveRetornarTabelaCorreta()
    {
        // Act
        var tabela = TabelaIrrf.CriarTabela2025();

        // Assert
        tabela.Identificador.Should().Be("IRRF-2025");
        tabela.Faixas.Should().HaveCount(5);
        tabela.DeducaoPorDependente.Valor.Should().Be(189.59m);
    }

    [Fact]
    public void CriarTabela2024_DeveRetornarTabelaCorreta()
    {
        // Act
        var tabela = TabelaIrrf.CriarTabela2024();

        // Assert
        tabela.Identificador.Should().Be("IRRF-2024");
        tabela.Faixas.Should().HaveCount(5);
    }

    #endregion

    #region Vigência

    [Fact]
    public void EstaVigenteParaCompetencia_CompetenciaVigente_DeveRetornarTrue()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2024();
        var competencia = Competencia.DeAnoMes(2024, 6);

        // Act & Assert
        tabela.EstaVigenteParaCompetencia(competencia).Should().BeTrue();
    }

    [Fact]
    public void EstaVigenteParaCompetencia_CompetenciaNaoVigente_DeveRetornarFalse()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2024();
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act & Assert
        tabela.EstaVigenteParaCompetencia(competencia).Should().BeFalse();
    }

    #endregion

    #region Encontrar Faixa

    [Fact]
    public void EncontrarFaixa_BaseIsenta_DeveRetornarFaixaIsenta()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(2000m);

        // Act
        var faixa = tabela.EncontrarFaixa(baseCalculo);

        // Assert
        faixa.EhFaixaIsenta.Should().BeTrue();
    }

    [Fact]
    public void EncontrarFaixa_BaseNaSegundaFaixa_DeveRetornarFaixaCorreta()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(2500m);

        // Act
        var faixa = tabela.EncontrarFaixa(baseCalculo);

        // Assert
        faixa.Aliquota.Should().Be(7.5m);
    }

    [Fact]
    public void EncontrarFaixa_BaseAltissima_DeveRetornarUltimaFaixa()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(50000m);

        // Act
        var faixa = tabela.EncontrarFaixa(baseCalculo);

        // Assert
        faixa.Aliquota.Should().Be(27.5m);
    }

    #endregion

    #region Cálculo

    [Fact]
    public void Calcular_BaseIsenta_DeveRetornarIsentoDeIrrf()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(2000m);

        // Act
        var resultado = tabela.Calcular(baseCalculo);

        // Assert
        resultado.EhIsento.Should().BeTrue();
        resultado.ValorIrrf.Valor.Should().Be(0);
    }

    [Fact]
    public void Calcular_BaseNaSegundaFaixa_DeveCalcularCorretamente()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(2500m);

        // Act
        var resultado = tabela.Calcular(baseCalculo);

        // Assert
        // 2500 * 7.5% = 187.50 - 169.44 = 18.06
        resultado.ValorIrrf.Valor.Should().Be(18.06m);
        resultado.AliquotaEfetiva.Should().Be(7.5m);
        resultado.ParcelaADeduzir.Valor.Should().Be(169.44m);
    }

    [Fact]
    public void Calcular_ComDependentes_DeveReduzirBase()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(2500m);
        int dependentes = 2;

        // Act
        var resultado = tabela.Calcular(baseCalculo, dependentes);

        // Assert
        // Base ajustada = 2500 - (189.59 * 2) = 2500 - 379.18 = 2120.82
        // 2120.82 está na faixa isenta (até 2259.20)
        resultado.DeducaoPorDependentes.Valor.Should().Be(379.18m);
        resultado.BaseAjustada.Valor.Should().Be(2120.82m);
        resultado.EhIsento.Should().BeTrue();
    }

    [Fact]
    public void Calcular_SalarioAlto_DeveUsarUltimaFaixa()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(10000m);

        // Act
        var resultado = tabela.Calcular(baseCalculo);

        // Assert
        // 10000 * 27.5% = 2750 - 896 = 1854
        resultado.ValorIrrf.Valor.Should().Be(1854.00m);
        resultado.AliquotaEfetiva.Should().Be(27.5m);
    }

    [Fact]
    public void Calcular_DeveRetornarResultadoDetalhado()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(5000m);
        int dependentes = 1;

        // Act
        var resultado = tabela.Calcular(baseCalculo, dependentes);

        // Assert
        resultado.BaseOriginal.Should().Be(baseCalculo);
        resultado.NumeroDependentes.Should().Be(dependentes);
        resultado.TabelaUtilizada.Should().Be("IRRF-2025");
        resultado.FaixaAplicada.Should().NotBeNull();
    }

    [Fact]
    public void Calcular_DependentesNegativo_DeveLancarExcecao()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();

        // Act
        var acao = () => tabela.Calcular(Dinheiro.DeDecimal(5000m), -1);

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("numeroDependentes");
    }

    #endregion
}
