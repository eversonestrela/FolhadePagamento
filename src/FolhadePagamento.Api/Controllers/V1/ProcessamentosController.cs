using Asp.Versioning;
using FolhadePagamento.Api.Autorizacao;
using FolhadePagamento.Api.DTOs;
using FolhadePagamento.Aplicacao.Portas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FolhadePagamento.Api.Controllers.V1;

/// <summary>
/// Controller para processamento de folha de pagamento.
/// 
/// REGRAS:
/// - Controller NÃO calcula
/// - Controller NÃO usa DbContext diretamente
/// - Controller chama Casos de Uso e repositórios da Application
/// - Valores vêm do Core já calculados
/// 
/// AUTORIZAÇÃO (RBAC):
/// - GET: Administrador, Operador, Consulta
/// - POST (Processar): Administrador, Operador
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ProcessamentosController : ControllerBase
{
    private readonly IProcessamentoRepositorio _repositorio;
    private readonly IFuncionarioRepositorio _funcionarioRepositorio;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public ProcessamentosController(
        IProcessamentoRepositorio repositorio,
        IFuncionarioRepositorio funcionarioRepositorio,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _repositorio = repositorio;
        _funcionarioRepositorio = funcionarioRepositorio;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    /// <summary>
    /// Obtém um processamento por ID.
    /// </summary>
    [HttpGet("{processamentoVersaoId:guid}")]
    [Authorize(Policy = Policies.ProcessamentoConsultar)]
    [ProducesResponseType(typeof(ProcessamentoConsulta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProcessamentoConsulta>> ObterPorId(
        Guid processamentoVersaoId,
        CancellationToken cancellationToken)
    {
        var processamento = await _repositorio.ObterPorIdAsync(processamentoVersaoId, cancellationToken);

        if (processamento is null)
        {
            return NotFound(new ErroResponse
            {
                Mensagem = "Processamento não encontrado",
                Codigo = "PROC_001"
            });
        }

        return Ok(processamento);
    }

    /// <summary>
    /// Obtém a versão atual do processamento para um funcionário e competência.
    /// </summary>
    [HttpGet("funcionario/{funcionarioId:guid}/competencia/{ano:int}/{mes:int}")]
    [Authorize(Policy = Policies.ProcessamentoConsultar)]
    [ProducesResponseType(typeof(ProcessamentoConsulta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProcessamentoConsulta>> ObterVersaoAtual(
        Guid funcionarioId,
        int ano,
        int mes,
        CancellationToken cancellationToken)
    {
        var processamento = await _repositorio.ObterVersaoAtualAsync(
            funcionarioId, ano, mes, cancellationToken);

        if (processamento is null)
        {
            return NotFound(new ErroResponse
            {
                Mensagem = "Nenhum processamento encontrado para esta competência",
                Codigo = "PROC_002"
            });
        }

        return Ok(processamento);
    }

    /// <summary>
    /// Obtém o histórico de versões para um funcionário e competência.
    /// </summary>
    [HttpGet("funcionario/{funcionarioId:guid}/competencia/{ano:int}/{mes:int}/historico")]
    [Authorize(Policy = Policies.ProcessamentoConsultar)]
    [ProducesResponseType(typeof(IEnumerable<ProcessamentoResumoConsulta>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProcessamentoResumoConsulta>>> ObterHistorico(
        Guid funcionarioId,
        int ano,
        int mes,
        CancellationToken cancellationToken)
    {
        var historico = await _repositorio.ObterHistoricoVersoesAsync(
            funcionarioId, ano, mes, cancellationToken);

        return Ok(historico);
    }

    /// <summary>
    /// Lista processamentos de uma competência.
    /// </summary>
    [HttpGet("competencia/{ano:int}/{mes:int}")]
    [Authorize(Policy = Policies.ProcessamentoConsultar)]
    [ProducesResponseType(typeof(IEnumerable<ProcessamentoResumoConsulta>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProcessamentoResumoConsulta>>> ListarPorCompetencia(
        int ano,
        int mes,
        [FromQuery] bool incluirHistorico = false,
        CancellationToken cancellationToken = default)
    {
        var processamentos = await _repositorio.ListarPorCompetenciaAsync(
            ano, mes, apenasAtual: !incluirHistorico, cancellationToken);

        return Ok(processamentos);
    }

    /// <summary>
    /// Processa a folha de pagamento de um funcionário.
    /// 
    /// NOTA: Este endpoint demonstra o fluxo de integração.
    /// Em produção, o cálculo deve ser delegado ao Core (Caso de Uso).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.ProcessamentoExecutar)]
    [ProducesResponseType(typeof(ProcessamentoCriadoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProcessamentoCriadoResponse>> Processar(
        [FromBody] ProcessarFolhaRequest request,
        CancellationToken cancellationToken)
    {
        // Validar competência
        if (request.CompetenciaAno < 2020 || request.CompetenciaAno > 2100)
        {
            return BadRequest(new ErroResponse
            {
                Mensagem = "Ano de competência inválido",
                Codigo = "PROC_003"
            });
        }

        if (request.CompetenciaMes < 1 || request.CompetenciaMes > 12)
        {
            return BadRequest(new ErroResponse
            {
                Mensagem = "Mês de competência inválido",
                Codigo = "PROC_004"
            });
        }

        // Verificar se funcionário existe
        var funcionario = await _funcionarioRepositorio.ObterPorIdAsync(
            request.FuncionarioId, cancellationToken);

        if (funcionario is null)
        {
            return NotFound(new ErroResponse
            {
                Mensagem = "Funcionário não encontrado",
                Codigo = "FUNC_001"
            });
        }

        // Iniciar transação
        await _unidadeDeTrabalho.IniciarTransacaoAsync(cancellationToken);

        try
        {
            // Verificar se já existe processamento e marcar como superado
            var versaoAtual = await _repositorio.ObterVersaoAtualAsync(
                request.FuncionarioId,
                request.CompetenciaAno,
                request.CompetenciaMes,
                cancellationToken);

            if (versaoAtual is not null)
            {
                await _repositorio.MarcarComoSuperadoAsync(
                    versaoAtual.ProcessamentoVersaoId,
                    DateTime.UtcNow,
                    cancellationToken);
            }

            // Obter próximo número de versão
            var numeroVersao = await _repositorio.ObterProximoNumeroVersaoAsync(
                request.FuncionarioId,
                request.CompetenciaAno,
                request.CompetenciaMes,
                cancellationToken);

            // ================================================================
            // AQUI SERIA A CHAMADA AO CORE PARA CALCULAR
            // Em produção: var resultado = _casoDeUso.Calcular(funcionario, ...);
            // 
            // Por enquanto, simulamos valores para demonstrar o fluxo
            // ================================================================
            var salarioBruto = funcionario.SalarioBase;
            var valorInss = Math.Round(salarioBruto * 0.11m, 2); // Simplificado
            var valorIrrf = Math.Round((salarioBruto - valorInss) * 0.075m, 2); // Simplificado
            var valorFgts = Math.Round(salarioBruto * 0.08m, 2);
            var totalDescontos = valorInss + valorIrrf;
            var salarioLiquido = salarioBruto - totalDescontos;

            var processamentoVersaoId = Guid.NewGuid();
            var resultadoCalculoId = Guid.NewGuid();
            var agora = DateTime.UtcNow;

            var processamento = new ProcessamentoPersistencia
            {
                ProcessamentoVersaoId = processamentoVersaoId,
                FuncionarioId = request.FuncionarioId,
                CompetenciaAno = request.CompetenciaAno,
                CompetenciaMes = request.CompetenciaMes,
                VersaoNumero = numeroVersao,
                VersaoAnteriorId = versaoAtual?.ProcessamentoVersaoId,
                Status = "Finalizado",
                IniciadoEm = agora,
                FinalizadoEm = agora,
                MotivoReprocessamento = versaoAtual is not null ? "Novo processamento" : null,
                Resultado = new ResultadoPersistencia
                {
                    ResultadoCalculoId = resultadoCalculoId,
                    SalarioBruto = salarioBruto,
                    ValorInss = valorInss,
                    ValorIrrf = valorIrrf,
                    ValorFgts = valorFgts,
                    ValorConsignados = 0,
                    TotalDescontos = totalDescontos,
                    SalarioLiquido = salarioLiquido,
                    TotalEncargosPatronais = valorFgts,
                    CustoTotalEmpregador = salarioBruto + valorFgts,
                    CalculadoEm = agora,
                    DetalheInss = new DetalheInssPersistencia
                    {
                        DetalheInssId = Guid.NewGuid(),
                        BaseCalculo = salarioBruto,
                        TabelaIdUsada = "INSS_2024_SIMPLIFICADO",
                        AliquotaEfetiva = 11m,
                        TetoAplicado = false
                    },
                    DetalheIrrf = new DetalheIrrfPersistencia
                    {
                        DetalheIrrfId = Guid.NewGuid(),
                        BaseCalculo = salarioBruto - valorInss,
                        DeducaoInss = valorInss,
                        NumeroDependentes = request.NumeroDependentes,
                        DeducaoPorDependente = 189.59m,
                        TabelaIdUsada = "IRRF_2024_SIMPLIFICADO",
                        AliquotaAplicada = 7.5m,
                        ParcelaDedutivelUsada = 0,
                        Isento = false
                    },
                    DetalheFgts = new DetalheFgtsPersistencia
                    {
                        DetalheFgtsId = Guid.NewGuid(),
                        BaseCalculo = salarioBruto,
                        TabelaIdUsada = "FGTS_2024",
                        AliquotaAplicada = 8m,
                        TipoContribuinte = "Normal"
                    }
                }
            };

            await _repositorio.SalvarProcessamentoAsync(processamento, cancellationToken);

            await _unidadeDeTrabalho.ConfirmarAsync(cancellationToken);

            var response = new ProcessamentoCriadoResponse
            {
                ProcessamentoVersaoId = processamentoVersaoId,
                VersaoNumero = numeroVersao,
                Status = "Finalizado",
                SalarioLiquido = salarioLiquido,
                ProcessadoEm = agora
            };

            return CreatedAtAction(
                nameof(ObterPorId),
                new { processamentoVersaoId },
                response);
        }
        catch (Exception ex)
        {
            await _unidadeDeTrabalho.ReverterAsync(cancellationToken);

            return BadRequest(new ErroResponse
            {
                Mensagem = "Erro ao processar folha",
                Detalhe = ex.Message,
                Codigo = "PROC_500"
            });
        }
    }

    /// <summary>
    /// Verifica se existe processamento para funcionário e competência.
    /// </summary>
    [HttpHead("funcionario/{funcionarioId:guid}/competencia/{ano:int}/{mes:int}")]
    [Authorize(Policy = Policies.ProcessamentoConsultar)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> VerificarExistencia(
        Guid funcionarioId,
        int ano,
        int mes,
        CancellationToken cancellationToken)
    {
        var existe = await _repositorio.ExisteProcessamentoAsync(
            funcionarioId, ano, mes, cancellationToken);

        if (!existe)
        {
            return NotFound();
        }

        return Ok();
    }
}
