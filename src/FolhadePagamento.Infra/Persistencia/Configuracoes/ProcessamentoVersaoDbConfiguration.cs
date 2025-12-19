using FolhadePagamento.Infra.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FolhadePagamento.Infra.Persistencia.Configuracoes;

/// <summary>
/// Configuração de mapeamento para ProcessamentoVersaoDb.
/// </summary>
public class ProcessamentoVersaoDbConfiguration : IEntityTypeConfiguration<ProcessamentoVersaoDb>
{
    public void Configure(EntityTypeBuilder<ProcessamentoVersaoDb> builder)
    {
        // Tabela
        builder.ToTable("ProcessamentoVersao", "dbo");

        // Primary Key
        builder.HasKey(p => p.ProcessamentoVersaoId)
            .HasName("PK_ProcessamentoVersao");

        // Colunas
        builder.Property(p => p.ProcessamentoVersaoId)
            .ValueGeneratedNever();

        builder.Property(p => p.FuncionarioId)
            .IsRequired();

        builder.Property(p => p.CompetenciaAno)
            .IsRequired();

        builder.Property(p => p.CompetenciaMes)
            .IsRequired();

        builder.Property(p => p.VersaoNumero)
            .IsRequired();

        builder.Property(p => p.VersaoAnteriorId);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.IniciadoEm)
            .IsRequired();

        builder.Property(p => p.FinalizadoEm);

        builder.Property(p => p.SuperadoEm);

        builder.Property(p => p.MotivoReprocessamento)
            .HasMaxLength(50);

        builder.Property(p => p.DescricaoReprocessamento)
            .HasMaxLength(500);

        builder.Property(p => p.UsuarioId)
            .HasMaxLength(100);

        builder.Property(p => p.HashResultado)
            .HasMaxLength(64);

        builder.Property(p => p.CriadoEm)
            .IsRequired();

        // Relacionamentos
        builder.HasOne(p => p.Funcionario)
            .WithMany(f => f.Processamentos)
            .HasForeignKey(p => p.FuncionarioId)
            .HasConstraintName("FK_ProcessamentoVersao_Funcionario")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.VersaoAnterior)
            .WithMany()
            .HasForeignKey(p => p.VersaoAnteriorId)
            .HasConstraintName("FK_ProcessamentoVersao_VersaoAnterior")
            .OnDelete(DeleteBehavior.Restrict);

        // Índice único: Funcionário + Competência + Versão
        builder.HasIndex(p => new { p.FuncionarioId, p.CompetenciaAno, p.CompetenciaMes, p.VersaoNumero })
            .IsUnique()
            .HasDatabaseName("UQ_ProcessamentoVersao_FuncionarioCompetenciaVersao");

        // Índices adicionais
        builder.HasIndex(p => new { p.FuncionarioId, p.CompetenciaAno, p.CompetenciaMes })
            .HasDatabaseName("IX_ProcessamentoVersao_FuncionarioCompetencia");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_ProcessamentoVersao_Status");

        builder.HasIndex(p => p.VersaoAnteriorId)
            .HasDatabaseName("IX_ProcessamentoVersao_VersaoAnterior")
            .HasFilter("[VersaoAnteriorId] IS NOT NULL");

        builder.HasIndex(p => new { p.CompetenciaAno, p.CompetenciaMes })
            .HasDatabaseName("IX_ProcessamentoVersao_Competencia");
    }
}
