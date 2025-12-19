using FluentAssertions;
using FolhadePagamento.Dominio.Entidades;
using FolhadePagamento.Dominio.Fgts;
using FolhadePagamento.Dominio.Folha;
using FolhadePagamento.Dominio.Inss;
using FolhadePagamento.Dominio.Irrf;
using FolhadePagamento.Dominio.Processamento;
using FolhadePagamento.Dominio.ValueObjects;
using Xunit;

namespace FolhadePagamento.Testes.Processamento;

/// <summary>
/// Testes para ProcessamentoVersao - garantindo imutabilidade e ciclo de vida.
/// </summary>
public class ProcessamentoVersaoTestes
{
    private readonly DateTime _timestampInicio = new(2025, 6, 15, 10, 0, 0);
    private readonly DateTime _timestampFim = new(2025, 6, 15, 10, 5, 0);

    private static Funcionario CriarFuncionario(decimal salario = 5000m)
    {
        return Funcionario.Criar(FuncionarioId.Novo(), "Teste", Dinheiro.DeDecimal(salario));
    }

    private static ResultadoCalculo CriarResultadoCalculo(Funcionario funcionario, Competencia competencia, DateTime timestamp)
    {
        var calculadoraInss = CalculadoraInss.CriarComTabelasPadrao();
        var calculadoraIrrf = CalculadoraIrrf.CriarComTabelasPadrao();
        var calculadoraFgts = CalculadoraFgts.CriarComTabelaPadrao();
        var calculadora = new CalculadoraFolha(calculadoraInss, calculadoraIrrf, calculadoraFgts);

        return calculadora.Calcular(funcionario, competencia, timestamp);
    }

    #region Iniciar Primeiro Processamento

    [Fact]
    public void IniciarPrimeiro_DeveCriarProcessamentoV1EmAndamento()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act
        var processamento = ProcessamentoVersao.IniciarPrimeiro(
            funcionario.Id,
            competencia,
            _timestampInicio,
            "usuario123");

