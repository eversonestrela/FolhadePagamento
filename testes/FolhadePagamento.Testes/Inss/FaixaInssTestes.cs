using FluentAssertions;
using FolhadePagamento.Dominio.Inss;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Inss;

/// <summary>
/// Testes para o Value Object FaixaInss.
/// </summary>
public class FaixaInssTestes
{
    #region Criação

    [Fact]
    public void Criar_ComValoresValidos_DeveCriarFaixa()
    {
        // Arrange
        var limiteInferior = Dinheiro.DeDecimal(0m);
        var limiteSuperior = Dinheiro.DeDecimal(1518.00m);
        var aliquota = 7.5m;

        // Act
        var faixa = FaixaInss.Criar(limiteInferior, limiteSuperior, aliquota);

        // Assert
        faixa.LimiteInferior.Should().Be(limiteInferior);
        faixa.LimiteSuperior.Should().Be(limiteSuperior);
        faixa.Aliquota.Should().Be(aliquota);
    }

    [Fact]
    public void Criar_SemLimiteSuperior_DevePermitir()
    {
        // Arrange & Act
        var faixa = FaixaInss.Criar(Dinheiro.DeDecimal(5000m), null, 14m);

        // Assert
        faixa.LimiteSuperior.Should().BeNull();
    }

    [Fact]
    public void Criar_ComLimiteInferiorNulo_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => FaixaInss.Criar(null!, Dinheiro.DeDecimal(1000m), 7.5m);

        // Assert
        acao.Should().Throw<ArgumentNullException>()
            .WithParameterName("limiteInferior");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Criar_ComAliquotaInvalida_DeveLancarExcecao(decimal aliquota)
    {
        // Arrange & Act
        var acao = () => FaixaInss.Criar(Dinheiro.Zero, Dinheiro.DeDecimal(1000m), aliquota);

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("aliquota");
    }

