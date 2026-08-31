using FolhadePagamento.Infra.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace FolhadePagamento.Infra.Persistencia;

/// <summary>
/// DbContext para o Sistema de Folha de Pagamento.
/// Mapeia as entidades de persistência para o SQL Server.
/// 
/// IMPORTANTE:
/// - Este contexto apenas persiste e consulta dados
/// - Nenhuma regra de negócio é executada aqui
/// - O Core é a única fonte de verdade
/// </summary>
public class FolhaDbContext : DbContext
{
    public FolhaDbContext(DbContextOptions<FolhaDbContext> options)
        : base(options)
    {
    }

    // ========================================================================
    // DBSETS
    // ========================================================================

    public DbSet<FuncionarioDb> Funcionarios => Set<FuncionarioDb>();
    public DbSet<ProcessamentoVersaoDb> ProcessamentosVersao => Set<ProcessamentoVersaoDb>();
    public DbSet<ResultadoCalculoDb> ResultadosCalculo => Set<ResultadoCalculoDb>();
    public DbSet<DetalheInssDb> DetalhesInss => Set<DetalheInssDb>();
    public DbSet<DetalheIrrfDb> DetalhesIrrf => Set<DetalheIrrfDb>();
    public DbSet<DetalheFgtsDb> DetalhesFgts => Set<DetalheFgtsDb>();
    public DbSet<DetalheConsignadosDb> DetalhesConsignados => Set<DetalheConsignadosDb>();
    
    // Lotes
    public DbSet<LoteProcessamentoDb> LotesProcessamento => Set<LoteProcessamentoDb>();
    public DbSet<ItemLoteDb> ItensLote => Set<ItemLoteDb>();

    // ========================================================================
    // CONFIGURAÇÃO DO MODELO
    // ========================================================================

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar todas as configurações de mapeamento
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FolhaDbContext).Assembly);
    }
}
