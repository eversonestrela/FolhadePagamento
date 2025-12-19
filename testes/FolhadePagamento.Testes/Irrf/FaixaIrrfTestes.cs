using FluentAssertions;
using FolhadePagamento.Dominio.Irrf;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Irrf;

/// <summary>
/// Testes para o Value Object FaixaIrrf.
/// </summary>
public class FaixaIrrfTestes
{
    #region Criação

    [Fact]
    public void Criar_ComValoresValidos_DeveCriarFaixa()
    {
        // Arrange
        var limiteInferior = Dinheiro.DeDecimal(2259.20m);
        var limiteSuperior = Dinheiro.DeDecimal(2826.65m);
        var aliquota = 7.5m;
        var parcelaADeduzir = Dinheiro.DeDecimal(169.44m);

        // Act
        var faixa = FaixaIrrf.Criar(limiteInferior, limiteSuperior, aliquota, parcelaADeduzir);

        // Assert
        faixa.LimiteInferior.Should().Be(limiteInferior);
        faixa.LimiteSuperior.Should().Be(limiteSuperior);
        faixa.Aliquota.Should().Be(aliquota);
        faixa.ParcelaADeduzir.Should().Be(parcelaADeduzir);
    }

    [Fact]
    public void CriarFaixaIsenta_DeveRetornarFaixaComAliquotaZero()
    {
        // Act
        var faixa = FaixaIrrf.CriarFaixaIsenta(Dinheiro.Zero, Dinheiro.DeDecimal(2259.20m));

        // Assert
        faixa.EhFaixaIsenta.Should().BeTrue();
        faixa.Aliquota.Should().Be(0);
        faixa.ParcelaADeduzir.Valor.Should().Be(0);
    }

    [Fact]
    public void Criar_SemLimiteSuperior_DevePermitir()
    {
        // Arrange & Act
        var faixa = FaixaIrrf.Criar(Dinheiro.DeDecimal(4664.68m), null, 27.5m, Dinheiro.DeDecimal(896.00m));

        // Assert
        faixa.LimiteSuperior.Should().BeNull();
    }