        // Assert
        processamento.Id.Should().NotBeNull();
        processamento.FuncionarioId.Should().Be(funcionario.Id);
        processamento.Competencia.Should().Be(competencia);
        processamento.Versao.Should().Be(VersaoProcessamento.Primeira);
        processamento.Status.Should().Be(StatusProcessamento.EmProcessamento);
        processamento.Resultado.Should().BeNull();
        processamento.IniciadoEm.Should().Be(_timestampInicio);
        processamento.FinalizadoEm.Should().BeNull();
        processamento.MotivoReprocessamento.Should().BeNull();
        processamento.VersaoAnteriorId.Should().BeNull();
        processamento.EhReprocessamento.Should().BeFalse();
    }

    [Fact]
    public void IniciarPrimeiro_SemFuncionario_DeveLancarExcecao()
    {
        // Arrange
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act
        var acao = () => ProcessamentoVersao.IniciarPrimeiro(null!, competencia, _timestampInicio);

        // Assert
        acao.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Finalizar Processamento

    [Fact]
    public void Finalizar_DeveRetornarProcessamentoFinalizado()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var processamento = ProcessamentoVersao.IniciarPrimeiro(funcionario.Id, competencia, _timestampInicio);
        var resultado = CriarResultadoCalculo(funcionario, competencia, _timestampInicio);

        // Act
        var finalizado = processamento.Finalizar(resultado, _timestampFim);

        // Assert
        finalizado.Status.Should().Be(StatusProcessamento.Finalizado);
        finalizado.Resultado.Should().Be(resultado);
        finalizado.FinalizadoEm.Should().Be(_timestampFim);
        finalizado.EstaFinalizado.Should().BeTrue();
        finalizado.HashResultado.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Finalizar_ProcessamentoJaFinalizado_DeveLancarExcecao()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var processamento = ProcessamentoVersao.IniciarPrimeiro(funcionario.Id, competencia, _timestampInicio);
        var resultado = CriarResultadoCalculo(funcionario, competencia, _timestampInicio);
        var finalizado = processamento.Finalizar(resultado, _timestampFim);

        // Act
        var acao = () => finalizado.Finalizar(resultado, _timestampFim);

        // Assert
        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*status*");
    }

    [Fact]
    public void Finalizar_ResultadoDeOutroFuncionario_DeveLancarExcecao()
    {
        // Arrange
        var funcionario1 = CriarFuncionario();
        var funcionario2 = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var processamento = ProcessamentoVersao.IniciarPrimeiro(funcionario1.Id, competencia, _timestampInicio);
        var resultado = CriarResultadoCalculo(funcionario2, competencia, _timestampInicio);

        // Act
        var acao = () => processamento.Finalizar(resultado, _timestampFim);

        // Assert
        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*outro funcionário*");
    }

    [Fact]
    public void Finalizar_ResultadoDeOutraCompetencia_DeveLancarExcecao()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia1 = Competencia.DeAnoMes(2025, 6);
        var competencia2 = Competencia.DeAnoMes(2025, 7);
        var processamento = ProcessamentoVersao.IniciarPrimeiro(funcionario.Id, competencia1, _timestampInicio);
        var resultado = CriarResultadoCalculo(funcionario, competencia2, _timestampInicio);

        // Act
        var acao = () => processamento.Finalizar(resultado, _timestampFim);

        // Assert
        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*outra competência*");
    }

    #endregion

    #region Imutabilidade

    [Fact]
    public void Finalizar_DeveRetornarNovaInstancia_NaoModificarOriginal()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var original = ProcessamentoVersao.IniciarPrimeiro(funcionario.Id, competencia, _timestampInicio);
        var resultado = CriarResultadoCalculo(funcionario, competencia, _timestampInicio);

        // Act
        var finalizado = original.Finalizar(resultado, _timestampFim);

        // Assert - Original não foi modificado
        original.Status.Should().Be(StatusProcessamento.EmProcessamento);
        original.Resultado.Should().BeNull();
        original.FinalizadoEm.Should().BeNull();

        // Assert - Novo objeto foi criado
        finalizado.Status.Should().Be(StatusProcessamento.Finalizado);
        finalizado.Should().NotBeSameAs(original);
    }

    [Fact]
    public void Cancelar_DeveRetornarNovaInstancia_NaoModificarOriginal()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var original = ProcessamentoVersao.IniciarPrimeiro(funcionario.Id, competencia, _timestampInicio);

        // Act
        var cancelado = original.Cancelar(_timestampFim);

        // Assert - Original não foi modificado
        original.Status.Should().Be(StatusProcessamento.EmProcessamento);

        // Assert - Novo objeto foi criado
        cancelado.Status.Should().Be(StatusProcessamento.Cancelado);
        cancelado.Should().NotBeSameAs(original);
    }

    [Fact]
    public void MarcarComoSuperado_DeveRetornarNovaInstancia_NaoModificarOriginal()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var processamento = ProcessamentoVersao.IniciarPrimeiro(funcionario.Id, competencia, _timestampInicio);
        var resultado = CriarResultadoCalculo(funcionario, competencia, _timestampInicio);
        var finalizado = processamento.Finalizar(resultado, _timestampFim);

        // Act
        var superado = finalizado.MarcarComoSuperado(_timestampFim.AddHours(1));

        // Assert - Original não foi modificado
        finalizado.Status.Should().Be(StatusProcessamento.Finalizado);
        finalizado.SuperadoEm.Should().BeNull();

        // Assert - Novo objeto foi criado
        superado.Status.Should().Be(StatusProcessamento.Superado);
        superado.SuperadoEm.Should().NotBeNull();
        superado.Should().NotBeSameAs(finalizado);
    }

    #endregion

    #region Reprocessamento

    [Fact]
    public void IniciarReprocessamento_DeveCriarV2()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var v1 = ProcessamentoVersao.IniciarPrimeiro(funcionario.Id, competencia, _timestampInicio);
        var resultado = CriarResultadoCalculo(funcionario, competencia, _timestampInicio);
        var v1Finalizado = v1.Finalizar(resultado, _timestampFim);

        // Act
        var v2 = ProcessamentoVersao.IniciarReprocessamento(
            v1Finalizado,
            MotivoReprocessamento.CorrecaoCalculo,
            _timestampFim.AddHours(1),
            "auditor");

        // Assert
        v2.Versao.Numero.Should().Be(2);
        v2.Status.Should().Be(StatusProcessamento.EmProcessamento);
        v2.MotivoReprocessamento.Should().Be(MotivoReprocessamento.CorrecaoCalculo);
        v2.VersaoAnteriorId.Should().Be(v1Finalizado.Id);
        v2.EhReprocessamento.Should().BeTrue();
        v2.UsuarioId.Should().Be("auditor");
    }

    [Fact]
    public void IniciarReprocessamento_VersaoNaoFinalizada_DeveLancarExcecao()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var v1 = ProcessamentoVersao.IniciarPrimeiro(funcionario.Id, competencia, _timestampInicio);

        // Act
        var acao = () => ProcessamentoVersao.IniciarReprocessamento(
            v1,
            MotivoReprocessamento.CorrecaoCalculo,
            _timestampFim);

        // Assert
        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*finalizadas*");
    }

    #endregion

    #region Cancelamento

    [Fact]
    public void Cancelar_ProcessamentoEmAndamento_DeveCancelar()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var processamento = ProcessamentoVersao.IniciarPrimeiro(funcionario.Id, competencia, _timestampInicio);

        // Act
        var cancelado = processamento.Cancelar(_timestampFim);

        // Assert
        cancelado.Status.Should().Be(StatusProcessamento.Cancelado);
        cancelado.FinalizadoEm.Should().Be(_timestampFim);
        cancelado.Resultado.Should().BeNull();
    }

    [Fact]
    public void Cancelar_ProcessamentoJaFinalizado_DeveLancarExcecao()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var processamento = ProcessamentoVersao.IniciarPrimeiro(funcionario.Id, competencia, _timestampInicio);
        var resultado = CriarResultadoCalculo(funcionario, competencia, _timestampInicio);
        var finalizado = processamento.Finalizar(resultado, _timestampFim);

        // Act
        var acao = () => finalizado.Cancelar(_timestampFim);

        // Assert
        acao.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Integridade

    [Fact]
    public void VerificarIntegridade_ResultadoNaoAlterado_DeveRetornarTrue()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var processamento = ProcessamentoVersao.IniciarPrimeiro(funcionario.Id, competencia, _timestampInicio);
        var resultado = CriarResultadoCalculo(funcionario, competencia, _timestampInicio);
        var finalizado = processamento.Finalizar(resultado, _timestampFim);

        // Act
        var integro = finalizado.VerificarIntegridade();

        // Assert
        integro.Should().BeTrue();
    }

    #endregion
}
