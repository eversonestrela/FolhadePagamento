using FolhadePagamento.Infra.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FolhadePagamento.Infra.Persistencia.Configuracoes;

/// <summary>
/// Configuração de mapeamento para FuncionarioDb.
/// </summary>
public class FuncionarioDbConfiguration : IEntityTypeConfiguration<FuncionarioDb>
{
    public void Configure(EntityTypeBuilder<FuncionarioDb> builder)
    {
        // Tabela
        builder.ToTable("Funcionario", "dbo");

        // Primary Key
        builder.HasKey(f => f.FuncionarioId)
            .HasName("PK_Funcionario");

        // Colunas
        builder.Property(f => f.FuncionarioId)
            .ValueGeneratedNever();

        builder.Property(f => f.Nome)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.SalarioBase)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(f => f.DataAdmissao);

        builder.Property(f => f.Ativo)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(f => f.CriadoEm)
            .IsRequired();

        builder.Property(f => f.AtualizadoEm);

        // Índices
        builder.HasIndex(f => f.Nome)
            .HasDatabaseName("IX_Funcionario_Nome");

        builder.HasIndex(f => f.Ativo)
            .HasDatabaseName("IX_Funcionario_Ativo")
            .HasFilter("[Ativo] = 1");
    }
}
