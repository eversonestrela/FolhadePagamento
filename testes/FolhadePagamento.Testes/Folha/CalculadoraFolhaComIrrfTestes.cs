using FluentAssertions;
using FolhadePagamento.Dominio.Entidades;
using FolhadePagamento.Dominio.Folha;
using FolhadePagamento.Dominio.Inss;
using FolhadePagamento.Dominio.Irrf;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Folha;

/// <summary>
/// Testes de integração da CalculadoraFolha com IRRF.
/// </summary>
public class CalculadoraFolhaComIrrfTestes
{
    private readonly DateTime _timestampFixo = new(2025, 3, 15, 10, 0, 0);

    private static Funcionario CriarFuncionario(string nome, decimal salarioBase)
    {
        var id = FuncionarioId.Novo();
        return Funcionario.Criar(id, nome, Dinheiro.DeDecimal(salarioBase));
    }

    #region Retrocompatibilidade

    [Fact]
    public void Calcular_SemCalculadoraIrrf_DeveCalcularSemDescontoIrrf()
    {
        // Arrange - Apenas INSS, sem IRRF
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss);
        var funcionario = CriarFuncionario("Maria da Silva", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.ValorIrrf.Valor.Should().Be(0m);
        resultado.DetalheIrrf.Should().BeNull();
        // INSS deve estar calculado
        resultado.ValorInss.Valor.Should().BeGreaterThan(0);
    }

    #endregion

    #region Integração INSS + IRRF

