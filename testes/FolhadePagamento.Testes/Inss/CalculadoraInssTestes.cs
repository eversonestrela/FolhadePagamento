using FluentAssertions;
using FolhadePagamento.Dominio.Inss;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Inss;

/// <summary>
/// Testes para o Serviço de Domínio CalculadoraInss.
/// </summary>
public class CalculadoraInssTestes
{
    #region Criação

    [Fact]
    public void Criar_SemTabelas_DeveLancarExcecao()
    {
        // Arrange & Act
        var acao = () => new CalculadoraInss(Array.Empty<TabelaInss>());

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*pelo menos uma tabela*");
    }

    [Fact]
    public void CriarComTabelasPadrao_DeveCriarComTabelas2024E2025()
    {
        // Act
        var calculadora = CalculadoraInss.CriarComTabelasPadrao();

        // Assert
        calculadora.ObterTodasTabelas().Should().HaveCount(2);
    }

    #endregion

    #region Seleção de Tabela por Vigência

    [Fact]
    public void ObterTabelaVigente_Competencia2024_DeveRetornarTabela2024()
    {
        // Arrange
        var calculadora = CalculadoraInss.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2024, 6);

        // Act
        var tabela = calculadora.ObterTabelaVigente(competencia);

        // Assert
        tabela.Identificador.Should().Be("INSS-2024");
    }

    [Fact]
    public void ObterTabelaVigente_Competencia2025_DeveRetornarTabela2025()
    {
        // Arrange
        var calculadora = CalculadoraInss.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act
        var tabela = calculadora.ObterTabelaVigente(competencia);

        // Assert
        tabela.Identificador.Should().Be("INSS-2025");
    }

    [Fact]
    public void ObterTabelaVigente_CompetenciaSemTabela_DeveLancarExcecao()
    {
        // Arrange
        var calculadora = CalculadoraInss.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2020, 1); // Antes de qualquer tabela

        // Act
        var acao = () => calculadora.ObterTabelaVigente(competencia);

        // Assert
        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*Não há tabela de INSS vigente*");
    }

    [Fact]
    public void ExisteTabelaVigente_ComTabelaExistente_DeveRetornarTrue()
    {
        // Arrange
        var calculadora = CalculadoraInss.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act & Assert
        calculadora.ExisteTabelaVigente(competencia).Should().BeTrue();
    }

    [Fact]
    public void ExisteTabelaVigente_SemTabelaExistente_DeveRetornarFalse()
    {
        // Arrange
        var calculadora = CalculadoraInss.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2020, 1);

        // Act & Assert
        calculadora.ExisteTabelaVigente(competencia).Should().BeFalse();
    }

    #endregion

    #region Cálculo com Vigência

    [Fact]
    public void Calcular_Competencia2024_DeveUsarValores2024()
    {
        // Arrange
        var calculadora = CalculadoraInss.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2024, 6);
        var salario = Dinheiro.DeDecimal(1000m);

        // Act
        var resultado = calculadora.Calcular(salario, competencia);

        // Assert
        resultado.TabelaUtilizada.Should().Be("INSS-2024");
        // 1000 * 7.5% = 75.00
        resultado.ValorInss.Valor.Should().Be(75.00m);
    }

    [Fact]
    public void Calcular_Competencia2025_DeveUsarValores2025()
    {
        // Arrange
        var calculadora = CalculadoraInss.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2025, 3);
        var salario = Dinheiro.DeDecimal(1000m);

        // Act
        var resultado = calculadora.Calcular(salario, competencia);

        // Assert
        resultado.TabelaUtilizada.Should().Be("INSS-2025");
        // 1000 * 7.5% = 75.00
        resultado.ValorInss.Valor.Should().Be(75.00m);
    }

    [Fact]
    public void Calcular_MesmoSalarioTabelasDiferentes_DeveResultarValoresDiferentes()
    {
        // Arrange
        var calculadora = CalculadoraInss.CriarComTabelasPadrao();
        // Salário que está na segunda faixa em ambas tabelas, mas com limites diferentes
        var salario = Dinheiro.DeDecimal(2000m);
        var competencia2024 = Competencia.DeAnoMes(2024, 6);
        var competencia2025 = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado2024 = calculadora.Calcular(salario, competencia2024);
        var resultado2025 = calculadora.Calcular(salario, competencia2025);

        // Assert
        // Os valores devem ser diferentes porque os limites das faixas são diferentes
        // 2024: Faixa 1 até 1412, Faixa 2 até 2666.68
        // 2025: Faixa 1 até 1518, Faixa 2 até 2793.88
        resultado2024.ValorInss.Valor.Should().NotBe(resultado2025.ValorInss.Valor);
    }

    #endregion

    #region Determinismo

    [Fact]
    public void Calcular_MesmasEntradas_DeveRetornarMesmoResultado()
    {
        // Arrange
        var calculadora = CalculadoraInss.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var salario = Dinheiro.DeDecimal(3500m);

        // Act
        var resultado1 = calculadora.Calcular(salario, competencia);
        var resultado2 = calculadora.Calcular(salario, competencia);
        var resultado3 = calculadora.Calcular(salario, competencia);

        // Assert - Determinismo: sempre o mesmo resultado
        resultado1.ValorInss.Should().Be(resultado2.ValorInss);
        resultado2.ValorInss.Should().Be(resultado3.ValorInss);
    }

    #endregion

    #region Validações

    [Fact]
    public void Calcular_SalarioNulo_DeveLancarExcecao()
    {
        // Arrange
        var calculadora = CalculadoraInss.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var acao = () => calculadora.Calcular(null!, competencia);

        // Assert
        acao.Should().Throw<ArgumentNullException>()
            .WithParameterName("salarioBruto");
    }

    [Fact]
    public void Calcular_CompetenciaNula_DeveLancarExcecao()
    {
        // Arrange
        var calculadora = CalculadoraInss.CriarComTabelasPadrao();
        var salario = Dinheiro.DeDecimal(3000m);

        // Act
        var acao = () => calculadora.Calcular(salario, null!);

        // Assert
        acao.Should().Throw<ArgumentNullException>()
            .WithParameterName("competencia");
    }

    #endregion
}
