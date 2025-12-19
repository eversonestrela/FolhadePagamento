using FluentAssertions;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Dominio;

/// <summary>
/// Testes unitários para o Value Object Vigencia.
/// Valida comportamentos de vigência válida, inválida, indefinida e expirada.
/// </summary>
public class VigenciaTestes
{
    #region Criação

    [Fact]
    public void Criar_DeveCriarVigenciaComDataInicioEFim()
    {
        // Arrange
        var dataInicio = new DateTime(2025, 1, 1);
        var dataFim = new DateTime(2025, 12, 31);

        // Act
        var vigencia = Vigencia.Criar(dataInicio, dataFim);

        // Assert
        vigencia.DataInicio.Should().Be(dataInicio);
        vigencia.DataFim.Should().Be(dataFim);
        vigencia.EhIndefinida.Should().BeFalse();
    }

    [Fact]
    public void Criar_DeveCriarVigenciaIndefinida_QuandoSemDataFim()
    {
        // Arrange
        var dataInicio = new DateTime(2025, 1, 1);

        // Act
        var vigencia = Vigencia.Criar(dataInicio);

        // Assert
        vigencia.DataInicio.Should().Be(dataInicio);
        vigencia.DataFim.Should().BeNull();
        vigencia.EhIndefinida.Should().BeTrue();
    }

    [Fact]
    public void Indefinida_DeveCriarVigenciaSemDataFim()
    {
        // Arrange
        var dataInicio = new DateTime(2025, 6, 1);

        // Act
        var vigencia = Vigencia.Indefinida(dataInicio);

        // Assert
        vigencia.EhIndefinida.Should().BeTrue();
        vigencia.DataFim.Should().BeNull();
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoDataFimAnteriorADataInicio()
    {
        // Arrange
        var dataInicio = new DateTime(2025, 6, 1);
        var dataFim = new DateTime(2025, 1, 1); // Anterior!

        // Act & Assert
        var acao = () => Vigencia.Criar(dataInicio, dataFim);
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*anterior*");
    }

    [Fact]
    public void Criar_DeveIgnorarComponenteHora()
    {
        // Arrange
        var dataInicioComHora = new DateTime(2025, 1, 1, 14, 30, 45);
        var dataFimComHora = new DateTime(2025, 12, 31, 23, 59, 59);

        // Act
        var vigencia = Vigencia.Criar(dataInicioComHora, dataFimComHora);

        // Assert
        vigencia.DataInicio.Should().Be(new DateTime(2025, 1, 1));
        vigencia.DataFim.Should().Be(new DateTime(2025, 12, 31));
    }

    #endregion

    #region EstaVigenteEm (Data Específica)

    [Fact]
    public void EstaVigenteEm_DeveRetornarTrue_QuandoDataDentroDoPeríodo()
    {
        // Arrange
        var vigencia = Vigencia.Criar(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 12, 31));
        var dataVerificar = new DateTime(2025, 6, 15);

        // Act
        var resultado = vigencia.EstaVigenteEm(dataVerificar);

        // Assert
        resultado.Should().BeTrue();
    }

    [Fact]
    public void EstaVigenteEm_DeveRetornarTrue_QuandoDataIgualADataInicio()
    {
        // Arrange
        var dataInicio = new DateTime(2025, 1, 1);
        var vigencia = Vigencia.Criar(dataInicio, new DateTime(2025, 12, 31));

        // Act
        var resultado = vigencia.EstaVigenteEm(dataInicio);

        // Assert
        resultado.Should().BeTrue();
    }

    [Fact]
    public void EstaVigenteEm_DeveRetornarTrue_QuandoDataIgualADataFim()
    {
        // Arrange
        var dataFim = new DateTime(2025, 12, 31);
        var vigencia = Vigencia.Criar(new DateTime(2025, 1, 1), dataFim);

        // Act
        var resultado = vigencia.EstaVigenteEm(dataFim);

        // Assert
        resultado.Should().BeTrue();
    }

    [Fact]
    public void EstaVigenteEm_DeveRetornarFalse_QuandoDataAnteriorAoInicio()
    {
        // Arrange
        var vigencia = Vigencia.Criar(
            new DateTime(2025, 6, 1),
            new DateTime(2025, 12, 31));
        var dataAnterior = new DateTime(2025, 5, 31);

        // Act
        var resultado = vigencia.EstaVigenteEm(dataAnterior);

        // Assert
        resultado.Should().BeFalse();
    }

    [Fact]
    public void EstaVigenteEm_DeveRetornarFalse_QuandoDataPosteriorAoFim()
    {
        // Arrange
        var vigencia = Vigencia.Criar(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 6, 30));
        var dataPosterior = new DateTime(2025, 7, 1);

        // Act
        var resultado = vigencia.EstaVigenteEm(dataPosterior);

