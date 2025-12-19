using FluentAssertions;
using FolhadePagamento.Dominio.Inss;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Inss;

/// <summary>
/// Testes para o Value Object TabelaInss.
/// </summary>
public class TabelaInssTestes
{
    #region Criação

    [Fact]
    public void Criar_ComValoresValidos_DeveCriarTabela()
    {
        // Arrange
        var vigencia = Vigencia.Indefinida(new DateTime(2025, 1, 1));
        var teto = Dinheiro.DeDecimal(8157.41m);
        var faixas = new[]
        {
            FaixaInss.Criar(Dinheiro.Zero, Dinheiro.DeDecimal(1518m), 7.5m)
        };

        // Act
        var tabela = TabelaInss.Criar("INSS-TEST", "Tabela Teste", vigencia, faixas, teto);

        // Assert
        tabela.Identificador.Should().Be("INSS-TEST");
        tabela.Descricao.Should().Be("Tabela Teste");
        tabela.Vigencia.Should().Be(vigencia);
        tabela.Faixas.Should().HaveCount(1);
        tabela.Teto.Should().Be(teto);
    }

    [Fact]
    public void Criar_SemFaixas_DeveLancarExcecao()
    {
        // Arrange
        var vigencia = Vigencia.Indefinida(new DateTime(2025, 1, 1));
        var faixas = Array.Empty<FaixaInss>();

        // Act
        var acao = () => TabelaInss.Criar(
            "INSS-TEST",
            "Tabela Teste",
            vigencia,
            faixas,
            Dinheiro.DeDecimal(8000m));

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
            FaixaInss.Criar(Dinheiro.DeDecimal(100m), Dinheiro.DeDecimal(1000m), 7.5m)
        };

