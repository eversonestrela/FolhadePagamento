using FolhadePagamento.Aplicacao.Autorizacao;
using Microsoft.AspNetCore.Authorization;

namespace FolhadePagamento.Api.Autorizacao;

/// <summary>
/// Extensões para configuração de autorização RBAC.
/// </summary>
public static class AutorizacaoExtensoes
{
    /// <summary>
    /// Adiciona autorização baseada em papéis (RBAC) com policies.
    /// </summary>
    public static IServiceCollection AdicionarAutorizacaoRbac(this IServiceCollection services)
    {
        // Registrar handler de autorização
        services.AddScoped<IAuthorizationHandler, PermissaoAuthorizationHandler>();

        // Configurar políticas
        services.AddAuthorizationBuilder()
            // Funcionários
            .AddPolicy(Policies.FuncionarioConsultar, policy =>
                policy.Requirements.Add(new PermissaoRequirement(Permissoes.FuncionarioConsultar)))
            .AddPolicy(Policies.FuncionarioCriar, policy =>
                policy.Requirements.Add(new PermissaoRequirement(Permissoes.FuncionarioCriar)))
            .AddPolicy(Policies.FuncionarioAtualizar, policy =>
                policy.Requirements.Add(new PermissaoRequirement(Permissoes.FuncionarioAtualizar)))
            .AddPolicy(Policies.FuncionarioDesativar, policy =>
                policy.Requirements.Add(new PermissaoRequirement(Permissoes.FuncionarioDesativar)))

            // Processamentos
            .AddPolicy(Policies.ProcessamentoConsultar, policy =>
                policy.Requirements.Add(new PermissaoRequirement(Permissoes.ProcessamentoConsultar)))
            .AddPolicy(Policies.ProcessamentoExecutar, policy =>
                policy.Requirements.Add(new PermissaoRequirement(Permissoes.ProcessamentoExecutar)))

            // Lotes
            .AddPolicy(Policies.LoteConsultar, policy =>
                policy.Requirements.Add(new PermissaoRequirement(Permissoes.LoteConsultar)))
            .AddPolicy(Policies.LoteCriar, policy =>
                policy.Requirements.Add(new PermissaoRequirement(Permissoes.LoteCriar)))
            .AddPolicy(Policies.LoteCancelar, policy =>
                policy.Requirements.Add(new PermissaoRequirement(Permissoes.LoteCancelar)));

        return services;
    }
}
