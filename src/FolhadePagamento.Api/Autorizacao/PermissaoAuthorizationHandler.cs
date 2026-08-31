using FolhadePagamento.Aplicacao.Autorizacao;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FolhadePagamento.Api.Autorizacao;

/// <summary>
/// Requirement para verificar permissão baseada em papel.
/// </summary>
public class PermissaoRequirement : IAuthorizationRequirement
{
    public string Permissao { get; }

    public PermissaoRequirement(string permissao)
    {
        Permissao = permissao;
    }
}

/// <summary>
/// Handler que verifica se o usuário tem a permissão requerida
/// baseado nos seus papéis.
/// </summary>
public class PermissaoAuthorizationHandler : AuthorizationHandler<PermissaoRequirement>
{
    private readonly ILogger<PermissaoAuthorizationHandler> _logger;

    public PermissaoAuthorizationHandler(ILogger<PermissaoAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissaoRequirement requirement)
    {
        // Obter papéis do usuário
        var papeis = context.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        var usuarioId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anônimo";
        var nomeUsuario = context.User.Identity?.Name ?? "desconhecido";

        if (!papeis.Any())
        {
            _logger.LogWarning(
                "Acesso negado para usuário {UsuarioId} ({Nome}): nenhum papel atribuído. " +
                "Permissão requerida: {Permissao}",
                usuarioId, nomeUsuario, requirement.Permissao);
            return Task.CompletedTask;
        }

        // Verificar se algum papel tem a permissão
        if (MapeamentoPapelPermissao.TemPermissao(papeis, requirement.Permissao))
        {
            _logger.LogDebug(
                "Acesso autorizado para usuário {UsuarioId} ({Nome}). " +
                "Papéis: [{Papeis}]. Permissão: {Permissao}",
                usuarioId, nomeUsuario, string.Join(", ", papeis), requirement.Permissao);

            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Acesso negado para usuário {UsuarioId} ({Nome}). " +
                "Papéis: [{Papeis}]. Permissão requerida: {Permissao}",
                usuarioId, nomeUsuario, string.Join(", ", papeis), requirement.Permissao);
        }

        return Task.CompletedTask;
    }
}
