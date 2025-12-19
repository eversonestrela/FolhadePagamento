using FluentAssertions;
using FolhadePagamento.Aplicacao.CasosDeUso;
using FolhadePagamento.Aplicacao.DTOs;
using FolhadePagamento.Dominio.Folha;
using Xunit;

namespace FolhadePagamento.Testes.Aplicacao;

/// <summary>
/// Testes unitários para ProcessarFolhaBasicaCasoDeUso.
/// Validam orquestração correta e mapeamento de DTOs.
/// </summary>
public class ProcessarFolhaBasicaCasoDeUsoTestes
{
    private readonly ProcessarFolhaBasicaCasoDeUso _casoDeUso;

    public ProcessarFolhaBasicaCasoDeUsoTestes()
    {
        var calculadora = new CalculadoraFolha();
        _casoDeUso = new ProcessarFolhaBasicaCasoDeUso(calculadora);
    }

    [Fact]
    public void Executar_DeveRetornarSucessoComValoresCorretos()
    {
        // Arrange
        var entrada = new ProcessarFolhaBasicaEntrada
        {
            FuncionarioId = Guid.NewGuid(),
            NomeFuncionario = "João Silva",
            SalarioBase = 5000.00m,
            Competencia = "2025-01",
            TimestampCalculo = new DateTime(2025, 1, 15, 10, 30, 0)
        };

        // Act
        var saida = _casoDeUso.Executar(entrada);

        // Assert
        saida.Sucesso.Should().BeTrue();
        saida.MensagemErro.Should().BeNull();
        saida.SalarioBruto.Should().Be(5000.00m);
        saida.TotalDescontos.Should().Be(0m);
        saida.SalarioLiquido.Should().Be(5000.00m);
        saida.Competencia.Should().Be("2025-01");
    }

    [Fact]
    public void Executar_DeveSerDeterministico_MesmaEntradaGeraMesmaSaida()
    {
        // Arrange
        var funcionarioId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var entrada = new ProcessarFolhaBasicaEntrada
        {
            FuncionarioId = funcionarioId,
            NomeFuncionario = "Maria Santos",
            SalarioBase = 3500.00m,
            Competencia = "2025-02",
            TimestampCalculo = new DateTime(2025, 2, 10, 14, 0, 0)
        };

        // Act
        var saida1 = _casoDeUso.Executar(entrada);
        var saida2 = _casoDeUso.Executar(entrada);

        // Assert
        saida1.SalarioLiquido.Should().Be(saida2.SalarioLiquido);
        saida1.SalarioBruto.Should().Be(saida2.SalarioBruto);
        saida1.TotalDescontos.Should().Be(saida2.TotalDescontos);
    }

    [Fact]
    public void Executar_DeveRetornarErro_QuandoFuncionarioIdVazio()
    {
        // Arrange
        var entrada = new ProcessarFolhaBasicaEntrada
        {
            FuncionarioId = Guid.Empty,
            NomeFuncionario = "Teste",
            SalarioBase = 1000.00m,
            Competencia = "2025-01",
            TimestampCalculo = new DateTime(2025, 1, 15, 10, 0, 0)
        };

        // Act
        var saida = _casoDeUso.Executar(entrada);

        // Assert
        saida.Sucesso.Should().BeFalse();
        saida.MensagemErro.Should().Contain("FuncionarioId");
    }

    [Fact]
    public void Executar_DeveRetornarErro_QuandoSalarioBaseZero()
    {
        // Arrange
        var entrada = new ProcessarFolhaBasicaEntrada
        {
            FuncionarioId = Guid.NewGuid(),
            NomeFuncionario = "Teste",
            SalarioBase = 0m,
            Competencia = "2025-01",
            TimestampCalculo = new DateTime(2025, 1, 15, 10, 0, 0)
        };

        // Act
        var saida = _casoDeUso.Executar(entrada);

        // Assert
        saida.Sucesso.Should().BeFalse();
        saida.MensagemErro.Should().Contain("SalarioBase");
    }

    [Fact]
    public void Executar_DeveRetornarErro_QuandoCompetenciaVazia()
    {
        // Arrange
        var entrada = new ProcessarFolhaBasicaEntrada
        {
            FuncionarioId = Guid.NewGuid(),
            NomeFuncionario = "Teste",
            SalarioBase = 1000.00m,
            Competencia = "",
            TimestampCalculo = new DateTime(2025, 1, 15, 10, 0, 0)
        };

        // Act
        var saida = _casoDeUso.Executar(entrada);

        // Assert
        saida.Sucesso.Should().BeFalse();
        saida.MensagemErro.Should().Contain("Competencia");
    }

    [Fact]
    public void Executar_DeveMapearFuncionarioIdCorretamente()
    {
        // Arrange
        var funcionarioIdEsperado = Guid.Parse("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
        var entrada = new ProcessarFolhaBasicaEntrada
        {
            FuncionarioId = funcionarioIdEsperado,
            NomeFuncionario = "Roberto Dias",
            SalarioBase = 3000.00m,
            Competencia = "2025-06",
            TimestampCalculo = new DateTime(2025, 6, 15, 12, 0, 0)
        };

        // Act
        var saida = _casoDeUso.Executar(entrada);

        // Assert
        saida.Sucesso.Should().BeTrue();
        saida.FuncionarioId.Should().Be(funcionarioIdEsperado);
    }

    [Fact]
    public void Executar_DeveMapearTimestampCorretamente()
    {
        // Arrange
        var timestampEsperado = new DateTime(2025, 7, 20, 15, 30, 45);
        var entrada = new ProcessarFolhaBasicaEntrada
        {
            FuncionarioId = Guid.NewGuid(),
            NomeFuncionario = "Paula Oliveira",
            SalarioBase = 4500.00m,
            Competencia = "2025-07",
            TimestampCalculo = timestampEsperado
        };

        // Act
        var saida = _casoDeUso.Executar(entrada);

        // Assert
        saida.Sucesso.Should().BeTrue();
        saida.CalculadoEm.Should().Be(timestampEsperado);
    }
}
