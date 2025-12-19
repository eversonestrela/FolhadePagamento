using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FolhadePagamento.Api.Configuracoes;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FolhadePagamento.Api.Servicos;

/// <summary>
/// Serviço para geração e validação de tokens JWT.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Gera um token JWT para o usuário.
    /// </summary>
    string GerarToken(string usuarioId, string nome, IEnumerable<string>? roles = null);

    /// <summary>
    /// Valida um token JWT.
    /// </summary>
    ClaimsPrincipal? ValidarToken(string token);
}

public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    public JwtService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GerarToken(string usuarioId, string nome, IEnumerable<string>? roles = null)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.ChaveSecreta));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuarioId),
            new(ClaimTypes.Name, nome),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (roles is not null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var token = new JwtSecurityToken(
            issuer: _settings.Emissor,
            audience: _settings.Audiencia,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpiracaoMinutos),
            signingCredentials: credenciais
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidarToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var chave = Encoding.UTF8.GetBytes(_settings.ChaveSecreta);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(chave),
                ValidateIssuer = true,
                ValidIssuer = _settings.Emissor,
                ValidateAudience = true,
                ValidAudience = _settings.Audiencia,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
