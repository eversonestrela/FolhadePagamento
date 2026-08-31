using FolhadePagamento.Infra.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FolhadePagamento.Infra.Persistencia.Configuracoes;

/// <summary>
/// Configuração de mapeamento para LoteProcessamentoDb.
/// </summary>
public class LoteProcessamentoDbConfiguration : IEntityTypeConfiguration<LoteProcessamentoDb>
{
    public void Configure(EntityTypeBuilder<LoteProcessamentoDb> builder)
    {
        builder.ToTable("LoteProcessamento", "dbo");

        builder.HasKey(l => l.LoteId)
            .HasName("PK_LoteProcessamento");

        builder.Property(l => l.LoteId).ValueGeneratedNever();
        builder.Property(l => l.CompetenciaAno).IsRequired();
        builder.Property(l => l.CompetenciaMes).IsRequired();
        builder.Property(l => l.Status).IsRequired().HasMaxLength(30);
        builder.Property(l => l.TotalItens).IsRequired();
        builder.Property(l => l.ItensConcluidos).IsRequired();
        builder.Property(l => l.ItensComFalha).IsRequired();
        builder.Property(l => l.ItensIgnorados).IsRequired();
        builder.Property(l => l.CriadoEm).IsRequired();
        builder.Property(l => l.IniciadoEm);
        builder.Property(l => l.ConcluidoEm);
        builder.Property(l => l.UsuarioId).HasMaxLength(100);
        builder.Property(l => l.Observacao).HasMaxLength(500);

        // Índices
        builder.HasIndex(l => new { l.CompetenciaAno, l.CompetenciaMes })
            .HasDatabaseName("IX_LoteProcessamento_Competencia");

        builder.HasIndex(l => l.Status)
            .HasDatabaseName("IX_LoteProcessamento_Status");

        builder.HasIndex(l => l.CriadoEm)
            .HasDatabaseName("IX_LoteProcessamento_CriadoEm");
    }
}

/// <summary>
/// Configuração de mapeamento para ItemLoteDb.
/// </summary>
public class ItemLoteDbConfiguration : IEntityTypeConfiguration<ItemLoteDb>
{
    public void Configure(EntityTypeBuilder<ItemLoteDb> builder)
    {
        builder.ToTable("ItemLote", "dbo");

        builder.HasKey(i => i.ItemLoteId)
            .HasName("PK_ItemLote");

        builder.Property(i => i.ItemLoteId).ValueGeneratedNever();
        builder.Property(i => i.LoteId).IsRequired();
        builder.Property(i => i.FuncionarioId).IsRequired();
        builder.Property(i => i.Status).IsRequired().HasMaxLength(20);
        builder.Property(i => i.ProcessamentoVersaoId);
        builder.Property(i => i.VersaoNumero);
        builder.Property(i => i.MensagemErro).HasMaxLength(1000);
        builder.Property(i => i.Tentativas).IsRequired();
        builder.Property(i => i.IniciadoEm);
        builder.Property(i => i.ConcluidoEm);

        // Relacionamentos
        builder.HasOne(i => i.Lote)
            .WithMany(l => l.Itens)
            .HasForeignKey(i => i.LoteId)
            .HasConstraintName("FK_ItemLote_LoteProcessamento")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Funcionario)
            .WithMany()
            .HasForeignKey(i => i.FuncionarioId)
            .HasConstraintName("FK_ItemLote_Funcionario")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ProcessamentoVersao)
            .WithMany()
            .HasForeignKey(i => i.ProcessamentoVersaoId)
            .HasConstraintName("FK_ItemLote_ProcessamentoVersao")
            .OnDelete(DeleteBehavior.SetNull);

        // Índices
        builder.HasIndex(i => i.LoteId)
            .HasDatabaseName("IX_ItemLote_Lote");

        builder.HasIndex(i => i.FuncionarioId)
            .HasDatabaseName("IX_ItemLote_Funcionario");

        builder.HasIndex(i => i.Status)
            .HasDatabaseName("IX_ItemLote_Status");

        builder.HasIndex(i => new { i.LoteId, i.Status })
            .HasDatabaseName("IX_ItemLote_LoteStatus");
    }
}
