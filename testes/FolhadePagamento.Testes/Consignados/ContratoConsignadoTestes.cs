using FluentAssertions;
using FolhadePagamento.Dominio.Consignados;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Consignados;

/// <summary>
/// Testes para o Value Object ContratoConsignado.
/// </summary>
public class ContratoConsignadoTestes
{
    private static Vigencia CriarVigenciaPadrao() =>
        Vigencia.Criar(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

    #region Criação

    [Fact]
    public void Criar_ComValoresValidos_DeveCriarContrato()
    {
        // Arrange & Act
        var contrato = ContratoConsignado.Criar(
            identificador: "CONS-001",
            descricao: "Empréstimo BB",
            valorParcela: Dinheiro.DeDecimal(500m),
            totalParcelas: 24,
            parcelaAtual: 1,
            vigencia: CriarVigenciaPadrao(),
            prioridade: 1);

        // Assert
        contrato.Identificador.Should().Be("CONS-001");
        contrato.Descricao.Should().Be("Empréstimo BB");
        contrato.ValorParcela.Valor.Should().Be(500m);
        contrato.TotalParcelas.Should().Be(24);
        contrato.ParcelaAtual.Should().Be(1);
        contrato.Prioridade.Should().Be(1);
    }

    [Fact]
    public void Criar_SemIdentificador_DeveLancarExcecao()
    {
        // Act
        var acao = () => ContratoConsignado.Criar(
            identificador: "",
            descricao: "Teste",
            valorParcela: Dinheiro.DeDecimal(100m),
            totalParcelas: 12,
            parcelaAtual: 1,
            vigencia: CriarVigenciaPadrao());

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*Identificador é obrigatório*");
    }

    [Fact]
    public void Criar_SemDescricao_DeveLancarExcecao()
    {
        // Act
        var acao = () => ContratoConsignado.Criar(
            identificador: "CONS-001",
            descricao: "",
            valorParcela: Dinheiro.DeDecimal(100m),
            totalParcelas: 12,
            parcelaAtual: 1,
            vigencia: CriarVigenciaPadrao());

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*Descrição é obrigatória*");
    }

    [Fact]
    public void Criar_ValorParcelaZero_DeveLancarExcecao()
    {
        // Act
        var acao = () => ContratoConsignado.Criar(
            identificador: "CONS-001",
            descricao: "Teste",
            valorParcela: Dinheiro.Zero,
            totalParcelas: 12,
            parcelaAtual: 1,
            vigencia: CriarVigenciaPadrao());

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("valorParcela");
    }

    [Fact]
    public void Criar_TotalParcelasZero_DeveLancarExcecao()
    {
        // Act
        var acao = () => ContratoConsignado.Criar(
            identificador: "CONS-001",
            descricao: "Teste",
            valorParcela: Dinheiro.DeDecimal(100m),
            totalParcelas: 0,
            parcelaAtual: 1,
            vigencia: CriarVigenciaPadrao());

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("totalParcelas");
    }

    [Fact]
    public void Criar_ParcelaAtualMaiorQueTotalParcelas_DeveLancarExcecao()
    {
        // Act
        var acao = () => ContratoConsignado.Criar(
            identificador: "CONS-001",
            descricao: "Teste",
            valorParcela: Dinheiro.DeDecimal(100m),
            totalParcelas: 12,
            parcelaAtual: 13,
            vigencia: CriarVigenciaPadrao());

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("parcelaAtual");
    }

    [Fact]
    public void Criar_PrioridadeZero_DeveLancarExcecao()
    {
        // Act
        var acao = () => ContratoConsignado.Criar(
            identificador: "CONS-001",
            descricao: "Teste",
            valorParcela: Dinheiro.DeDecimal(100m),
            totalParcelas: 12,
            parcelaAtual: 1,
            vigencia: CriarVigenciaPadrao(),
            prioridade: 0);

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("prioridade");
    }

    #endregion

    #region Vigência

    [Fact]
    public void EstaVigenteParaCompetencia_CompetenciaDentroVigencia_DeveRetornarTrue()
    {
        // Arrange
        var contrato = ContratoConsignado.Criar(
            identificador: "CONS-001",
            descricao: "Teste",
            valorParcela: Dinheiro.DeDecimal(100m),
            totalParcelas: 12,
            parcelaAtual: 1,
            vigencia: CriarVigenciaPadrao());
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act & Assert
        contrato.EstaVigenteParaCompetencia(competencia).Should().BeTrue();
    }

    [Fact]
    public void EstaVigenteParaCompetencia_CompetenciaAntesVigencia_DeveRetornarFalse()
    {
        // Arrange
        var contrato = ContratoConsignado.Criar(
            identificador: "CONS-001",
            descricao: "Teste",
            valorParcela: Dinheiro.DeDecimal(100m),
            totalParcelas: 12,
            parcelaAtual: 1,
            vigencia: CriarVigenciaPadrao());
        var competencia = Competencia.DeAnoMes(2024, 12);

        // Act & Assert
        contrato.EstaVigenteParaCompetencia(competencia).Should().BeFalse();
    }

    [Fact]
    public void EstaVigenteParaCompetencia_CompetenciaDepoisVigencia_DeveRetornarFalse()
    {
        // Arrange
        var contrato = ContratoConsignado.Criar(
            identificador: "CONS-001",
            descricao: "Teste",
            valorParcela: Dinheiro.DeDecimal(100m),
            totalParcelas: 12,
            parcelaAtual: 1,
            vigencia: CriarVigenciaPadrao());
        var competencia = Competencia.DeAnoMes(2026, 1);

        // Act & Assert
        contrato.EstaVigenteParaCompetencia(competencia).Should().BeFalse();
    }

    #endregion

    #region Parcelas

    [Fact]
    public void ParcelasRestantes_DeveCalcularCorretamente()
    {
        // Arrange
        var contrato = ContratoConsignado.Criar(
            identificador: "CONS-001",
            descricao: "Teste",
            valorParcela: Dinheiro.DeDecimal(100m),
            totalParcelas: 12,
            parcelaAtual: 5,
            vigencia: CriarVigenciaPadrao());

        // Act & Assert
        contrato.ParcelasRestantes.Should().Be(8); // 12 - 5 + 1 = 8
    }

    [Fact]
    public void EstaQuitado_UltimaParcela_DeveRetornarFalse()
    {
        // Arrange
        var contrato = ContratoConsignado.Criar(
            identificador: "CONS-001",
            descricao: "Teste",
            valorParcela: Dinheiro.DeDecimal(100m),
            totalParcelas: 12,
            parcelaAtual: 12,
            vigencia: CriarVigenciaPadrao());

        // Act & Assert
        contrato.EstaQuitado.Should().BeFalse();
    }

    #endregion

    #region Igualdade

    [Fact]
    public void Equals_MesmoIdentificador_DeveRetornarTrue()
    {
        // Arrange
        var contrato1 = ContratoConsignado.Criar(
            identificador: "CONS-001",
            descricao: "Empréstimo BB",
            valorParcela: Dinheiro.DeDecimal(500m),
            totalParcelas: 24,
            parcelaAtual: 1,
            vigencia: CriarVigenciaPadrao());

        var contrato2 = ContratoConsignado.Criar(
            identificador: "CONS-001",
            descricao: "Empréstimo CEF", // Descrição diferente
            valorParcela: Dinheiro.DeDecimal(300m), // Valor diferente
            totalParcelas: 12,
            parcelaAtual: 5,
            vigencia: CriarVigenciaPadrao());

        // Act & Assert
        contrato1.Should().Be(contrato2);
    }

    [Fact]
    public void Equals_IdentificadoresDiferentes_DeveRetornarFalse()
    {
        // Arrange
        var contrato1 = ContratoConsignado.Criar(
            identificador: "CONS-001",
            descricao: "Teste",
            valorParcela: Dinheiro.DeDecimal(100m),
            totalParcelas: 12,
            parcelaAtual: 1,
            vigencia: CriarVigenciaPadrao());

        var contrato2 = ContratoConsignado.Criar(
            identificador: "CONS-002",
            descricao: "Teste",
            valorParcela: Dinheiro.DeDecimal(100m),
            totalParcelas: 12,
            parcelaAtual: 1,
            vigencia: CriarVigenciaPadrao());

        // Act & Assert
        contrato1.Should().NotBe(contrato2);
    }

    #endregion
}
