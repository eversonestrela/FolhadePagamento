using FluentAssertions;
using FolhadePagamento.Dominio.Entidades;
using FolhadePagamento.Dominio.Fgts;
using FolhadePagamento.Dominio.Folha;
using FolhadePagamento.Dominio.Inss;
using FolhadePagamento.Dominio.Irrf;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Folha;

/// <summary>
/// Testes de integração da CalculadoraFolha com FGTS.
/// 
/// IMPORTANTE: FGTS é encargo PATRONAL e NÃO impacta salário líquido.
/// </summary>
public class CalculadoraFolhaComFgtsTestes
{
    private readonly DateTime _timestampFixo = new(2025, 3, 15, 10, 0, 0);

    private static Funcionario CriarFuncionario(string nome, decimal salarioBase)
    {
        var id = FuncionarioId.Novo();
        return Funcionario.Criar(id, nome, Dinheiro.DeDecimal(salarioBase));
    }

    #region Retrocompatibilidade

    [Fact]
    public void Calcular_SemCalculadoraFgts_DeveTerFgtsZero()
    {
        // Arrange - Sem FGTS
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf);
        var funcionario = CriarFuncionario("Maria da Silva", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.ValorFgts.Valor.Should().Be(0m);
        resultado.DetalheFgts.Should().BeNull();
    }

    #endregion

    #region Cálculo Básico

