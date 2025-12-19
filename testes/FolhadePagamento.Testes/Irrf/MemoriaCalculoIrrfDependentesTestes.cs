using FluentAssertions;
using FolhadePagamento.Dominio.Irrf;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Irrf;

/// <summary>
/// Testes específicos para verificar a memória de cálculo
/// do IRRF com dependentes.
/// </summary>
public class MemoriaCalculoIrrfDependentesTestes
{
    #region Rastreabilidade da Dedução

    [Fact]
    public void Calcular_ComDependentes_DeveRegistrarValorUnitario()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(5000m);
        int dependentes = 2;

        // Act
        var resultado = tabela.Calcular(baseCalculo, dependentes);

        // Assert
        resultado.ValorUnitarioPorDependente.Valor.Should().Be(189.59m);
    }

    [Fact]
    public void Calcular_ComDependentes_DeveCalcularDeducaoCorreta()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(5000m);
        int dependentes = 3;

        // Act
        var resultado = tabela.Calcular(baseCalculo, dependentes);

        // Assert
        // 3 dependentes × R$ 189,59 = R$ 568,77
        resultado.DeducaoPorDependentes.Valor.Should().Be(568.77m);
    }

    [Fact]
    public void Calcular_ComDependentes_DeveCalcularBaseAjustadaCorreta()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(5000m);
        int dependentes = 2;

        // Act
        var resultado = tabela.Calcular(baseCalculo, dependentes);

        // Assert
        // Base ajustada = 5000 - (2 × 189.59) = 5000 - 379.18 = 4620.82
        resultado.BaseAjustada.Valor.Should().Be(4620.82m);
    }

    [Fact]
    public void Calcular_SemDependentes_ValorUnitarioDeveSerZeroMasRegistrado()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(5000m);

        // Act
        var resultado = tabela.Calcular(baseCalculo, 0);

        // Assert
        resultado.NumeroDependentes.Should().Be(0);
        resultado.ValorUnitarioPorDependente.Valor.Should().Be(189.59m); // Registrado mesmo sem uso
        resultado.DeducaoPorDependentes.Valor.Should().Be(0);
    }

    [Fact]
    public void Calcular_DeveManterNumeroDependentes()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(5000m);

        // Act
        var resultado = tabela.Calcular(baseCalculo, 4);

        // Assert
        resultado.NumeroDependentes.Should().Be(4);
    }

    #endregion

    #region Impacto no Cálculo Final

    [Fact]
    public void Calcular_DependentesReduzBaseComImpactoEmFaixa()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        // Base que cairia na faixa 22.5% sem dependentes
        var baseCalculo = Dinheiro.DeDecimal(4000m);

        // Act
        var resultadoSemDep = tabela.Calcular(baseCalculo, 0);
        var resultadoComDep = tabela.Calcular(baseCalculo, 3); // Deduz 568.77

        // Assert
        // Sem dependentes: 4000 está na faixa 22.5%
        // Com 3 dep: 4000 - 568.77 = 3431.23 cai na faixa 15%
        resultadoSemDep.AliquotaEfetiva.Should().Be(22.5m);
        resultadoComDep.AliquotaEfetiva.Should().Be(15m);
    }

    [Fact]
    public void Calcular_MuitosDependentes_PodeGerarIsencao()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(2500m);
        // 2 dependentes × 189.59 = 379.18
        // Base ajustada = 2500 - 379.18 = 2120.82 (abaixo de 2259.20 - ISENTO)

        // Act
        var resultado = tabela.Calcular(baseCalculo, 2);

        // Assert
        resultado.EhIsento.Should().BeTrue();
        resultado.ValorIrrf.Valor.Should().Be(0);
    }

    [Fact]
    public void Calcular_BaseOriginalDeveSerMantida()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(6000m);

        // Act
        var resultado = tabela.Calcular(baseCalculo, 5);

        // Assert - Base original não é afetada
        resultado.BaseOriginal.Should().Be(baseCalculo);
        resultado.BaseOriginal.Valor.Should().Be(6000m);
    }

    #endregion

    #region Cenários de Borda

    [Fact]
    public void Calcular_DeducaoMaiorQueBase_BaseAjustadaDeveSerZero()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(500m); // Base baixa
        int dependentes = 5; // 5 × 189.59 = 947.95 > 500

        // Act
        var resultado = tabela.Calcular(baseCalculo, dependentes);

        // Assert
        resultado.BaseAjustada.Valor.Should().Be(0);
        resultado.EhIsento.Should().BeTrue();
    }

    [Fact]
    public void Calcular_UmDependente_DeveCalcularCorretamente()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(3000m);

        // Act
        var resultado = tabela.Calcular(baseCalculo, 1);

        // Assert
        // Base ajustada = 3000 - 189.59 = 2810.41
        resultado.BaseAjustada.Valor.Should().Be(2810.41m);
        resultado.DeducaoPorDependentes.Valor.Should().Be(189.59m);
    }

    #endregion

    #region ToString com Dependentes

    [Fact]
    public void ToString_ComDependentes_DeveIncluirDetalhamento()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(5000m);

        // Act
        var resultado = tabela.Calcular(baseCalculo, 2);
        var texto = resultado.ToString();

        // Assert
        texto.Should().Contain("Dependentes: 2");
        texto.Should().Contain("189,59");
        texto.Should().Contain("379,18");
    }

    [Fact]
    public void ToString_SemDependentes_NaoDeveIncluirDependentes()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(5000m);

        // Act
        var resultado = tabela.Calcular(baseCalculo, 0);
        var texto = resultado.ToString();

        // Assert
        texto.Should().NotContain("Dependentes:");
    }

    #endregion

    #region Determinismo

    [Fact]
    public void Calcular_MesmasEntradasComDependentes_DeveRetornarMesmoResultado()
    {
        // Arrange
        var tabela = TabelaIrrf.CriarTabela2025();
        var baseCalculo = Dinheiro.DeDecimal(5000m);
        int dependentes = 2;

        // Act
        var resultado1 = tabela.Calcular(baseCalculo, dependentes);
        var resultado2 = tabela.Calcular(baseCalculo, dependentes);
        var resultado3 = tabela.Calcular(baseCalculo, dependentes);

        // Assert - DETERMINISMO
        resultado1.ValorIrrf.Should().Be(resultado2.ValorIrrf);
        resultado2.ValorIrrf.Should().Be(resultado3.ValorIrrf);
        resultado1.DeducaoPorDependentes.Should().Be(resultado2.DeducaoPorDependentes);
        resultado1.BaseAjustada.Should().Be(resultado2.BaseAjustada);
    }

    #endregion

    #region Integração com CalculadoraIrrf

    [Fact]
    public void CalculadoraIrrf_ComDependentes_DevePassarParaTabela()
    {
        // Arrange
        var calculadora = CalculadoraIrrf.CriarComTabelasPadrao();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var baseCalculo = Dinheiro.DeDecimal(5000m);
        int dependentes = 2;

        // Act
        var resultado = calculadora.Calcular(baseCalculo, competencia, dependentes);

        // Assert
        resultado.NumeroDependentes.Should().Be(2);
        resultado.ValorUnitarioPorDependente.Valor.Should().Be(189.59m);
        resultado.DeducaoPorDependentes.Valor.Should().Be(379.18m);
    }

    #endregion
}