        // Act
        var acao = () => TabelaInss.Criar(
            "INSS-TEST",
            "Tabela Teste",
            vigencia,
            faixas,
            Dinheiro.DeDecimal(8000m));

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*primeira faixa deve começar em R$ 0,00*");
    }

    [Fact]
    public void Criar_ComIdentificadorVazio_DeveLancarExcecao()
    {
        // Arrange
        var vigencia = Vigencia.Indefinida(new DateTime(2025, 1, 1));
        var faixas = new[] { FaixaInss.Criar(Dinheiro.Zero, Dinheiro.DeDecimal(1000m), 7.5m) };

        // Act
        var acao = () => TabelaInss.Criar("", "Tabela Teste", vigencia, faixas, Dinheiro.DeDecimal(8000m));

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithParameterName("identificador");
    }

    #endregion

    #region Tabelas Padrão

    [Fact]
    public void CriarTabela2025_DeveRetornarTabelaCorreta()
    {
        // Act
        var tabela = TabelaInss.CriarTabela2025();

        // Assert
        tabela.Identificador.Should().Be("INSS-2025");
        tabela.Faixas.Should().HaveCount(4);
        tabela.Teto.Valor.Should().Be(8157.41m);
    }

    [Fact]
    public void CriarTabela2024_DeveRetornarTabelaCorreta()
    {
        // Act
        var tabela = TabelaInss.CriarTabela2024();

        // Assert
        tabela.Identificador.Should().Be("INSS-2024");
        tabela.Faixas.Should().HaveCount(4);
        tabela.Teto.Valor.Should().Be(7786.02m);
    }

    #endregion

    #region Vigência

    [Fact]
    public void EstaVigenteParaCompetencia_CompetenciaVigente_DeveRetornarTrue()
    {
        // Arrange
        var tabela = TabelaInss.CriarTabela2024();
        var competencia = Competencia.DeAnoMes(2024, 6);

        // Act & Assert
        tabela.EstaVigenteParaCompetencia(competencia).Should().BeTrue();
    }

    [Fact]
    public void EstaVigenteParaCompetencia_CompetenciaNaoVigente_DeveRetornarFalse()
    {
        // Arrange
        var tabela = TabelaInss.CriarTabela2024();
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act & Assert
        tabela.EstaVigenteParaCompetencia(competencia).Should().BeFalse();
    }

    [Fact]
    public void EstaVigenteParaCompetencia_Tabela2025ECompetencia2025_DeveRetornarTrue()
    {
        // Arrange
        var tabela = TabelaInss.CriarTabela2025();
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act & Assert
        tabela.EstaVigenteParaCompetencia(competencia).Should().BeTrue();
    }

    #endregion

    #region Cálculo Progressivo

    [Fact]
    public void Calcular_SalarioNaPrimeiraFaixa_DeveCalcularCorretamente()
    {
        // Arrange
        var tabela = TabelaInss.CriarTabela2025();
        var salario = Dinheiro.DeDecimal(1000m);

        // Act
        var resultado = tabela.Calcular(salario);

        // Assert
        // 1000 * 7.5% = 75.00
        resultado.ValorInss.Valor.Should().Be(75.00m);
        resultado.BaseCalculo.Should().Be(salario);
        resultado.DetalhamentoPorFaixa.Should().HaveCount(1);
    }

    [Fact]
    public void Calcular_SalarioNaSegundaFaixa_DeveSerProgressivo()
    {
        // Arrange
        var tabela = TabelaInss.CriarTabela2025();
        var salario = Dinheiro.DeDecimal(2000m);

        // Act
        var resultado = tabela.Calcular(salario);

        // Assert
        // Faixa 1: 1518 * 7.5% = 113.85
        // Faixa 2: (2000 - 1518) * 9% = 482 * 9% = 43.38
        // Total: 113.85 + 43.38 = 157.23
        resultado.ValorInss.Valor.Should().Be(157.23m);
        resultado.DetalhamentoPorFaixa.Should().HaveCount(2);
    }

    [Fact]
    public void Calcular_SalarioNoTeto_DeveCalcularComTodasAsFaixas()
    {
        // Arrange
        var tabela = TabelaInss.CriarTabela2025();
        var salario = Dinheiro.DeDecimal(8157.41m); // Exatamente o teto

        // Act
        var resultado = tabela.Calcular(salario);

        // Assert
        // Faixa 1: 1518.00 * 7.5% = 113.85
        // Faixa 2: (2793.88 - 1518.00) * 9% = 1275.88 * 9% = 114.83 (arredondado)
        // Faixa 3: (4190.83 - 2793.88) * 12% = 1396.95 * 12% = 167.63 (arredondado)
        // Faixa 4: (8157.41 - 4190.83) * 14% = 3966.58 * 14% = 555.32 (arredondado)
        // Total esperado: ~951.63
        resultado.DetalhamentoPorFaixa.Should().HaveCount(4);
        resultado.ValorInss.Valor.Should().BeGreaterThan(900m);
        resultado.ValorInss.Valor.Should().BeLessThan(1000m);
    }

    [Fact]
    public void Calcular_SalarioAcimaDoTeto_DeveUsarTetoComoBase()
    {
        // Arrange
        var tabela = TabelaInss.CriarTabela2025();
        var salarioAlto = Dinheiro.DeDecimal(20000m);

        // Act
        var resultado = tabela.Calcular(salarioAlto);

        // Assert
        resultado.SalarioBruto.Valor.Should().Be(20000m);
        resultado.BaseCalculo.Valor.Should().Be(8157.41m); // Teto
    }

    [Fact]
    public void Calcular_SalarioZero_DeveRetornarInssZero()
    {
        // Arrange
        var tabela = TabelaInss.CriarTabela2025();
        var salario = Dinheiro.Zero;

        // Act
        var resultado = tabela.Calcular(salario);

        // Assert
        resultado.ValorInss.Valor.Should().Be(0m);
    }

    #endregion

    #region Resultado Detalhado

    [Fact]
    public void Calcular_DeveRetornarDetalhamentoPorFaixa()
    {
        // Arrange
        var tabela = TabelaInss.CriarTabela2025();
        var salario = Dinheiro.DeDecimal(3000m);

        // Act
        var resultado = tabela.Calcular(salario);

        // Assert
        resultado.TabelaUtilizada.Should().Be("INSS-2025");
        resultado.DetalhamentoPorFaixa.Should().NotBeEmpty();

        foreach (var detalhe in resultado.DetalhamentoPorFaixa)
        {
            detalhe.ValorContribuicao.Valor.Should().BeGreaterThan(0);
        }
    }

    #endregion
}
