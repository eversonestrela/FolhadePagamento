using Asp.Versioning;
using FolhadePagamento.Api.DTOs;
using FolhadePagamento.Api.Servicos;
using FolhadePagamento.Aplicacao.Autorizacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FolhadePagamento.Api.Controllers.V1;

/// <summary>
/// Controller para autenticação.
/// 
/// PAPÉIS DISPONÍVEIS (RBAC):
/// - Administrador: Acesso total
/// - Operador: Processar folha e consultar
/// - Consulta: Apenas leitura
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
    /// 
    /// **Administrador:**
    /// - Usuario: admin
    /// - Senha: admin123
    /// 
    /// **Operador:**
    /// - Usuario: operador
    /// - Senha: operador123
    /// 
    /// **Consulta:**
    /// - Usuario: consulta
    /// - Senha: consulta123
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        // NOTA: Em produção, validar contra banco de dados ou Identity Provider
        // Este é um exemplo simplificado para demonstração com múltiplos papéis

        var (usuarioValido, papel) = ValidarCredenciais(request.Usuario, request.Senha);

        if (!usuarioValido)
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
            roles: new[] { papel! }
        );

        var expiracaoMinutos = _configuration.GetValue<int>("Jwt:ExpiracaoMinutos", 60);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiraEm = DateTime.UtcNow.AddMinutes(expiracaoMinutos),
            TipoToken = "Bearer",
            Papel = papel
        });
    }

    /// <summary>
    /// Verifica se o token atual é válido e retorna informações do usuário.
    /// </summary>
    [HttpGet("verificar")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult Verificar()
    {
        var usuarioId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var nome = User.Identity?.Name;
        var papeis = User.FindAll(System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return Ok(new
        {
            Valido = true,
            UsuarioId = usuarioId,
            Nome = nome,
            Papeis = papeis,
            VerificadoEm = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Lista os papéis e permissões disponíveis no sistema.
    /// </summary>
    [HttpGet("papeis")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult ListarPapeis()
    {
        var resultado = Papeis.TodosOsPapeis.Select(papel => new
        {
            Papel = papel,
            Permissoes = MapeamentoPapelPermissao.ObterPermissoes(papel)
        });

        return Ok(resultado);
    }

    /// <summary>
    /// Valida credenciais e retorna o papel do usuário.
    /// Em produção, isso seria feito contra um banco de dados.
    /// </summary>
    private (bool Valido, string? Papel) ValidarCredenciais(string usuario, string senha)
    {
        // Credenciais de demonstração
        var credenciaisDemo = new Dictionary<string, (string Senha, string Papel)>
        {
            ["admin"] = ("admin123", Papeis.Administrador),
            ["operador"] = ("operador123", Papeis.Operador),
            ["consulta"] = ("consulta123", Papeis.Consulta)
        };

        // Também aceita credenciais configuradas no appsettings
        var usuarioConfig = _configuration.GetValue<string>("UsuarioDemo:Usuario");
        var senhaConfig = _configuration.GetValue<string>("UsuarioDemo:Senha");

        if (!string.IsNullOrEmpty(usuarioConfig) && !string.IsNullOrEmpty(senhaConfig))
        {
            if (usuario == usuarioConfig && senha == senhaConfig)
            {
                return (true, Papeis.Administrador);
            }
        }

        // Verificar credenciais de demonstração
        if (credenciaisDemo.TryGetValue(usuario.ToLowerInvariant(), out var credencial))
        {
            if (senha == credencial.Senha)
            {
                return (true, credencial.Papel);
            }
        }

        return (false, null);
    }
}
