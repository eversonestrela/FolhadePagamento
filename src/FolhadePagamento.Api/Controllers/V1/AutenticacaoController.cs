using Asp.Versioning;
using FolhadePagamento.Api.DTOs;
using FolhadePagamento.Api.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FolhadePagamento.Api.Controllers.V1;

/// <summary>
/// Controller para autenticação.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AutenticacaoController : ControllerBase
{
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;

    public AutenticacaoController(IJwtService jwtService, IConfiguration configuration)
    {
        _jwtService = jwtService;
        _configuration = configuration;
    }

    /// <summary>
    /// Autentica um usuário e retorna um token JWT.
    /// </summary>
    /// <remarks>
    /// Credenciais de exemplo para desenvolvimento:
    /// - Usuario: admin
    /// - Senha: admin123
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        // NOTA: Em produção, validar contra banco de dados ou Identity Provider
        // Este é apenas um exemplo simplificado para demonstração
        var usuarioValido = _configuration.GetValue<string>("UsuarioDemo:Usuario") ?? "admin";
        var senhaValida = _configuration.GetValue<string>("UsuarioDemo:Senha") ?? "admin123";

        if (request.Usuario != usuarioValido || request.Senha != senhaValida)
        {
            return Unauthorized(new ErroResponse
            {
                Mensagem = "Credenciais inválidas",
                Codigo = "AUTH_001"
            });
        }

        var token = _jwtService.GerarToken(
            usuarioId: Guid.NewGuid().ToString(),
            nome: request.Usuario,
            roles: new[] { "Usuario", "Admin" }
        );

        var expiracaoMinutos = _configuration.GetValue<int>("Jwt:ExpiracaoMinutos", 60);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiraEm = DateTime.UtcNow.AddMinutes(expiracaoMinutos),
            TipoToken = "Bearer"
        });
    }

    /// <summary>
    /// Verifica se o token atual é válido.
    /// </summary>
    [HttpGet("verificar")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult Verificar()
    {
        var usuarioId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var nome = User.Identity?.Name;

        return Ok(new
        {
            Valido = true,
            UsuarioId = usuarioId,
            Nome = nome,
            VerificadoEm = DateTime.UtcNow
        });
    }
}
