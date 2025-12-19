using FluentAssertions;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.ValueObjects;

/// <summary>
/// Testes para o Value Object QuantidadeDependentes.
/// 
/// Este Value Object representa APENAS a quantidade de dependentes
/// para fins de cálculo do IRRF, sem dados pessoais.
/// </summary>
public class QuantidadeDependentesTestes
{
    #region Criação

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void De_ComValorValido_DeveCriarQuantidade(int quantidade)
    {
        // Act
        var qtd = QuantidadeDependentes.De(quantidade);

        // Assert
        qtd.Valor.Should().Be(quantidade);
    }

    [Fact]
    public void De_ComValorNegativo_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => QuantidadeDependentes.De(-1);

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("quantidade")
            .WithMessage("*Quantidade de dependentes não pode ser negativa*");
    }

    [Fact]
    public void Zero_DeveRetornarQuantidadeZero()
    {
        // Act
        var qtd = QuantidadeDependentes.Zero;

        // Assert
        qtd.Valor.Should().Be(0);
        qtd.SemDependentes.Should().BeTrue();
        qtd.TemDependentes.Should().BeFalse();
    }

    #endregion

    #region Propriedades

    [Fact]
    public void TemDependentes_ComQuantidadeMaiorQueZero_DeveRetornarTrue()
    {
        // Arrange
        var qtd = QuantidadeDependentes.De(3);

        // Act & Assert
        qtd.TemDependentes.Should().BeTrue();
        qtd.SemDependentes.Should().BeFalse();
    }

    [Fact]
    public void SemDependentes_ComQuantidadeZero_DeveRetornarTrue()
    {
        // Arrange
        var qtd = QuantidadeDependentes.De(0);

        // Act & Assert
        qtd.SemDependentes.Should().BeTrue();
        qtd.TemDependentes.Should().BeFalse();
    }

    #endregion

    #region Cálculo de Dedução

    [Theory]
    [InlineData(0, 189.59, 0)]
    [InlineData(1, 189.59, 189.59)]
    [InlineData(2, 189.59, 379.18)]
    [InlineData(3, 189.59, 568.77)]
    [InlineData(5, 189.59, 947.95)]
    public void CalcularDeducaoTotal_DeveRetornarValorCorreto(int quantidade, decimal valorUnitario, decimal esperado)
    {
        // Arrange
        var qtd = QuantidadeDependentes.De(quantidade);
        var valorPorDependente = Dinheiro.DeDecimal(valorUnitario);

        // Act
        var total = qtd.CalcularDeducaoTotal(valorPorDependente);

        // Assert
        total.Valor.Should().Be(esperado);
    }

    [Fact]
    public void CalcularDeducaoTotal_ComValorNulo_DeveLancarExcecao()
    {
        // Arrange
        var qtd = QuantidadeDependentes.De(2);

        // Act
        var acao = () => qtd.CalcularDeducaoTotal(null!);

        // Assert
        acao.Should().Throw<ArgumentNullException>()
            .WithParameterName("valorUnitario");
    }

    #endregion

    #region Conversão Implícita

    [Fact]
    public void ConversaoImplicita_DeInt_DeveFuncionar()
    {
        // Arrange & Act
        QuantidadeDependentes qtd = 3;

        // Assert
        qtd.Valor.Should().Be(3);
    }

    [Fact]
    public void ConversaoImplicita_ParaInt_DeveFuncionar()
    {
        // Arrange
        var qtd = QuantidadeDependentes.De(5);

        // Act
        int valor = qtd;

        // Assert
        valor.Should().Be(5);
    }

    [Fact]
    public void ConversaoImplicita_DeIntNegativo_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => { QuantidadeDependentes qtd = -1; };

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Igualdade

    [Fact]
    public void Equals_QuantidadesIguais_DeveRetornarTrue()
    {
        // Arrange
        var qtd1 = QuantidadeDependentes.De(3);
        var qtd2 = QuantidadeDependentes.De(3);

        // Act & Assert
        qtd1.Equals(qtd2).Should().BeTrue();
        (qtd1 == qtd2).Should().BeTrue();
    }

    [Fact]
    public void Equals_QuantidadesDiferentes_DeveRetornarFalse()
    {
        // Arrange
        var qtd1 = QuantidadeDependentes.De(2);
        var qtd2 = QuantidadeDependentes.De(3);

        // Act & Assert
        qtd1.Equals(qtd2).Should().BeFalse();
        (qtd1 != qtd2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_QuantidadesIguais_DeveSerIgual()
    {
        // Arrange
        var qtd1 = QuantidadeDependentes.De(5);
        var qtd2 = QuantidadeDependentes.De(5);

        // Act & Assert
        qtd1.GetHashCode().Should().Be(qtd2.GetHashCode());
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_UmDependente_DeveRetornarSingular()
    {
        // Arrange
        var qtd = QuantidadeDependentes.De(1);

        // Act
        var texto = qtd.ToString();

        // Assert
        texto.Should().Be("1 dependente");
    }

    [Fact]
    public void ToString_MultiplosDependentes_DeveRetornarPlural()
    {
        // Arrange
        var qtd = QuantidadeDependentes.De(3);

        // Act
        var texto = qtd.ToString();

        // Assert
        texto.Should().Be("3 dependentes");
    }

    [Fact]
    public void ToString_ZeroDependentes_DeveRetornarPlural()
    {
        // Arrange
        var qtd = QuantidadeDependentes.Zero;

        // Act
        var texto = qtd.ToString();

        // Assert
        texto.Should().Be("0 dependentes");
    }

    #endregion
}
