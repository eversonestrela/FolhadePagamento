using FluentAssertions;
using FolhadePagamento.Dominio.Entidades;
using FolhadePagamento.Dominio.Folha;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Dominio;

/// <summary>
/// Testes unitários para CalculadoraFolha.
/// Validam determinismo, ausência de efeitos colaterais e cálculos corretos.
/// </summary>
public class CalculadoraFolhaTestes
{
    private readonly CalculadoraFolha _calculadora;

    public CalculadoraFolhaTestes()
    {
        _calculadora = new CalculadoraFolha();
    }

    [Fact]
    public void Calcular_DeveRetornarSalarioLiquidoIgualAoSalarioBase_QuandoSemDescontos()
    {
        // Arrange
        var funcionarioId = FuncionarioId.Novo();
        var salarioBase = Dinheiro.DeDecimal(5000.00m);
        var funcionario = Funcionario.Criar(funcionarioId, "João Silva", salarioBase);
        var competencia = Competencia.DeAnoMes(2025, 1);
        var timestampCalculo = new DateTime(2025, 1, 15, 10, 30, 0);

        // Act
        var resultado = _calculadora.Calcular(funcionario, competencia, timestampCalculo);

        // Assert
        resultado.Should().NotBeNull();
        resultado.SalarioBruto.Valor.Should().Be(5000.00m);
        resultado.TotalDescontos.Valor.Should().Be(0m);
        resultado.SalarioLiquido.Valor.Should().Be(5000.00m);
    }

    [Fact]
    public void Calcular_DeveSerDeterministico_MesmaEntradaGeraMesmaSaida()
    {
        // Arrange
        var funcionarioId = FuncionarioId.De(Guid.Parse("12345678-1234-1234-1234-123456789012"));
        var salarioBase = Dinheiro.DeDecimal(3500.00m);
        var funcionario = Funcionario.Criar(funcionarioId, "Maria Santos", salarioBase);
        var competencia = Competencia.DeAnoMes(2025, 2);
        var timestampCalculo = new DateTime(2025, 2, 10, 14, 0, 0);

        // Act - Executar múltiplas vezes com mesma entrada
        var resultado1 = _calculadora.Calcular(funcionario, competencia, timestampCalculo);
        var resultado2 = _calculadora.Calcular(funcionario, competencia, timestampCalculo);
        var resultado3 = _calculadora.Calcular(funcionario, competencia, timestampCalculo);

        // Assert - Todos os resultados devem ser idênticos
        resultado1.SalarioBruto.Valor.Should().Be(resultado2.SalarioBruto.Valor);
        resultado2.SalarioBruto.Valor.Should().Be(resultado3.SalarioBruto.Valor);

        resultado1.SalarioLiquido.Valor.Should().Be(resultado2.SalarioLiquido.Valor);
        resultado2.SalarioLiquido.Valor.Should().Be(resultado3.SalarioLiquido.Valor);

        resultado1.TotalDescontos.Valor.Should().Be(resultado2.TotalDescontos.Valor);
        resultado2.TotalDescontos.Valor.Should().Be(resultado3.TotalDescontos.Valor);
    }

    [Fact]
    public void Calcular_NaoDeveAlterarFuncionarioOriginal_SemEfeitosColaterais()
    {
        // Arrange
        var funcionarioId = FuncionarioId.Novo();
        var salarioBaseOriginal = 4200.00m;
        var salarioBase = Dinheiro.DeDecimal(salarioBaseOriginal);
        var funcionario = Funcionario.Criar(funcionarioId, "Pedro Costa", salarioBase);
        var competencia = Competencia.DeAnoMes(2025, 3);
        var timestampCalculo = new DateTime(2025, 3, 5, 9, 0, 0);

        // Act
        var resultado = _calculadora.Calcular(funcionario, competencia, timestampCalculo);

        // Assert - Funcionário original não foi modificado
        funcionario.SalarioBase.Valor.Should().Be(salarioBaseOriginal);
        funcionario.Ativo.Should().BeTrue();
        funcionario.Nome.Should().Be("Pedro Costa");
    }

    [Fact]
    public void Calcular_DeveLancarExcecao_QuandoFuncionarioInativo()
    {
        // Arrange
        var funcionarioId = FuncionarioId.Novo();
        var salarioBase = Dinheiro.DeDecimal(2500.00m);
        var funcionario = Funcionario.Criar(funcionarioId, "Ana Souza", salarioBase);
        funcionario.Desativar(); // Desativar funcionário

        var competencia = Competencia.DeAnoMes(2025, 1);
        var timestampCalculo = new DateTime(2025, 1, 20, 8, 0, 0);

        // Act & Assert
        var acao = () => _calculadora.Calcular(funcionario, competencia, timestampCalculo);
        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*inativo*");
    }

    [Fact]
    public void Calcular_DeveRegistrarTimestampCorreto_FornecidoExternamente()
    {
        // Arrange
        var funcionarioId = FuncionarioId.Novo();
        var salarioBase = Dinheiro.DeDecimal(6000.00m);
        var funcionario = Funcionario.Criar(funcionarioId, "Carlos Lima", salarioBase);
        var competencia = Competencia.DeAnoMes(2025, 4);
        var timestampEsperado = new DateTime(2025, 4, 15, 16, 45, 30);

        // Act
        var resultado = _calculadora.Calcular(funcionario, competencia, timestampEsperado);

        // Assert - Timestamp deve ser exatamente o fornecido (não DateTime.Now)
        resultado.CalculadoEm.Should().Be(timestampEsperado);
    }

    [Fact]
    public void Calcular_DeveRetornarCompetenciaCorreta()
    {
        // Arrange
        var funcionarioId = FuncionarioId.Novo();
        var salarioBase = Dinheiro.DeDecimal(4500.00m);
        var funcionario = Funcionario.Criar(funcionarioId, "Lucia Ferreira", salarioBase);
        var competencia = Competencia.DeAnoMes(2025, 12);
        var timestampCalculo = new DateTime(2025, 12, 31, 23, 59, 59);

        // Act
        var resultado = _calculadora.Calcular(funcionario, competencia, timestampCalculo);

        // Assert
        resultado.Competencia.Ano.Should().Be(2025);
        resultado.Competencia.Mes.Should().Be(12);
        resultado.Competencia.ToString().Should().Be("2025-12");
    }

    [Fact]
    public void Calcular_DeveRetornarFuncionarioIdCorreto()
    {
        // Arrange
        var guidEsperado = Guid.Parse("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
        var funcionarioId = FuncionarioId.De(guidEsperado);
        var salarioBase = Dinheiro.DeDecimal(3000.00m);
        var funcionario = Funcionario.Criar(funcionarioId, "Roberto Dias", salarioBase);
        var competencia = Competencia.DeAnoMes(2025, 6);
        var timestampCalculo = new DateTime(2025, 6, 15, 12, 0, 0);

        // Act
        var resultado = _calculadora.Calcular(funcionario, competencia, timestampCalculo);

        // Assert
        resultado.FuncionarioId.Valor.Should().Be(guidEsperado);
    }
}
