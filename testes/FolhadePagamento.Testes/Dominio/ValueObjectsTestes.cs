using FluentAssertions;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Dominio;

/// <summary>
/// Testes unitários para Value Objects do Domínio.
/// Validam imutabilidade, igualdade e comportamentos.
/// </summary>
public class ValueObjectsTestes
{
    #region Dinheiro

    [Fact]
    public void Dinheiro_DeveSerImutavel_OperacoesRetornamNovaInstancia()
    {
        // Arrange
        var original = Dinheiro.DeDecimal(100.00m);

        // Act
        var resultado = original.Somar(Dinheiro.DeDecimal(50.00m));

        // Assert
        original.Valor.Should().Be(100.00m); // Original não mudou
        resultado.Valor.Should().Be(150.00m);
    }

    [Fact]
    public void Dinheiro_DeveArredondarPara2CasasDecimais()
    {
        // Act
        var dinheiro = Dinheiro.DeDecimal(100.555m);

        // Assert
        dinheiro.Valor.Should().Be(100.56m);
    }

    [Fact]
    public void Dinheiro_DeveLancarExcecao_QuandoValorNegativo()
    {
        // Act & Assert
        var acao = () => Dinheiro.DeDecimal(-100.00m);
        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Dinheiro_IgualdadeDeveSerPorValor()
    {
        // Arrange
        var dinheiro1 = Dinheiro.DeDecimal(500.00m);
        var dinheiro2 = Dinheiro.DeDecimal(500.00m);

        // Assert
        dinheiro1.Should().Be(dinheiro2);
        (dinheiro1 == dinheiro2).Should().BeTrue();
    }

    #endregion

    #region Competencia

    [Fact]
    public void Competencia_DeveCriarCorretamente()
    {
        // Act
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Assert
        competencia.Ano.Should().Be(2025);
        competencia.Mes.Should().Be(6);
    }

    [Fact]
    public void Competencia_ProximaDeveRetornarMesSeguinte()
    {
        // Arrange
        var competencia = Competencia.DeAnoMes(2025, 11);

        // Act
        var proxima = competencia.Proxima();

        // Assert
        proxima.Ano.Should().Be(2025);
        proxima.Mes.Should().Be(12);
    }

    [Fact]
    public void Competencia_ProximaDeveVirarAno_QuandoDezembro()
    {
        // Arrange
        var competencia = Competencia.DeAnoMes(2025, 12);

        // Act
        var proxima = competencia.Proxima();

        // Assert
        proxima.Ano.Should().Be(2026);
        proxima.Mes.Should().Be(1);
    }

    [Fact]
    public void Competencia_ConverterDeveParserarFormatoCorreto()
    {
        // Act
        var competencia = Competencia.Converter("2025-07");

        // Assert
        competencia.Ano.Should().Be(2025);
        competencia.Mes.Should().Be(7);
    }

    [Fact]
    public void Competencia_DeveLancarExcecao_QuandoMesInvalido()
    {
        // Act & Assert
        var acao = () => Competencia.DeAnoMes(2025, 13);
        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region FuncionarioId

    [Fact]
    public void FuncionarioId_NovosDevemSerUnicos()
    {
        // Act
        var id1 = FuncionarioId.Novo();
        var id2 = FuncionarioId.Novo();

        // Assert
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void FuncionarioId_DeGuidExistente_DevePreservarValor()
    {
        // Arrange
        var guidOriginal = Guid.Parse("12345678-1234-1234-1234-123456789012");

        // Act
        var funcionarioId = FuncionarioId.De(guidOriginal);

        // Assert
        funcionarioId.Valor.Should().Be(guidOriginal);
    }

    [Fact]
    public void FuncionarioId_DeveLancarExcecao_QuandoGuidVazio()
    {
        // Act & Assert
        var acao = () => FuncionarioId.De(Guid.Empty);
        acao.Should().Throw<ArgumentException>();
    }

    #endregion
}