    [Fact]
    public void Criar_ComLimiteInferiorNulo_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => FaixaIrrf.Criar(null!, Dinheiro.DeDecimal(1000m), 7.5m, Dinheiro.Zero);

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
        var acao = () => FaixaIrrf.Criar(Dinheiro.Zero, Dinheiro.DeDecimal(1000m), aliquota, Dinheiro.Zero);

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("aliquota");
    }

    [Fact]
    public void Criar_ComLimiteSuperiorMenorQueInferior_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => FaixaIrrf.Criar(
            Dinheiro.DeDecimal(1000m),
            Dinheiro.DeDecimal(500m),
            7.5m,
            Dinheiro.Zero);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithParameterName("limiteSuperior");
    }

    #endregion

    #region Cálculo de Imposto

    [Fact]
    public void CalcularImposto_FaixaIsenta_DeveRetornarZero()
    {
        // Arrange
        var faixa = FaixaIrrf.CriarFaixaIsenta(Dinheiro.Zero, Dinheiro.DeDecimal(2259.20m));
        var baseCalculo = Dinheiro.DeDecimal(2000m);

        // Act
        var imposto = faixa.CalcularImposto(baseCalculo);

        // Assert
        imposto.Valor.Should().Be(0);
    }

    [Fact]
    public void CalcularImposto_FaixaNaoIsenta_DeveCalcularCorretamente()
    {
        // Arrange - Faixa 7.5% com dedução de R$ 169.44
        var faixa = FaixaIrrf.Criar(
            Dinheiro.DeDecimal(2259.20m),
            Dinheiro.DeDecimal(2826.65m),
            7.5m,
            Dinheiro.DeDecimal(169.44m));
        var baseCalculo = Dinheiro.DeDecimal(2500m);

        // Act
        var imposto = faixa.CalcularImposto(baseCalculo);

        // Assert
        // 2500 * 7.5% = 187.50 - 169.44 = 18.06
        imposto.Valor.Should().Be(18.06m);
    }

    [Fact]
    public void CalcularImposto_ResultadoNegativo_DeveRetornarZero()
    {
        // Arrange - Base muito baixa para a faixa
        var faixa = FaixaIrrf.Criar(
            Dinheiro.DeDecimal(2259.20m),
            Dinheiro.DeDecimal(2826.65m),
            7.5m,
            Dinheiro.DeDecimal(169.44m));
        var baseCalculo = Dinheiro.DeDecimal(1000m); // 1000 * 7.5% = 75 - 169.44 = negativo

        // Act
        var imposto = faixa.CalcularImposto(baseCalculo);

        // Assert
        imposto.Valor.Should().Be(0);
    }

    [Fact]
    public void CalcularImposto_UltimaFaixa_DeveCalcularCorretamente()
    {
        // Arrange - Última faixa 27.5% com dedução de R$ 896.00
        var faixa = FaixaIrrf.Criar(
            Dinheiro.DeDecimal(4664.68m),
            null,
            27.5m,
            Dinheiro.DeDecimal(896.00m));
        var baseCalculo = Dinheiro.DeDecimal(10000m);

        // Act
        var imposto = faixa.CalcularImposto(baseCalculo);

        // Assert
        // 10000 * 27.5% = 2750 - 896 = 1854.00
        imposto.Valor.Should().Be(1854.00m);
    }

    #endregion

    #region Verificação de Faixa

    [Fact]
    public void BaseEstaNaFaixa_BaseDentro_DeveRetornarTrue()
    {
        // Arrange
        var faixa = FaixaIrrf.Criar(
            Dinheiro.DeDecimal(2259.20m),
            Dinheiro.DeDecimal(2826.65m),
            7.5m,
            Dinheiro.DeDecimal(169.44m));

        // Act & Assert
        faixa.BaseEstaNaFaixa(Dinheiro.DeDecimal(2500m)).Should().BeTrue();
    }

    [Fact]
    public void BaseEstaNaFaixa_BaseNoLimiteInferior_DeveRetornarTrue()
    {
        // Arrange
        var faixa = FaixaIrrf.Criar(
            Dinheiro.DeDecimal(2259.20m),
            Dinheiro.DeDecimal(2826.65m),
            7.5m,
            Dinheiro.DeDecimal(169.44m));

        // Act & Assert
        faixa.BaseEstaNaFaixa(Dinheiro.DeDecimal(2259.20m)).Should().BeTrue();
    }

    [Fact]
    public void BaseEstaNaFaixa_BaseAbaixo_DeveRetornarFalse()
    {
        // Arrange
        var faixa = FaixaIrrf.Criar(
            Dinheiro.DeDecimal(2259.20m),
            Dinheiro.DeDecimal(2826.65m),
            7.5m,
            Dinheiro.DeDecimal(169.44m));

        // Act & Assert
        faixa.BaseEstaNaFaixa(Dinheiro.DeDecimal(2000m)).Should().BeFalse();
    }

    [Fact]
    public void BaseEstaNaFaixa_BaseAcima_DeveRetornarFalse()
    {
        // Arrange
        var faixa = FaixaIrrf.Criar(
            Dinheiro.DeDecimal(2259.20m),
            Dinheiro.DeDecimal(2826.65m),
            7.5m,
            Dinheiro.DeDecimal(169.44m));

        // Act & Assert
        faixa.BaseEstaNaFaixa(Dinheiro.DeDecimal(3000m)).Should().BeFalse();
    }

    [Fact]
    public void BaseEstaNaFaixa_UltimaFaixaSemLimite_DeveRetornarTrueParaQualquerValorAcima()
    {
        // Arrange
        var faixa = FaixaIrrf.Criar(
            Dinheiro.DeDecimal(4664.68m),
            null,
            27.5m,
            Dinheiro.DeDecimal(896.00m));

        // Act & Assert
        faixa.BaseEstaNaFaixa(Dinheiro.DeDecimal(50000m)).Should().BeTrue();
    }

    #endregion

    #region Igualdade

    [Fact]
    public void Equals_FaixasIguais_DeveRetornarTrue()
    {
        // Arrange
        var faixa1 = FaixaIrrf.Criar(Dinheiro.DeDecimal(2259.20m), Dinheiro.DeDecimal(2826.65m), 7.5m, Dinheiro.DeDecimal(169.44m));
        var faixa2 = FaixaIrrf.Criar(Dinheiro.DeDecimal(2259.20m), Dinheiro.DeDecimal(2826.65m), 7.5m, Dinheiro.DeDecimal(169.44m));

        // Act & Assert
        faixa1.Should().Be(faixa2);
        (faixa1 == faixa2).Should().BeTrue();
    }

    [Fact]
    public void Equals_FaixasDiferentes_DeveRetornarFalse()
    {
        // Arrange
        var faixa1 = FaixaIrrf.Criar(Dinheiro.DeDecimal(2259.20m), Dinheiro.DeDecimal(2826.65m), 7.5m, Dinheiro.DeDecimal(169.44m));
        var faixa2 = FaixaIrrf.Criar(Dinheiro.DeDecimal(2259.20m), Dinheiro.DeDecimal(2826.65m), 15m, Dinheiro.DeDecimal(381.44m));

        // Act & Assert
        faixa1.Should().NotBe(faixa2);
        (faixa1 != faixa2).Should().BeTrue();
    }

    #endregion
}
