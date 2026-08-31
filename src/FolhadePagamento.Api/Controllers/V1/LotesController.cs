using Asp.Versioning;
using FolhadePagamento.Api.Autorizacao;
using FolhadePagamento.Api.DTOs;
using FolhadePagamento.Aplicacao.Lotes;
using FolhadePagamento.Aplicacao.Portas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FolhadePagamento.Api.Controllers.V1;

/// <summary>
/// Controller para gerenciamento de lotes de processamento.
/// 
/// Permite criar, consultar e acompanhar lotes de processamento em massa.
/// 
/// AUTORIZAÇÃO (RBAC):
/// - GET: Administrador, Operador, Consulta
/// - POST (Criar): Administrador, Operador
/// - POST (Cancelar): Apenas Administrador
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class LotesController : ControllerBase
{
    private readonly ILoteRepositorio _loteRepositorio;
    private readonly IFuncionarioRepositorio _funcionarioRepositorio;

    public LotesController(
        ILoteRepositorio loteRepositorio,
        IFuncionarioRepositorio funcionarioRepositorio)
    {
        _loteRepositorio = loteRepositorio;
        _funcionarioRepositorio = funcionarioRepositorio;
    }

    /// <summary>
    /// Cria um novo lote de processamento para uma competência.
    /// O lote será processado de forma assíncrona pelo Worker.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.LoteCriar)]
    [ProducesResponseType(typeof(LoteCriadoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoteCriadoResponse>> CriarLote(
        [FromBody] CriarLoteRequest request,
        CancellationToken cancellationToken)
    {
        // Validar competência
        if (request.CompetenciaAno < 2020 || request.CompetenciaAno > 2100)
        {
            return BadRequest(new ErroResponse
            {
                Mensagem = "Ano de competência inválido",
                Codigo = "LOTE_001"
            });
        }

        if (request.CompetenciaMes < 1 || request.CompetenciaMes > 12)
        {
            return BadRequest(new ErroResponse
            {
                Mensagem = "Mês de competência inválido",
                Codigo = "LOTE_002"
            });
        }

        // Verificar se já existe lote ativo para a competência
        var existeLoteAtivo = await _loteRepositorio.ExisteLoteAtivoParaCompetenciaAsync(
            request.CompetenciaAno, request.CompetenciaMes, cancellationToken);

        if (existeLoteAtivo)
        {
            return Conflict(new ErroResponse
            {
                Mensagem = "Já existe um lote ativo para esta competência",
                Codigo = "LOTE_003"
            });
        }

        // Obter funcionários ativos
        var funcionarios = await _funcionarioRepositorio.ListarAtivosAsync(cancellationToken);

        if (!funcionarios.Any())
        {
            return BadRequest(new ErroResponse
            {
                Mensagem = "Nenhum funcionário ativo encontrado",
                Codigo = "LOTE_004"
            });
        }

        // Filtrar por IDs específicos se informado
        if (request.FuncionarioIds?.Any() == true)
        {
            funcionarios = funcionarios
                .Where(f => request.FuncionarioIds.Contains(f.FuncionarioId))
                .ToList();

            if (!funcionarios.Any())
            {
                return BadRequest(new ErroResponse
                {
                    Mensagem = "Nenhum dos funcionários informados está ativo",
                    Codigo = "LOTE_005"
                });
            }
        }

        var loteId = Guid.NewGuid();
        var agora = DateTime.UtcNow;
        var usuarioId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var lote = new LoteProcessamentoPersistencia
        {
            LoteId = loteId,
            CompetenciaAno = request.CompetenciaAno,
            CompetenciaMes = request.CompetenciaMes,
            Status = StatusLote.Pendente,
            TotalItens = funcionarios.Count,
            ItensConcluidos = 0,
            ItensComFalha = 0,
            ItensIgnorados = 0,
            CriadoEm = agora,
            UsuarioId = usuarioId,
            Observacao = request.Observacao
        };

        var itens = funcionarios.Select(f => new ItemLotePersistencia
        {
            ItemLoteId = Guid.NewGuid(),
            LoteId = loteId,
            FuncionarioId = f.FuncionarioId,
            Status = StatusItemLote.Pendente,
            Tentativas = 0
        });

        await _loteRepositorio.CriarLoteAsync(lote, itens, cancellationToken);

        var response = new LoteCriadoResponse
        {
            LoteId = loteId,
            CompetenciaAno = request.CompetenciaAno,
            CompetenciaMes = request.CompetenciaMes,
            TotalItens = funcionarios.Count,
            Status = "Pendente",
            CriadoEm = agora,
            Mensagem = "Lote criado com sucesso. O processamento será iniciado em breve."
        };

        return CreatedAtAction(nameof(ObterPorId), new { loteId }, response);
    }

    /// <summary>
    /// Obtém detalhes de um lote por ID.
    /// </summary>
    [HttpGet("{loteId:guid}")]
    [Authorize(Policy = Policies.LoteConsultar)]
    [ProducesResponseType(typeof(LoteProcessamentoConsulta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoteProcessamentoConsulta>> ObterPorId(
        Guid loteId,
        CancellationToken cancellationToken)
    {
        var lote = await _loteRepositorio.ObterLotePorIdAsync(loteId, cancellationToken);

        if (lote is null)
        {
            return NotFound(new ErroResponse
            {
                Mensagem = "Lote não encontrado",
                Codigo = "LOTE_006"
            });
        }

        return Ok(lote);
    }

    /// <summary>
    /// Lista itens de um lote.
    /// </summary>
    [HttpGet("{loteId:guid}/itens")]
    [Authorize(Policy = Policies.LoteConsultar)]
    [ProducesResponseType(typeof(IEnumerable<ItemLoteConsulta>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ItemLoteConsulta>>> ListarItens(
        Guid loteId,
        CancellationToken cancellationToken)
    {
        var lote = await _loteRepositorio.ObterLotePorIdAsync(loteId, cancellationToken);

        if (lote is null)
        {
            return NotFound(new ErroResponse
            {
                Mensagem = "Lote não encontrado",
                Codigo = "LOTE_006"
            });
        }

        var itens = await _loteRepositorio.ListarItensDoLoteAsync(loteId, cancellationToken);

        return Ok(itens);
    }

    /// <summary>
    /// Lista lotes ativos (pendentes ou em processamento).
    /// </summary>
    [HttpGet("ativos")]
    [Authorize(Policy = Policies.LoteConsultar)]
    [ProducesResponseType(typeof(IEnumerable<LoteResumoConsulta>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LoteResumoConsulta>>> ListarAtivos(
        CancellationToken cancellationToken)
    {
        var lotes = await _loteRepositorio.ListarLotesAtivosAsync(cancellationToken);
        return Ok(lotes);
    }

    /// <summary>
    /// Lista lotes por competência.
    /// </summary>
    [HttpGet("competencia/{ano:int}/{mes:int}")]
    [Authorize(Policy = Policies.LoteConsultar)]
    [ProducesResponseType(typeof(IEnumerable<LoteResumoConsulta>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LoteResumoConsulta>>> ListarPorCompetencia(
        int ano,
        int mes,
        CancellationToken cancellationToken)
    {
        var lotes = await _loteRepositorio.ListarLotesPorCompetenciaAsync(ano, mes, cancellationToken);
        return Ok(lotes);
    }

    /// <summary>
    /// Obtém progresso de um lote.
    /// </summary>
    [HttpGet("{loteId:guid}/progresso")]
    [Authorize(Policy = Policies.LoteConsultar)]
    [ProducesResponseType(typeof(ProgressoLoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgressoLoteResponse>> ObterProgresso(
        Guid loteId,
        CancellationToken cancellationToken)
    {
        var lote = await _loteRepositorio.ObterLotePorIdAsync(loteId, cancellationToken);

        if (lote is null)
        {
            return NotFound(new ErroResponse
            {
                Mensagem = "Lote não encontrado",
                Codigo = "LOTE_006"
            });
        }

        var contagem = await _loteRepositorio.ContarItensPorStatusAsync(loteId, cancellationToken);

        var response = new ProgressoLoteResponse
        {
            LoteId = loteId,
            Status = lote.Status,
            TotalItens = lote.TotalItens,
            Pendentes = contagem.GetValueOrDefault(StatusItemLote.Pendente, 0),
            EmProcessamento = contagem.GetValueOrDefault(StatusItemLote.EmProcessamento, 0),
            Concluidos = contagem.GetValueOrDefault(StatusItemLote.Sucesso, 0),
            ComFalha = contagem.GetValueOrDefault(StatusItemLote.Falha, 0),
            Ignorados = contagem.GetValueOrDefault(StatusItemLote.Ignorado, 0),
            PercentualConcluido = lote.PercentualConcluido,
            IniciadoEm = lote.IniciadoEm,
            ConcluidoEm = lote.ConcluidoEm,
            DuracaoTotal = lote.DuracaoTotal
        };

        return Ok(response);
    }

    /// <summary>
    /// Cancela um lote pendente.
    /// </summary>
    [HttpPost("{loteId:guid}/cancelar")]
    [Authorize(Policy = Policies.LoteCancelar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Cancelar(
        Guid loteId,
        CancellationToken cancellationToken)
    {
        var lote = await _loteRepositorio.ObterLotePorIdAsync(loteId, cancellationToken);

        if (lote is null)
        {
            return NotFound(new ErroResponse
            {
                Mensagem = "Lote não encontrado",
                Codigo = "LOTE_006"
            });
        }

        if (lote.Status != "Pendente")
        {
            return BadRequest(new ErroResponse
            {
                Mensagem = "Apenas lotes pendentes podem ser cancelados",
                Codigo = "LOTE_007"
            });
        }

        await _loteRepositorio.AtualizarStatusLoteAsync(
            loteId,
            StatusLote.Cancelado,
            concluidoEm: DateTime.UtcNow,
            cancellationToken: cancellationToken);

        return NoContent();
    }
}
