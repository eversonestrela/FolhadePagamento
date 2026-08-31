using FolhadePagamento.Aplicacao.Lotes;
using FolhadePagamento.Aplicacao.Portas;
using FolhadePagamento.Infra.Persistencia;
using FolhadePagamento.Infra.Persistencia.Repositorios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FolhadePagamento.Infra;

/// <summary>
/// Extensões para configuração de serviços de infraestrutura.
/// </summary>
public static class InfraExtensoes
{
    /// <summary>
    /// Adiciona os serviços de infraestrutura ao container de DI.
    /// </summary>
    /// <param name="services">Container de serviços</param>
    /// <param name="connectionString">String de conexão do SQL Server</param>
    /// <returns>Container de serviços configurado</returns>
    public static IServiceCollection AdicionarInfraestrutura(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // DbContext
        services.AddDbContext<FolhaDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
                
                sqlOptions.CommandTimeout(30);
            });
        });

        // Repositórios
        services.AddScoped<IProcessamentoRepositorio, ProcessamentoRepositorio>();
        services.AddScoped<IFuncionarioRepositorio, FuncionarioRepositorio>();
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
        services.AddScoped<ILoteRepositorio, LoteRepositorio>();

        return services;
    }

    /// <summary>
    /// Adiciona os serviços de infraestrutura com configurações customizadas.
    /// </summary>
    public static IServiceCollection AdicionarInfraestrutura(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddDbContext<FolhaDbContext>(configureOptions);

        // Repositórios
        services.AddScoped<IProcessamentoRepositorio, ProcessamentoRepositorio>();
        services.AddScoped<IFuncionarioRepositorio, FuncionarioRepositorio>();
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
        services.AddScoped<ILoteRepositorio, LoteRepositorio>();

        return services;
    }
}