    [Fact]
    public void Calcular_ComInssEIrrf_DeveCalcularAmbos()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf);
        var funcionario = CriarFuncionario("João Silva", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.ValorInss.Valor.Should().BeGreaterThan(0);
        resultado.DetalheInss.Should().NotBeNull();
        resultado.ValorIrrf.Valor.Should().BeGreaterThan(0);
        resultado.DetalheIrrf.Should().NotBeNull();
    }

    [Fact]
    public void Calcular_BaseIrrfDeveSerSalarioBrutoMenosInss()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf);
        var funcionario = CriarFuncionario("Ana Costa", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        // Base IRRF = Bruto - INSS
        var baseEsperada = resultado.SalarioBruto.Subtrair(resultado.ValorInss);
        resultado.DetalheIrrf!.BaseOriginal.Should().Be(baseEsperada);
    }

    [Fact]
    public void Calcular_TotalDescontosDeveIncluirInssEIrrf()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf);
        var funcionario = CriarFuncionario("Pedro Souza", 6000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        var totalEsperado = resultado.ValorInss.Somar(resultado.ValorIrrf);
        resultado.TotalDescontos.Should().Be(totalEsperado);
    }

    [Fact]
    public void Calcular_SalarioLiquidoDeveSerBrutoMenosDescontos()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf);
        var funcionario = CriarFuncionario("Lucas Mendes", 7000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        var liquidoEsperado = resultado.SalarioBruto.Subtrair(resultado.TotalDescontos);
        resultado.SalarioLiquido.Should().Be(liquidoEsperado);
    }

    #endregion

    #region Faixa de Isenção

    [Fact]
    public void Calcular_SalarioBaixo_DeveSerIsentoDeIrrf()
    {
        // Arrange - Salário que gera base IRRF na faixa isenta
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf);
        var funcionario = CriarFuncionario("Funcionário Isento", 2000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.ValorIrrf.Valor.Should().Be(0);
        resultado.DetalheIrrf!.EhIsento.Should().BeTrue();
    }

    #endregion

    #region Dependentes

    [Fact]
    public void Calcular_ComDependentes_DeveReduzirBaseIrrf()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf);
        var funcionario = CriarFuncionario("Pai de Família", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultadoSemDependentes = calculadora.Calcular(funcionario, competencia, _timestampFixo, 0);
        var resultadoComDependentes = calculadora.Calcular(funcionario, competencia, _timestampFixo, 2);

        // Assert
        resultadoComDependentes.ValorIrrf.Valor.Should().BeLessThan(resultadoSemDependentes.ValorIrrf.Valor);
        resultadoComDependentes.DetalheIrrf!.NumeroDependentes.Should().Be(2);
    }

    [Fact]
    public void Calcular_MuitosDependentes_PodeZerarIrrf()
    {
        // Arrange - Salário médio com muitos dependentes
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf);
        var funcionario = CriarFuncionario("Família Grande", 3000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo, 5);

        // Assert
        // Com 5 dependentes (5 * 189.59 = 947.95), a base fica bem reduzida
        resultado.DetalheIrrf!.NumeroDependentes.Should().Be(5);
        resultado.ValorIrrf.Valor.Should().BeLessThanOrEqualTo(resultado.DetalheIrrf.BaseOriginal.Valor * 0.1m);
    }

    #endregion

    #region Vigência

    [Fact]
    public void Calcular_Competencia2024_DeveUsarTabelas2024()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf);
        var funcionario = CriarFuncionario("Funcionário 2024", 5000m);
        var competencia = Competencia.DeAnoMes(2024, 12);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.DetalheInss!.TabelaUtilizada.Should().Be("INSS-2024");
        resultado.DetalheIrrf!.TabelaUtilizada.Should().Be("IRRF-2024");
    }

    [Fact]
    public void Calcular_Competencia2025_DeveUsarTabelas2025()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf);
        var funcionario = CriarFuncionario("Funcionário 2025", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 1);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.DetalheInss!.TabelaUtilizada.Should().Be("INSS-2025");
        resultado.DetalheIrrf!.TabelaUtilizada.Should().Be("IRRF-2025");
    }

    [Fact]
    public void Calcular_CompetenciaSemTabelaIrrf_DeveCalcularSemIrrf()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf);
        var funcionario = CriarFuncionario("Funcionário Antigo", 5000m);
        var competenciaAntiga = Competencia.DeAnoMes(2020, 1);

        // Act
        var resultado = calculadora.Calcular(funcionario, competenciaAntiga, _timestampFixo);

        // Assert
        resultado.ValorIrrf.Valor.Should().Be(0m);
        resultado.DetalheIrrf.Should().BeNull();
    }

    #endregion

    #region Determinismo

    [Fact]
    public void Calcular_MesmasEntradas_DeveRetornarMesmoResultado()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf);
        var funcionario = CriarFuncionario("Teste Determinismo", 6500m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act
        var resultado1 = calculadora.Calcular(funcionario, competencia, _timestampFixo, 1);
        var resultado2 = calculadora.Calcular(funcionario, competencia, _timestampFixo, 1);
        var resultado3 = calculadora.Calcular(funcionario, competencia, _timestampFixo, 1);

        // Assert - DETERMINISMO
        resultado1.ValorInss.Should().Be(resultado2.ValorInss);
        resultado2.ValorInss.Should().Be(resultado3.ValorInss);
        resultado1.ValorIrrf.Should().Be(resultado2.ValorIrrf);
        resultado2.ValorIrrf.Should().Be(resultado3.ValorIrrf);
        resultado1.SalarioLiquido.Should().Be(resultado2.SalarioLiquido);
    }

    #endregion

    #region Pipeline Correto

    [Fact]
    public void Calcular_IrrfNaoDevePrecederInss()
    {
        // Arrange - Verifica que IRRF usa INSS já calculado
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf);
        var funcionario = CriarFuncionario("Ordem Pipeline", 8000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        // Base IRRF deve ser Bruto - INSS
        var baseIrrfEsperada = resultado.SalarioBruto.Subtrair(resultado.ValorInss);
        resultado.DetalheIrrf!.BaseOriginal.Should().Be(baseIrrfEsperada);

        // INSS não deve ser afetado pelo IRRF
        // Recalcular INSS isoladamente para confirmar
        var inssIsolado = calculadoraInss.Calcular(resultado.SalarioBruto, competencia);
        resultado.ValorInss.Should().Be(inssIsolado.ValorInss);
    }

    #endregion

    #region Estrutura do Resultado

    [Fact]
    public void Calcular_DeveManterEstruturaCorreta()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf);
        var funcionario = CriarFuncionario("Estrutura Teste", 7500m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo, 1);

        // Assert
        resultado.FuncionarioId.Should().Be(funcionario.Id);
        resultado.Competencia.Should().Be(competencia);
        resultado.SalarioBruto.Should().Be(funcionario.SalarioBase);
        resultado.ValorInss.Valor.Should().BeGreaterThan(0);
        resultado.ValorIrrf.Valor.Should().BeGreaterThan(0);
        resultado.TotalDescontos.Should().Be(resultado.ValorInss.Somar(resultado.ValorIrrf));
        resultado.SalarioLiquido.Should().Be(resultado.SalarioBruto.Subtrair(resultado.TotalDescontos));
        resultado.CalculadoEm.Should().Be(_timestampFixo);
    }

    #endregion
}