        // Assert
        resultado.Should().BeFalse();
    }

    [Fact]
    public void EstaVigenteEm_DeveRetornarTrue_QuandoVigenciaIndefinidaEDataFutura()
    {
        // Arrange
        var vigencia = Vigencia.Indefinida(new DateTime(2025, 1, 1));
        var dataFutura = new DateTime(2099, 12, 31);

        // Act
        var resultado = vigencia.EstaVigenteEm(dataFutura);

        // Assert
        resultado.Should().BeTrue();
    }

    #endregion

    #region EstaVigenteParaCompetencia

    [Fact]
    public void EstaVigenteParaCompetencia_DeveRetornarTrue_QuandoCompetenciaDentroDoPeríodo()
    {
        // Arrange
        var vigencia = Vigencia.Criar(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 12, 31));
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act
        var resultado = vigencia.EstaVigenteParaCompetencia(competencia);

        // Assert
        resultado.Should().BeTrue();
    }

    [Fact]
    public void EstaVigenteParaCompetencia_DeveRetornarTrue_QuandoVigenciaComecaNoMeioDaCompetencia()
    {
        // Arrange - Vigência começa em 15/06/2025
        var vigencia = Vigencia.Criar(
            new DateTime(2025, 6, 15),
            new DateTime(2025, 12, 31));
        var competencia = Competencia.DeAnoMes(2025, 6); // Jun/2025

        // Act
        var resultado = vigencia.EstaVigenteParaCompetencia(competencia);

        // Assert - Válida porque cobre parte do mês
        resultado.Should().BeTrue();
    }

    [Fact]
    public void EstaVigenteParaCompetencia_DeveRetornarTrue_QuandoVigenciaTerminaNoMeioDaCompetencia()
    {
        // Arrange - Vigência termina em 15/06/2025
        var vigencia = Vigencia.Criar(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 6, 15));
        var competencia = Competencia.DeAnoMes(2025, 6); // Jun/2025

        // Act
        var resultado = vigencia.EstaVigenteParaCompetencia(competencia);

        // Assert - Válida porque cobre parte do mês
        resultado.Should().BeTrue();
    }

    [Fact]
    public void EstaVigenteParaCompetencia_DeveRetornarFalse_QuandoCompetenciaAntesDaVigencia()
    {
        // Arrange - Vigência começa em Abr/2025
        var vigencia = Vigencia.Criar(
            new DateTime(2025, 4, 1),
            new DateTime(2025, 12, 31));
        var competencia = Competencia.DeAnoMes(2025, 3); // Mar/2025

        // Act
        var resultado = vigencia.EstaVigenteParaCompetencia(competencia);

        // Assert
        resultado.Should().BeFalse();
    }

    [Fact]
    public void EstaVigenteParaCompetencia_DeveRetornarFalse_QuandoCompetenciaDepoisDaVigencia()
    {
        // Arrange - Vigência termina em Mar/2025
        var vigencia = Vigencia.Criar(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 3, 31));
        var competencia = Competencia.DeAnoMes(2025, 4); // Abr/2025

        // Act
        var resultado = vigencia.EstaVigenteParaCompetencia(competencia);

        // Assert
        resultado.Should().BeFalse();
    }

    [Fact]
    public void EstaVigenteParaCompetencia_DeveRetornarTrue_QuandoVigenciaIndefinida()
    {
        // Arrange
        var vigencia = Vigencia.Indefinida(new DateTime(2025, 1, 1));
        var competenciaFutura = Competencia.DeAnoMes(2030, 12);

        // Act
        var resultado = vigencia.EstaVigenteParaCompetencia(competenciaFutura);

        // Assert
        resultado.Should().BeTrue();
    }

    [Fact]
    public void EstaVigenteParaCompetencia_DeveLancarExcecao_QuandoCompetenciaNula()
    {
        // Arrange
        var vigencia = Vigencia.Criar(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        // Act & Assert
        var acao = () => vigencia.EstaVigenteParaCompetencia(null!);
        acao.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Estados da Vigência

    [Fact]
    public void EstaExpiradaEm_DeveRetornarTrue_QuandoDataPosteriorAoFim()
    {
        // Arrange
        var vigencia = Vigencia.Criar(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 6, 30));
        var dataVerificar = new DateTime(2025, 7, 1);

        // Act
        var resultado = vigencia.EstaExpiradaEm(dataVerificar);

        // Assert
        resultado.Should().BeTrue();
    }

    [Fact]
    public void EstaExpiradaEm_DeveRetornarFalse_QuandoVigenciaIndefinida()
    {
        // Arrange
        var vigencia = Vigencia.Indefinida(new DateTime(2025, 1, 1));
        var dataFutura = new DateTime(2099, 12, 31);

        // Act
        var resultado = vigencia.EstaExpiradaEm(dataFutura);

        // Assert
        resultado.Should().BeFalse();
    }

    [Fact]
    public void AindaNaoIniciouEm_DeveRetornarTrue_QuandoDataAnteriorAoInicio()
    {
        // Arrange
        var vigencia = Vigencia.Criar(
            new DateTime(2025, 6, 1),
            new DateTime(2025, 12, 31));
        var dataAnterior = new DateTime(2025, 5, 31);

        // Act
        var resultado = vigencia.AindaNaoIniciouEm(dataAnterior);

        // Assert
        resultado.Should().BeTrue();
    }

    [Fact]
    public void AindaNaoIniciouEm_DeveRetornarFalse_QuandoDataIgualAoInicio()
    {
        // Arrange
        var dataInicio = new DateTime(2025, 6, 1);
        var vigencia = Vigencia.Criar(dataInicio, new DateTime(2025, 12, 31));

        // Act
        var resultado = vigencia.AindaNaoIniciouEm(dataInicio);

        // Assert
        resultado.Should().BeFalse();
    }

    #endregion

    #region Igualdade

    [Fact]
    public void Vigencias_DevemSerIguais_QuandoMesmasDatas()
    {
        // Arrange
        var vigencia1 = Vigencia.Criar(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));
        var vigencia2 = Vigencia.Criar(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        // Assert
        vigencia1.Should().Be(vigencia2);
        (vigencia1 == vigencia2).Should().BeTrue();
    }

    [Fact]
    public void Vigencias_DevemSerDiferentes_QuandoDatasDiferentes()
    {
        // Arrange
        var vigencia1 = Vigencia.Criar(new DateTime(2025, 1, 1), new DateTime(2025, 6, 30));
        var vigencia2 = Vigencia.Criar(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        // Assert
        vigencia1.Should().NotBe(vigencia2);
        (vigencia1 != vigencia2).Should().BeTrue();
    }

    [Fact]
    public void VigenciasIndefinidas_DevemSerIguais_QuandoMesmaDataInicio()
    {
        // Arrange
        var vigencia1 = Vigencia.Indefinida(new DateTime(2025, 1, 1));
        var vigencia2 = Vigencia.Indefinida(new DateTime(2025, 1, 1));

        // Assert
        vigencia1.Should().Be(vigencia2);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_DeveFormatarCorretamente_QuandoVigenciaDefinida()
    {
        // Arrange
        var vigencia = Vigencia.Criar(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        // Act
        var texto = vigencia.ToString();

        // Assert
        texto.Should().Be("01/01/2025 a 31/12/2025");
    }

    [Fact]
    public void ToString_DeveIndicarIndefinida_QuandoSemDataFim()
    {
        // Arrange
        var vigencia = Vigencia.Indefinida(new DateTime(2025, 6, 1));

        // Act
        var texto = vigencia.ToString();

        // Assert
        texto.Should().Contain("indefinida");
    }

    #endregion

    #region Cenários Reais de Folha de Pagamento

    [Fact]
    public void CenarioReal_TabelaIRRFMudouEmAbril_DeveRetornarVigenciaCorreta()
    {
        // Arrange - Tabela IRRF antiga vigente até 31/03/2025
        var vigenciaTabelaAntiga = Vigencia.Criar(
            new DateTime(2024, 1, 1),
            new DateTime(2025, 3, 31));

        // Arrange - Tabela IRRF nova vigente a partir de 01/04/2025
        var vigenciaTabelaNova = Vigencia.Indefinida(new DateTime(2025, 4, 1));

        var competenciaMarco = Competencia.DeAnoMes(2025, 3);
        var competenciaAbril = Competencia.DeAnoMes(2025, 4);

        // Act & Assert
        vigenciaTabelaAntiga.EstaVigenteParaCompetencia(competenciaMarco).Should().BeTrue();
        vigenciaTabelaAntiga.EstaVigenteParaCompetencia(competenciaAbril).Should().BeFalse();

        vigenciaTabelaNova.EstaVigenteParaCompetencia(competenciaMarco).Should().BeFalse();
        vigenciaTabelaNova.EstaVigenteParaCompetencia(competenciaAbril).Should().BeTrue();
    }

    [Fact]
    public void CenarioReal_RubricaTemporaria_DeveValidarPeriodoCorreto()
    {
        // Arrange - Rubrica de bônus especial válida apenas em Dez/2025
        var vigenciaBonus = Vigencia.Criar(
            new DateTime(2025, 12, 1),
            new DateTime(2025, 12, 31));

        var competenciaNovembro = Competencia.DeAnoMes(2025, 11);
        var competenciaDezembro = Competencia.DeAnoMes(2025, 12);
        var competenciaJaneiro2026 = Competencia.DeAnoMes(2026, 1);

        // Act & Assert
        vigenciaBonus.EstaVigenteParaCompetencia(competenciaNovembro).Should().BeFalse();
        vigenciaBonus.EstaVigenteParaCompetencia(competenciaDezembro).Should().BeTrue();
        vigenciaBonus.EstaVigenteParaCompetencia(competenciaJaneiro2026).Should().BeFalse();
    }

    #endregion
}
