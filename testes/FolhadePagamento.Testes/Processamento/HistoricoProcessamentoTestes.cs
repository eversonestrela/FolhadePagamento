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
/// Testes para HistoricoProcessamento - gerenciamento de versões.
/// </summary>
public class HistoricoProcessamentoTestes
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

    #region Criação

    [Fact]
    public void Criar_DeveIniciarVazio()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);

        // Act
        var historico = HistoricoProcessamento.Criar(funcionario.Id, competencia);

        // Assert
        historico.FuncionarioId.Should().Be(funcionario.Id);
        historico.Competencia.Should().Be(competencia);
        historico.TotalVersoes.Should().Be(0);
        historico.VersaoAtual.Should().BeNull();
        historico.HouveReprocessamento.Should().BeFalse();
    }

    #endregion

    #region Primeiro Processamento

    [Fact]
    public void IniciarPrimeiroProcessamento_DeveCriarV1()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var historico = HistoricoProcessamento.Criar(funcionario.Id, competencia);

        // Act
        var processamento = historico.IniciarPrimeiroProcessamento(_timestampInicio, "usuario");

        // Assert
        processamento.Versao.Should().Be(VersaoProcessamento.Primeira);
        historico.TotalVersoes.Should().Be(1);
        historico.TemProcessamentoEmAndamento().Should().BeTrue();
    }

    [Fact]
    public void IniciarPrimeiroProcessamento_JaExisteProcessamento_DeveLancarExcecao()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var historico = HistoricoProcessamento.Criar(funcionario.Id, competencia);
        historico.IniciarPrimeiroProcessamento(_timestampInicio);

        // Act
        var acao = () => historico.IniciarPrimeiroProcessamento(_timestampInicio);

        // Assert
        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*IniciarReprocessamento*");
    }

    #endregion

    #region Finalização

    [Fact]
    public void FinalizarProcessamentoAtual_DeveAtualizarHistorico()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var historico = HistoricoProcessamento.Criar(funcionario.Id, competencia);
        var processamento = historico.IniciarPrimeiroProcessamento(_timestampInicio);
        var resultado = CriarResultadoCalculo(funcionario, competencia, _timestampInicio);
        var finalizado = processamento.Finalizar(resultado, _timestampFim);

        // Act
        historico.FinalizarProcessamentoAtual(finalizado, _timestampFim);

        // Assert
        historico.VersaoAtual.Should().NotBeNull();
        historico.VersaoAtual!.Status.Should().Be(StatusProcessamento.Finalizado);
        historico.VersaoAtual.Resultado.Should().Be(resultado);
        historico.TemProcessamentoEmAndamento().Should().BeFalse();
    }

    #endregion

    #region Reprocessamento

    [Fact]
    public void IniciarReprocessamento_DeveCriarV2()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var historico = HistoricoProcessamento.Criar(funcionario.Id, competencia);

        // V1
        var v1 = historico.IniciarPrimeiroProcessamento(_timestampInicio);
        var resultadoV1 = CriarResultadoCalculo(funcionario, competencia, _timestampInicio);
        var v1Finalizado = v1.Finalizar(resultadoV1, _timestampFim);
        historico.FinalizarProcessamentoAtual(v1Finalizado, _timestampFim);

        // Act - V2
        var v2 = historico.IniciarReprocessamento(
            MotivoReprocessamento.CorrecaoCalculo,
            _timestampFim.AddHours(1));

        // Assert
        v2.Versao.Numero.Should().Be(2);
        historico.TotalVersoes.Should().Be(2);
        historico.HouveReprocessamento.Should().BeTrue();
    }

    [Fact]
    public void IniciarReprocessamento_SemVersaoFinalizada_DeveLancarExcecao()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var historico = HistoricoProcessamento.Criar(funcionario.Id, competencia);

        // Act
        var acao = () => historico.IniciarReprocessamento(
            MotivoReprocessamento.CorrecaoCalculo,
            _timestampInicio);

        // Assert
        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*versão finalizada*");
    }

    [Fact]
    public void Reprocessamento_DeveSuperarVersaoAnterior()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var historico = HistoricoProcessamento.Criar(funcionario.Id, competencia);

        // V1
        var v1 = historico.IniciarPrimeiroProcessamento(_timestampInicio);
        var resultadoV1 = CriarResultadoCalculo(funcionario, competencia, _timestampInicio);
        var v1Finalizado = v1.Finalizar(resultadoV1, _timestampFim);
        historico.FinalizarProcessamentoAtual(v1Finalizado, _timestampFim);

        // V2
        var v2 = historico.IniciarReprocessamento(
            MotivoReprocessamento.CorrecaoCalculo,
            _timestampFim.AddHours(1));
        var resultadoV2 = CriarResultadoCalculo(funcionario, competencia, _timestampFim.AddHours(1));
        var v2Finalizado = v2.Finalizar(resultadoV2, _timestampFim.AddHours(2));

        // Act
        historico.FinalizarProcessamentoAtual(v2Finalizado, _timestampFim.AddHours(2));

        // Assert
        var versaoV1 = historico.ObterVersao(1);
        versaoV1!.Status.Should().Be(StatusProcessamento.Superado);
        versaoV1.SuperadoEm.Should().NotBeNull();

        var versaoV2 = historico.ObterVersao(2);
        versaoV2!.Status.Should().Be(StatusProcessamento.Finalizado);

        historico.VersaoAtual.Should().Be(versaoV2);
    }

    #endregion

    #region Consulta de Versões

    [Fact]
    public void ObterVersao_DeveRetornarVersaoCorreta()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var historico = HistoricoProcessamento.Criar(funcionario.Id, competencia);

        var v1 = historico.IniciarPrimeiroProcessamento(_timestampInicio);
        var resultadoV1 = CriarResultadoCalculo(funcionario, competencia, _timestampInicio);
        var v1Finalizado = v1.Finalizar(resultadoV1, _timestampFim);
        historico.FinalizarProcessamentoAtual(v1Finalizado, _timestampFim);

        // Act
        var versao = historico.ObterVersao(VersaoProcessamento.Primeira);

        // Assert
        versao.Should().NotBeNull();
        versao!.Versao.Numero.Should().Be(1);
    }

    [Fact]
    public void ObterVersao_VersaoInexistente_DeveRetornarNull()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var historico = HistoricoProcessamento.Criar(funcionario.Id, competencia);

        // Act
        var versao = historico.ObterVersao(99);

        // Assert
        versao.Should().BeNull();
    }

    [Fact]
    public void ObterVersoesFinalizadas_DeveRetornarApenasFinalizadas()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var historico = HistoricoProcessamento.Criar(funcionario.Id, competencia);

        // V1 finalizada
        var v1 = historico.IniciarPrimeiroProcessamento(_timestampInicio);
        var resultadoV1 = CriarResultadoCalculo(funcionario, competencia, _timestampInicio);
        var v1Finalizado = v1.Finalizar(resultadoV1, _timestampFim);
        historico.FinalizarProcessamentoAtual(v1Finalizado, _timestampFim);

        // V2 em andamento
        var v2 = historico.IniciarReprocessamento(
            MotivoReprocessamento.CorrecaoCalculo,
            _timestampFim.AddHours(1));

        // Act
        var finalizadas = historico.ObterVersoesFinalizadas();

        // Assert
        finalizadas.Should().HaveCount(1);
        finalizadas[0].Versao.Numero.Should().Be(1);
    }

    #endregion

    #region Comparação de Versões

    [Fact]
    public void CompararVersoes_DeveRetornarDiferencas()
    {
        // Arrange
        var funcionario = CriarFuncionario(5000m);
        var competencia = Competencia.DeAnoMes(2025, 6);
        var historico = HistoricoProcessamento.Criar(funcionario.Id, competencia);

        // V1
        var v1 = historico.IniciarPrimeiroProcessamento(_timestampInicio);
        var resultadoV1 = CriarResultadoCalculo(funcionario, competencia, _timestampInicio);
        var v1Finalizado = v1.Finalizar(resultadoV1, _timestampFim);
        historico.FinalizarProcessamentoAtual(v1Finalizado, _timestampFim);

        // Simula mudança - novo funcionário com salário diferente (para fins de teste)
        // Em produção, seria o mesmo funcionário com dados corrigidos
        var funcionarioCorrigido = Funcionario.Criar(funcionario.Id, "Teste", Dinheiro.DeDecimal(5500m));

        // V2
        var v2 = historico.IniciarReprocessamento(
            MotivoReprocessamento.CorrecaoCadastro,
            _timestampFim.AddHours(1));

        var resultadoV2 = ResultadoCalculo.Criar(
            funcionario.Id,
            competencia,
            Dinheiro.DeDecimal(5500m), // Salário corrigido
            resultadoV1.ValorInss,
            resultadoV1.DetalheInss,
            resultadoV1.ValorIrrf,
            resultadoV1.DetalheIrrf,
            resultadoV1.ValorFgts,
            resultadoV1.DetalheFgts,
            Dinheiro.Zero,
            _timestampFim.AddHours(1));

        var v2Finalizado = v2.Finalizar(resultadoV2, _timestampFim.AddHours(2));
        historico.FinalizarProcessamentoAtual(v2Finalizado, _timestampFim.AddHours(2));

        // Act
        var diferenca = historico.CompararVersoes(
            VersaoProcessamento.Primeira,
            VersaoProcessamento.DeNumero(2));

        // Assert
        diferenca.Should().NotBeNull();
        diferenca!.HouveMudanca.Should().BeTrue();
        diferenca.DiferencaBruto.Should().Be(500m);
    }

    #endregion

    #region Reconstituição

    [Fact]
    public void Reconstituir_DeveCarregarVersoes()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var resultado = CriarResultadoCalculo(funcionario, competencia, _timestampInicio);

        var versaoExistente = ProcessamentoVersao.CriarFinalizado(
            ProcessamentoId.Novo(),
            funcionario.Id,
            competencia,
            VersaoProcessamento.Primeira,
            resultado,
            _timestampInicio,
            _timestampFim);

        // Act
        var historico = HistoricoProcessamento.Reconstituir(
            funcionario.Id,
            competencia,
            new[] { versaoExistente });

        // Assert
        historico.TotalVersoes.Should().Be(1);
        historico.VersaoAtual.Should().NotBeNull();
        historico.VersaoAtual!.Resultado.Should().Be(resultado);
    }

    #endregion

    #region Rastreabilidade

    [Fact]
    public void ProcessamentoVersao_DeveManterlinkParaVersaoAnterior()
    {
        // Arrange
        var funcionario = CriarFuncionario();
        var competencia = Competencia.DeAnoMes(2025, 6);
        var historico = HistoricoProcessamento.Criar(funcionario.Id, competencia);

        // V1
        var v1 = historico.IniciarPrimeiroProcessamento(_timestampInicio);
        var resultadoV1 = CriarResultadoCalculo(funcionario, competencia, _timestampInicio);
        var v1Finalizado = v1.Finalizar(resultadoV1, _timestampFim);
        historico.FinalizarProcessamentoAtual(v1Finalizado, _timestampFim);

        // V2
        var v2 = historico.IniciarReprocessamento(
            MotivoReprocessamento.AtualizacaoLegislacao,
            _timestampFim.AddHours(1));

        // Assert
        v2.VersaoAnteriorId.Should().Be(v1Finalizado.Id);
        v2.MotivoReprocessamento.Should().Be(MotivoReprocessamento.AtualizacaoLegislacao);
    }

    #endregion
}