    [Fact]
    public void Calcular_ComFgts_DeveCalcular8Porcento()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadoraFgts = CalculadoraFgts.CriarComTabelaPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf, calculadoraFgts);
        var funcionario = CriarFuncionario("João Silva", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        // FGTS = 5000 × 8% = 400
        resultado.ValorFgts.Valor.Should().Be(400m);
        resultado.DetalheFgts.Should().NotBeNull();
        resultado.DetalheFgts!.AliquotaAplicada.Should().Be(8m);
    }

    [Fact]
    public void Calcular_Aprendiz_DeveCalcular2Porcento()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadoraFgts = CalculadoraFgts.CriarComTabelaPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf, calculadoraFgts);
        var funcionario = CriarFuncionario("Jovem Aprendiz", 1500m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo, ehAprendiz: true);

        // Assert
        // FGTS = 1500 × 2% = 30
        resultado.ValorFgts.Valor.Should().Be(30m);
        resultado.DetalheFgts!.EhAprendiz.Should().BeTrue();
    }

    #endregion

    #region FGTS Não Impacta Líquido

    [Fact]
    public void Calcular_FgtsNaoDeveImpactarSalarioLiquido()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadoraFgts = CalculadoraFgts.CriarComTabelaPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf, calculadoraFgts);
        var funcionario = CriarFuncionario("Teste FGTS", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        // Líquido = Bruto - INSS - IRRF (FGTS NÃO desconta)
        var liquidoEsperado = resultado.SalarioBruto
            .Subtrair(resultado.ValorInss)
            .Subtrair(resultado.ValorIrrf);

        resultado.SalarioLiquido.Should().Be(liquidoEsperado);

        // FGTS não deve estar nos descontos
        resultado.TotalDescontos.Valor.Should().Be(
            resultado.ValorInss.Valor + resultado.ValorIrrf.Valor);
    }

    [Fact]
    public void Calcular_FgtsDeveEstarNosEncargosPatronais()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadoraFgts = CalculadoraFgts.CriarComTabelaPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf, calculadoraFgts);
        var funcionario = CriarFuncionario("Teste Encargos", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.TotalEncargosPatronais.Should().Be(resultado.ValorFgts);
    }

    #endregion

    #region Custo Total do Empregador

    [Fact]
    public void Calcular_CustoTotalEmpregadorDeveIncluirFgts()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadoraFgts = CalculadoraFgts.CriarComTabelaPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf, calculadoraFgts);
        var funcionario = CriarFuncionario("Teste Custo", 5000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        // Custo = Bruto + Encargos Patronais
        var custoEsperado = resultado.SalarioBruto.Somar(resultado.TotalEncargosPatronais);
        resultado.CustoTotalEmpregador.Should().Be(custoEsperado);

        // 5000 + 400 (FGTS 8%) = 5400
        resultado.CustoTotalEmpregador.Valor.Should().Be(5400m);
    }

    [Theory]
    [InlineData(1000, 80, 1080)]      // 1000 + 8% = 1080
    [InlineData(3000, 240, 3240)]     // 3000 + 8% = 3240
    [InlineData(5000, 400, 5400)]     // 5000 + 8% = 5400
    [InlineData(10000, 800, 10800)]   // 10000 + 8% = 10800
    public void Calcular_CustoTotalEmpregador_DiversosSalarios(
        decimal salario, decimal fgtsEsperado, decimal custoEsperado)
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadoraFgts = CalculadoraFgts.CriarComTabelaPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf, calculadoraFgts);
        var funcionario = CriarFuncionario("Teste", salario);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert
        resultado.ValorFgts.Valor.Should().Be(fgtsEsperado);
        resultado.CustoTotalEmpregador.Valor.Should().Be(custoEsperado);
    }

    #endregion

    #region Pipeline Correto

    [Fact]
    public void Calcular_FgtsDeveSerCalculadoAposIrrf()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadoraFgts = CalculadoraFgts.CriarComTabelaPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf, calculadoraFgts);
        var funcionario = CriarFuncionario("Teste Pipeline", 8000m);
        var competencia = Competencia.DeAnoMes(2025, 3);

        // Act
        var resultado = calculadora.Calcular(funcionario, competencia, _timestampFixo);

        // Assert - Pipeline: INSS → IRRF → FGTS
        resultado.DetalheInss.Should().NotBeNull();
        resultado.DetalheIrrf.Should().NotBeNull();
        resultado.DetalheFgts.Should().NotBeNull();

        // Base do FGTS é o salário bruto (não é afetado por INSS/IRRF)
        resultado.DetalheFgts!.BaseCalculo.Should().Be(resultado.SalarioBruto);
    }

    #endregion

    #region Determinismo

    [Fact]
    public void Calcular_MesmasEntradas_DeveRetornarMesmoResultado()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadoraFgts = CalculadoraFgts.CriarComTabelaPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf, calculadoraFgts);
        var funcionario = CriarFuncionario("Teste Determinismo", 6500m);
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act
        var resultado1 = calculadora.Calcular(funcionario, competencia, _timestampFixo, 1);
        var resultado2 = calculadora.Calcular(funcionario, competencia, _timestampFixo, 1);
        var resultado3 = calculadora.Calcular(funcionario, competencia, _timestampFixo, 1);

        // Assert - DETERMINISMO
        resultado1.ValorFgts.Should().Be(resultado2.ValorFgts);
        resultado2.ValorFgts.Should().Be(resultado3.ValorFgts);
        resultado1.CustoTotalEmpregador.Should().Be(resultado2.CustoTotalEmpregador);
    }

    #endregion

    #region Estrutura do Resultado

    [Fact]
    public void Calcular_DeveManterEstruturaCorreta()
    {
        // Arrange
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadoraFgts = CalculadoraFgts.CriarComTabelaPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf, calculadoraFgts);
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
        resultado.ValorFgts.Valor.Should().BeGreaterThan(0);

        // Descontos NÃO incluem FGTS
        resultado.TotalDescontos.Should().Be(resultado.ValorInss.Somar(resultado.ValorIrrf));

        // Encargos patronais incluem FGTS
        resultado.TotalEncargosPatronais.Should().Be(resultado.ValorFgts);

        // Custo empregador = Bruto + Encargos
        resultado.CustoTotalEmpregador.Should().Be(
            resultado.SalarioBruto.Somar(resultado.TotalEncargosPatronais));

        resultado.CalculadoEm.Should().Be(_timestampFixo);
    }

    #endregion
}
