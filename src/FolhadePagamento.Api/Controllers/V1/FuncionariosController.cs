using Asp.Versioning;
using FolhadePagamento.Api.Autorizacao;
using FolhadePagamento.Api.DTOs;
using FolhadePagamento.Aplicacao.Portas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FolhadePagamento.Api.Controllers.V1;

/// <summary>
/// Controller para gerenciamento de funcionários.
/// 
/// REGRAS:
/// - Controller NÃO calcula
/// - Controller NÃO usa DbContext diretamente
/// - Controller chama repositórios da Application
/// 
/// AUTORIZAÇÃO (RBAC):
/// - GET: Administrador, Operador, Consulta
/// - POST/PUT/DELETE: Apenas Administrador
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class FuncionariosController : ControllerBase
{
    private readonly IFuncionarioRepositorio _repositorio;

    public FuncionariosController(IFuncionarioRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    /// <summary>
    /// Lista todos os funcionários ativos.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Policies.FuncionarioConsultar)]
    [ProducesResponseType(typeof(IEnumerable<FuncionarioConsulta>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FuncionarioConsulta>>> Listar(
        CancellationToken cancellationToken)
    {
        var funcionarios = await _repositorio.ListarAtivosAsync(cancellationToken);
        return Ok(funcionarios);
    }

    /// <summary>
    /// Obtém um funcionário pelo ID.
    /// </summary>
    [HttpGet("{funcionarioId:guid}")]
    [Authorize(Policy = Policies.FuncionarioConsultar)]
    [ProducesResponseType(typeof(FuncionarioConsulta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FuncionarioConsulta>> ObterPorId(
        Guid funcionarioId,
        CancellationToken cancellationToken)
    {
        var funcionario = await _repositorio.ObterPorIdAsync(funcionarioId, cancellationToken);

        if (funcionario is null)
        {
            return NotFound(new ErroResponse
            {
                Mensagem = "Funcionário não encontrado",
                Codigo = "FUNC_001"
            });
        }

        return Ok(funcionario);
    }

    /// <summary>
    /// Cria um novo funcionário.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.FuncionarioCriar)]
    [ProducesResponseType(typeof(FuncionarioCriadoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FuncionarioCriadoResponse>> Criar(
        [FromBody] CriarFuncionarioRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return BadRequest(new ErroResponse
            {
                Mensagem = "Nome é obrigatório",
                Codigo = "FUNC_002"
            });
        }

        if (request.SalarioBase <= 0)
        {
            return BadRequest(new ErroResponse
            {
                Mensagem = "Salário base deve ser maior que zero",
                Codigo = "FUNC_003"
            });
        }

        var funcionarioId = Guid.NewGuid();
        var criadoEm = DateTime.UtcNow;

        var funcionario = new FuncionarioPersistencia
        {
            FuncionarioId = funcionarioId,
            Nome = request.Nome,
            SalarioBase = request.SalarioBase,
            DataAdmissao = request.DataAdmissao,
            Ativo = true,
            CriadoEm = criadoEm
        };

        await _repositorio.SalvarAsync(funcionario, cancellationToken);

        var response = new FuncionarioCriadoResponse
        {
            FuncionarioId = funcionarioId,
            Nome = request.Nome,
            CriadoEm = criadoEm
        };

        return CreatedAtAction(
            nameof(ObterPorId),
            new { funcionarioId },
            response);
    }

    /// <summary>
    /// Atualiza um funcionário existente.
    /// </summary>
    [HttpPut("{funcionarioId:guid}")]
    [Authorize(Policy = Policies.FuncionarioAtualizar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Atualizar(
        Guid funcionarioId,
        [FromBody] AtualizarFuncionarioRequest request,
        CancellationToken cancellationToken)
    {
        var funcionarioExistente = await _repositorio.ObterPorIdAsync(funcionarioId, cancellationToken);

        if (funcionarioExistente is null)
        {
            return NotFound(new ErroResponse
            {
                Mensagem = "Funcionário não encontrado",
                Codigo = "FUNC_001"
            });
        }

        if (request.SalarioBase.HasValue && request.SalarioBase.Value <= 0)
        {
            return BadRequest(new ErroResponse
            {
                Mensagem = "Salário base deve ser maior que zero",
                Codigo = "FUNC_003"
            });
        }

        var atualizacao = new FuncionarioAtualizacao
        {
            Nome = request.Nome,
            SalarioBase = request.SalarioBase,
            DataAdmissao = request.DataAdmissao,
            AtualizadoEm = DateTime.UtcNow
        };

        await _repositorio.AtualizarAsync(funcionarioId, atualizacao, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Desativa um funcionário (soft delete).
    /// </summary>
    [HttpDelete("{funcionarioId:guid}")]
    [Authorize(Policy = Policies.FuncionarioDesativar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Desativar(
        Guid funcionarioId,
        CancellationToken cancellationToken)
    {
        var funcionario = await _repositorio.ObterPorIdAsync(funcionarioId, cancellationToken);

        if (funcionario is null)
        {
            return NotFound(new ErroResponse
            {
                Mensagem = "Funcionário não encontrado",
                Codigo = "FUNC_001"
            });
        }

        await _repositorio.DesativarAsync(funcionarioId, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Verifica se um funcionário existe e está ativo.
    /// </summary>
    [HttpHead("{funcionarioId:guid}")]
    [Authorize(Policy = Policies.FuncionarioConsultar)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Verificar(
        Guid funcionarioId,
        CancellationToken cancellationToken)
    {
        var existe = await _repositorio.ExisteEAtivoAsync(funcionarioId, cancellationToken);

        if (!existe)
        {
            return NotFound();
        }

        return Ok();
    }
}
