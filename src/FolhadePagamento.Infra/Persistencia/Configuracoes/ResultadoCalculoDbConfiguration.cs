using FolhadePagamento.Infra.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FolhadePagamento.Infra.Persistencia.Configuracoes;

/// <summary>
/// Configuração de mapeamento para ResultadoCalculoDb.
/// </summary>
public class ResultadoCalculoDbConfiguration : IEntityTypeConfiguration<ResultadoCalculoDb>
{
    public void Configure(EntityTypeBuilder<ResultadoCalculoDb> builder)
    {
        // Tabela
        builder.ToTable("ResultadoCalculo", "dbo");

        // Primary Key
        builder.HasKey(r => r.ResultadoCalculoId)
            .HasName("PK_ResultadoCalculo");

        // Colunas
        builder.Property(r => r.ResultadoCalculoId)
            .ValueGeneratedNever();

        builder.Property(r => r.ProcessamentoVersaoId)
            .IsRequired();

        builder.Property(r => r.SalarioBruto)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.ValorInss)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.ValorIrrf)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.ValorFgts)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.ValorConsignados)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.TotalDescontos)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.SalarioLiquido)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.TotalEncargosPatronais)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.CustoTotalEmpregador)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.CalculadoEm)
            .IsRequired();

        // Relacionamento 1:1 com ProcessamentoVersao
        builder.HasOne(r => r.ProcessamentoVersao)
            .WithOne(p => p.Resultado)
            .HasForeignKey<ResultadoCalculoDb>(r => r.ProcessamentoVersaoId)
            .HasConstraintName("FK_ResultadoCalculo_ProcessamentoVersao")
            .OnDelete(DeleteBehavior.Restrict);

        // Índice único (garante 1:1)
        builder.HasIndex(r => r.ProcessamentoVersaoId)
            .IsUnique()
            .HasDatabaseName("UQ_ResultadoCalculo_ProcessamentoVersao");
    }
}
