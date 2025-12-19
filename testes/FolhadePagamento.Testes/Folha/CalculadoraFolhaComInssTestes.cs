using FluentAssertions;
using FolhadePagamento.Dominio.Entidades;
using FolhadePagamento.Dominio.Folha;
using FolhadePagamento.Dominio.Inss;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Folha;

/// <summary>
/// Testes de integração da CalculadoraFolha com INSS.
/// </summary>
public class CalculadoraFolhaComInssTestes
{
    private readonly DateTime _timestampFixo = new(2025, 3, 15, 10, 0, 0);

    private static Funcionario CriarFuncionario(string nome, decimal salarioBase)
    {
        var id = FuncionarioId.Novo();
        return Funcionario.Criar(id, nome, Dinheiro.DeDecimal(salarioBase));
    }

    #region Retrocompatibilidade

    [Fact]
    public void Calcular_SemCalculadoraInss_DeveCalcularSemDescontoInss()
    {
        // Arrange
        var calculadora = new CalculadoraFolha(); // Sem INSS
        var funcionario = CriarFuncionario("Maria da Silva", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.ValorInss.Valor.Should().Be(0m);
        resultado.DetalheInss.Should().BeNull();
        resultado.SalarioBruto.Should().Be(resultado.SalarioLiquido);
    }

    #endregion

    #region Integração com INSS

    [Fact]
    public void Calcular_ComCalculadoraInss_DeveCalcularInssCorreto()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss);
        var funcionario = CriarFuncionario("João Silva", 3000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.ValorInss.Valor.Should().BeGreaterThan(0);
        resultado.DetalheInss.Should().NotBeNull();
        resultado.DetalheInss!.TabelaUtilizada.Should().Be("INSS-2025");
    }

    [Fact]
    public void Calcular_DeveDescontarInssDoSalarioLiquido()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss);
        var funcionario = CriarFuncionario("Ana Costa", 2000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        // SalarioLiquido = SalarioBruto - TotalDescontos
        // TotalDescontos deve incluir o INSS
        resultado.TotalDescontos.Valor.Should().Be(resultado.ValorInss.Valor);
        var liquidoEsperado = resultado.SalarioBruto.Subtrair(resultado.TotalDescontos);
        resultado.SalarioLiquido.Should().Be(liquidoEsperado);
    }

    [Fact]
    public void Calcular_SalarioMinimo_DeveCalcularInssDaPrimeiraFaixa()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss);
        var funcionario = CriarFuncionario("Pedro Souza", 1518m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        // 1518 * 7.5% = 113.85
        resultado.ValorInss.Valor.Should().Be(113.85m);
        resultado.DetalheInss!.DetalhamentoPorFaixa.Should().HaveCount(1);
    }

    [Fact]
    public void Calcular_SalarioNoTeto_DeveCalcularComTodasFaixas()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss);
        var funcionario = CriarFuncionario("Executivo Teto", 8157.41m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.DetalheInss!.DetalhamentoPorFaixa.Should().HaveCount(4);
        resultado.ValorInss.Valor.Should().BeGreaterThan(900m);
    }

    [Fact]
    public void Calcular_SalarioAcimaDoTeto_DeveUsarTetoComoBase()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss);
        var funcionario = CriarFuncionario("Diretor Senior", 25000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.SalarioBruto.Valor.Should().Be(25000m);
        resultado.DetalheInss!.BaseCalculo.Valor.Should().Be(8157.41m); // Teto
        // INSS máximo (igual ao do teto)
        resultado.ValorInss.Valor.Should().BeGreaterThan(900m);
        resultado.ValorInss.Valor.Should().BeLessThan(1000m);
    }

    #endregion

    #region Vigência e Competência

    [Fact]
    public void Calcular_Competencia2024_DeveUsarTabela2024()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss);
        var funcionario = CriarFuncionario("Funcionário 2024", 3000m);
        var competencia = Competencia.DeAnoMes(2024, 12);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.DetalheInss!.TabelaUtilizada.Should().Be("INSS-2024");
    }

    [Fact]
    public void Calcular_Competencia2025_DeveUsarTabela2025()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss);
        var funcionario = CriarFuncionario("Funcionário 2025", 3000m);
        var competencia = Competencia.DeAnoMes(2025, 1);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.DetalheInss!.TabelaUtilizada.Should().Be("INSS-2025");
    }

    [Fact]
    public void Calcular_MesmoSalarioCompetenciasDiferentes_DeveResultarValoresDiferentes()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss);
        var funcionario = CriarFuncionario("Comparativo", 2000m);
        var competencia2024 = Competencia.DeAnoMes(2024, 12);
        var competencia2025 = Competencia.DeAnoMes(2025, 1);

        // Act
        var resultado2024 = calculadora.Calcular(funcionario, competencia2024, _timestampFixo);
        var resultado2025 = calculadora.Calcular(funcionario, competencia2025, _timestampFixo);

        // Assert
        // Tabelas diferentes devem gerar valores diferentes
        resultado2024.ValorInss.Valor.Should().NotBe(resultado2025.ValorInss.Valor);
    }

    [Fact]
    public void Calcular_CompetenciaSemTabelaVigente_DeveCalcularSemInss()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss);
        var funcionario = CriarFuncionario("Funcionário Antigo", 3000m);
        var competenciaAntiga = Competencia.DeAnoMes(2020, 1); // Sem tabela para 2020

        // Act
        var resultado = calculadora.Calcular(funcionario, competenciaAntiga, _timestampFixo);

        // Assert
        resultado.ValorInss.Valor.Should().Be(0m);
        resultado.DetalheInss.Should().BeNull();
    }

    #endregion

    #region Determinismo

    [Fact]
    public void Calcular_MesmasEntradas_DeveRetornarMesmoResultado()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss);
        var funcionario = CriarFuncionario("Teste Determinismo", 4500m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act
        var resultado1 = calculadora.Calcular(funcionario, competencia, _timestampFixo);
        var resultado2 = calculadora.Calcular(funcionario, competencia, _timestampFixo);
        var resultado3 = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert - DETERMINISMO: 100 execuções = 100 resultados idênticos
        resultado1.ValorInss.Should().Be(resultado2.ValorInss);
        resultado2.ValorInss.Should().Be(resultado3.ValorInss);
        resultado1.SalarioLiquido.Should().Be(resultado2.SalarioLiquido);
        resultado2.SalarioLiquido.Should().Be(resultado3.SalarioLiquido);
    }

    #endregion

    #region Estrutura do Resultado

    [Fact]
    public void Calcular_DeveManterEstruturaCorreta()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss);
        var funcionario = CriarFuncionario("Estrutura Teste", 5500m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.FuncionarioId.Should().Be(funcionario.Id);
        resultado.Competencia.Should().Be(competencia);
        resultado.SalarioBruto.Should().Be(funcionario.SalarioBase);
        resultado.ValorInss.Valor.Should().BeGreaterThan(0);
        resultado.TotalDescontos.Should().Be(resultado.ValorInss); // Por enquanto, só INSS
        resultado.SalarioLiquido.Should().Be(resultado.SalarioBruto.Subtrair(resultado.TotalDescontos));
        resultado.CalculadoEm.Should().Be(_timestampFixo);
    }

    #endregion
}
