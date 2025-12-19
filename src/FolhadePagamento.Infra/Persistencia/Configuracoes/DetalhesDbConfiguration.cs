using FolhadePagamento.Infra.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FolhadePagamento.Infra.Persistencia.Configuracoes;

/// <summary>
/// Configuração de mapeamento para DetalheInssDb.
/// </summary>
public class DetalheInssDbConfiguration : IEntityTypeConfiguration<DetalheInssDb>
{
    public void Configure(EntityTypeBuilder<DetalheInssDb> builder)
    {
        builder.ToTable("DetalheInss", "dbo");

        builder.HasKey(d => d.DetalheInssId)
            .HasName("PK_DetalheInss");

        builder.Property(d => d.DetalheInssId).ValueGeneratedNever();
        builder.Property(d => d.ResultadoCalculoId).IsRequired();
        builder.Property(d => d.BaseCalculo).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.TabelaIdUsada).HasMaxLength(50).IsRequired();
        builder.Property(d => d.AliquotaEfetiva).HasPrecision(5, 2).IsRequired();
        builder.Property(d => d.TetoAplicado).IsRequired();
        builder.Property(d => d.ContribuicaoPorFaixaJson);

        builder.HasOne(d => d.ResultadoCalculo)
            .WithOne(r => r.DetalheInss)
            .HasForeignKey<DetalheInssDb>(d => d.ResultadoCalculoId)
            .HasConstraintName("FK_DetalheInss_ResultadoCalculo")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.ResultadoCalculoId)
            .IsUnique()
            .HasDatabaseName("UQ_DetalheInss_ResultadoCalculo");

        builder.HasIndex(d => d.TabelaIdUsada)
            .HasDatabaseName("IX_DetalheInss_TabelaIdUsada");
    }
}

/// <summary>
/// Configuração de mapeamento para DetalheIrrfDb.
/// </summary>
public class DetalheIrrfDbConfiguration : IEntityTypeConfiguration<DetalheIrrfDb>
{
    public void Configure(EntityTypeBuilder<DetalheIrrfDb> builder)
    {
        builder.ToTable("DetalheIrrf", "dbo");

        builder.HasKey(d => d.DetalheIrrfId)
            .HasName("PK_DetalheIrrf");

        builder.Property(d => d.DetalheIrrfId).ValueGeneratedNever();
        builder.Property(d => d.ResultadoCalculoId).IsRequired();
        builder.Property(d => d.BaseCalculo).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.DeducaoInss).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.NumeroDependentes).IsRequired();
        builder.Property(d => d.DeducaoPorDependente).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.TabelaIdUsada).HasMaxLength(50).IsRequired();
        builder.Property(d => d.FaixaAplicada).HasMaxLength(200);
        builder.Property(d => d.AliquotaAplicada).HasPrecision(5, 2).IsRequired();
        builder.Property(d => d.ParcelaDedutivelUsada).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.Isento).IsRequired();

        builder.HasOne(d => d.ResultadoCalculo)
            .WithOne(r => r.DetalheIrrf)
            .HasForeignKey<DetalheIrrfDb>(d => d.ResultadoCalculoId)
            .HasConstraintName("FK_DetalheIrrf_ResultadoCalculo")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.ResultadoCalculoId)
            .IsUnique()
            .HasDatabaseName("UQ_DetalheIrrf_ResultadoCalculo");

        builder.HasIndex(d => d.TabelaIdUsada)
            .HasDatabaseName("IX_DetalheIrrf_TabelaIdUsada");

        builder.HasIndex(d => d.Isento)
            .HasDatabaseName("IX_DetalheIrrf_Isento")
            .HasFilter("[Isento] = 1");
    }
}

/// <summary>
/// Configuração de mapeamento para DetalheFgtsDb.
/// </summary>
public class DetalheFgtsDbConfiguration : IEntityTypeConfiguration<DetalheFgtsDb>
{
    public void Configure(EntityTypeBuilder<DetalheFgtsDb> builder)
    {
        builder.ToTable("DetalheFgts", "dbo");

        builder.HasKey(d => d.DetalheFgtsId)
            .HasName("PK_DetalheFgts");

        builder.Property(d => d.DetalheFgtsId).ValueGeneratedNever();
        builder.Property(d => d.ResultadoCalculoId).IsRequired();
        builder.Property(d => d.BaseCalculo).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.TabelaIdUsada).HasMaxLength(50).IsRequired();
        builder.Property(d => d.AliquotaAplicada).HasPrecision(5, 2).IsRequired();
        builder.Property(d => d.TipoContribuinte).HasMaxLength(20).IsRequired();

        builder.HasOne(d => d.ResultadoCalculo)
            .WithOne(r => r.DetalheFgts)
            .HasForeignKey<DetalheFgtsDb>(d => d.ResultadoCalculoId)
            .HasConstraintName("FK_DetalheFgts_ResultadoCalculo")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.ResultadoCalculoId)
            .IsUnique()
            .HasDatabaseName("UQ_DetalheFgts_ResultadoCalculo");

        builder.HasIndex(d => d.TabelaIdUsada)
            .HasDatabaseName("IX_DetalheFgts_TabelaIdUsada");

        builder.HasIndex(d => d.TipoContribuinte)
            .HasDatabaseName("IX_DetalheFgts_TipoContribuinte");
    }
}

/// <summary>
/// Configuração de mapeamento para DetalheConsignadosDb.
/// </summary>
public class DetalheConsignadosDbConfiguration : IEntityTypeConfiguration<DetalheConsignadosDb>
{
    public void Configure(EntityTypeBuilder<DetalheConsignadosDb> builder)
    {
        builder.ToTable("DetalheConsignados", "dbo");

        builder.HasKey(d => d.DetalheConsignadosId)
            .HasName("PK_DetalheConsignados");

        builder.Property(d => d.DetalheConsignadosId).ValueGeneratedNever();
        builder.Property(d => d.ResultadoCalculoId).IsRequired();
        builder.Property(d => d.SalarioBaseConsiderado).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.PercentualMargem).HasPrecision(5, 2).IsRequired();
        builder.Property(d => d.MargemTotal).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.MargemUtilizada).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.MargemDisponivel).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.TotalContratosAtivos).IsRequired();
        builder.Property(d => d.DescontosJson);

        builder.HasOne(d => d.ResultadoCalculo)
            .WithOne(r => r.DetalheConsignados)
            .HasForeignKey<DetalheConsignadosDb>(d => d.ResultadoCalculoId)
            .HasConstraintName("FK_DetalheConsignados_ResultadoCalculo")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.ResultadoCalculoId)
            .IsUnique()
            .HasDatabaseName("UQ_DetalheConsignados_ResultadoCalculo");

        builder.HasIndex(d => d.TotalContratosAtivos)
            .HasDatabaseName("IX_DetalheConsignados_TotalContratosAtivos")
            .HasFilter("[TotalContratosAtivos] > 0");
    }
}