    [Fact]
    public void Criar_ComLimiteSuperiorMenorQueInferior_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => FaixaInss.Criar(
            Dinheiro.DeDecimal(1000m),
            Dinheiro.DeDecimal(500m),
            7.5m);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithParameterName("limiteSuperior");
    }

    #endregion

    #region Cálculo de Contribuição

    [Fact]
    public void CalcularContribuicaoFaixa_SalarioAbaixoDoLimiteInferior_DeveRetornarZero()
    {
        // Arrange
        var faixa = FaixaInss.Criar(
            Dinheiro.DeDecimal(1518m),
            Dinheiro.DeDecimal(2793.88m),
            9m);
        var salario = Dinheiro.DeDecimal(1000m);

        // Act
        var contribuicao = faixa.CalcularContribuicaoFaixa(salario);

        // Assert
        contribuicao.Should().Be(Dinheiro.Zero);
    }

    [Fact]
    public void CalcularContribuicaoFaixa_SalarioDentroDaFaixa_DeveCalcularProporcional()
    {
        // Arrange - Primeira faixa: 0 a 1518 com 7.5%
        var faixa = FaixaInss.Criar(
            Dinheiro.DeDecimal(0m),
            Dinheiro.DeDecimal(1518m),
            7.5m);
        var salario = Dinheiro.DeDecimal(1000m);

        // Act
        var contribuicao = faixa.CalcularContribuicaoFaixa(salario);

        // Assert
        // 1000 * 7.5% = 75.00
        contribuicao.Valor.Should().Be(75.00m);
    }

    [Fact]
    public void CalcularContribuicaoFaixa_SalarioAcimaDoLimiteSuperior_DeveUsarAmplitudeDaFaixa()
    {
        // Arrange - Primeira faixa: 0 a 1518 com 7.5%
        var faixa = FaixaInss.Criar(
            Dinheiro.DeDecimal(0m),
            Dinheiro.DeDecimal(1518m),
            7.5m);
        var salario = Dinheiro.DeDecimal(5000m);

        // Act
        var contribuicao = faixa.CalcularContribuicaoFaixa(salario);

        // Assert
        // Amplitude = 1518 - 0 = 1518
        // 1518 * 7.5% = 113.85
        contribuicao.Valor.Should().Be(113.85m);
    }

    [Fact]
    public void CalcularContribuicaoFaixa_SegundaFaixa_DeveCalcularApenasExcedente()
    {
        // Arrange - Segunda faixa: 1518 a 2793.88 com 9%
        var faixa = FaixaInss.Criar(
            Dinheiro.DeDecimal(1518m),
            Dinheiro.DeDecimal(2793.88m),
            9m);
        var salario = Dinheiro.DeDecimal(2000m);

        // Act
        var contribuicao = faixa.CalcularContribuicaoFaixa(salario);

        // Assert
        // Excedente = 2000 - 1518 = 482
        // 482 * 9% = 43.38
        contribuicao.Valor.Should().Be(43.38m);
    }

    [Fact]
    public void CalcularContribuicaoFaixa_UltimaFaixaSemLimiteSuperior_DeveCalcularTodoExcedente()
    {
        // Arrange - Última faixa sem limite superior
        var faixa = FaixaInss.Criar(
            Dinheiro.DeDecimal(4190.83m),
            null,
            14m);
        var salario = Dinheiro.DeDecimal(8000m);

        // Act
        var contribuicao = faixa.CalcularContribuicaoFaixa(salario);

        // Assert
        // Excedente = 8000 - 4190.83 = 3809.17
        // 3809.17 * 14% = 533.28 (arredondado)
        contribuicao.Valor.Should().BeApproximately(533.28m, 0.01m);
    }

    #endregion

    #region Verificação de Faixa

    [Fact]
    public void SalarioEstaNaFaixa_SalarioDentro_DeveRetornarTrue()
    {
        // Arrange
        var faixa = FaixaInss.Criar(
            Dinheiro.DeDecimal(1000m),
            Dinheiro.DeDecimal(2000m),
            10m);

        // Act & Assert
        faixa.SalarioEstaNaFaixa(Dinheiro.DeDecimal(1500m)).Should().BeTrue();
    }

    [Fact]
    public void SalarioEstaNaFaixa_SalarioNoLimiteInferior_DeveRetornarTrue()
    {
        // Arrange
        var faixa = FaixaInss.Criar(
            Dinheiro.DeDecimal(1000m),
            Dinheiro.DeDecimal(2000m),
            10m);

        // Act & Assert
        faixa.SalarioEstaNaFaixa(Dinheiro.DeDecimal(1000m)).Should().BeTrue();
    }

    [Fact]
    public void SalarioEstaNaFaixa_SalarioNoLimiteSuperior_DeveRetornarTrue()
    {
        // Arrange
        var faixa = FaixaInss.Criar(
            Dinheiro.DeDecimal(1000m),
            Dinheiro.DeDecimal(2000m),
            10m);

        // Act & Assert
        faixa.SalarioEstaNaFaixa(Dinheiro.DeDecimal(2000m)).Should().BeTrue();
    }

    [Fact]
    public void SalarioEstaNaFaixa_SalarioAbaixo_DeveRetornarFalse()
    {
        // Arrange
        var faixa = FaixaInss.Criar(
            Dinheiro.DeDecimal(1000m),
            Dinheiro.DeDecimal(2000m),
            10m);

        // Act & Assert
        faixa.SalarioEstaNaFaixa(Dinheiro.DeDecimal(500m)).Should().BeFalse();
    }

    [Fact]
    public void SalarioEstaNaFaixa_SalarioAcima_DeveRetornarFalse()
    {
        // Arrange
        var faixa = FaixaInss.Criar(
            Dinheiro.DeDecimal(1000m),
            Dinheiro.DeDecimal(2000m),
            10m);

        // Act & Assert
        faixa.SalarioEstaNaFaixa(Dinheiro.DeDecimal(2500m)).Should().BeFalse();
    }

    #endregion

    #region Igualdade

    [Fact]
    public void Equals_FaixasIguais_DeveRetornarTrue()
    {
        // Arrange
        var faixa1 = FaixaInss.Criar(Dinheiro.Zero, Dinheiro.DeDecimal(1518m), 7.5m);
        var faixa2 = FaixaInss.Criar(Dinheiro.Zero, Dinheiro.DeDecimal(1518m), 7.5m);

        // Act & Assert
        faixa1.Should().Be(faixa2);
        (faixa1 == faixa2).Should().BeTrue();
    }

    [Fact]
    public void Equals_FaixasDiferentes_DeveRetornarFalse()
    {
        // Arrange
        var faixa1 = FaixaInss.Criar(Dinheiro.Zero, Dinheiro.DeDecimal(1518m), 7.5m);
        var faixa2 = FaixaInss.Criar(Dinheiro.Zero, Dinheiro.DeDecimal(1518m), 9m);

        // Act & Assert
        faixa1.Should().NotBe(faixa2);
        (faixa1 != faixa2).Should().BeTrue();
    }

    #endregion
}
