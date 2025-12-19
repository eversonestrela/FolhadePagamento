namespace FolhadePagamento.Infra.Persistencia.Entidades;

/// <summary>
/// Entidade de persistência para Resultado de Cálculo.
/// Mapeada para a tabela dbo.ResultadoCalculo.
/// 
/// IMPORTANTE:
/// - Esta entidade é IMUTÁVEL (snapshot congelado)
/// - Nunca deve sofrer UPDATE ou DELETE
/// - Contém todos os valores finais do cálculo
/// </summary>
public class ResultadoCalculoDb
{
    /// <summary>
    /// Identificador único do resultado (PK).
    /// </summary>
    public Guid ResultadoCalculoId { get; set; }

    /// <summary>
    /// Processamento ao qual este resultado pertence (FK, 1:1).
    /// </summary>
    public Guid ProcessamentoVersaoId { get; set; }

    /// <summary>
    /// Salário bruto (proventos).
    /// </summary>
    public decimal SalarioBruto { get; set; }

    /// <summary>
    /// Desconto de INSS.
    /// </summary>
    public decimal ValorInss { get; set; }

    /// <summary>
    /// Desconto de IRRF.
    /// </summary>
    public decimal ValorIrrf { get; set; }

    /// <summary>
    /// Valor de FGTS (encargo patronal).
    /// </summary>
    public decimal ValorFgts { get; set; }

    /// <summary>
    /// Total de consignados descontados.
    /// </summary>
    public decimal ValorConsignados { get; set; }

    /// <summary>
    /// Total de descontos (INSS + IRRF + Consignados).
    /// </summary>
    public decimal TotalDescontos { get; set; }

    /// <summary>
    /// Salário líquido.
    /// </summary>
    public decimal SalarioLiquido { get; set; }

    /// <summary>
    /// Total de encargos patronais.
    /// </summary>
    public decimal TotalEncargosPatronais { get; set; }

    /// <summary>
    /// Custo total do funcionário para o empregador.
    /// </summary>
    public decimal CustoTotalEmpregador { get; set; }

    /// <summary>
    /// Timestamp de quando o cálculo foi realizado.
    /// </summary>
    public DateTime CalculadoEm { get; set; }

    // ========================================================================
    // NAVEGAÇÃO
    // ========================================================================

    /// <summary>
    /// Processamento (navegação 1:1).
    /// </summary>
    public virtual ProcessamentoVersaoDb? ProcessamentoVersao { get; set; }

    /// <summary>
    /// Detalhe do INSS (navegação 1:0..1).
    /// </summary>
    public virtual DetalheInssDb? DetalheInss { get; set; }

    /// <summary>
    /// Detalhe do IRRF (navegação 1:0..1).
    /// </summary>
    public virtual DetalheIrrfDb? DetalheIrrf { get; set; }

    /// <summary>
    /// Detalhe do FGTS (navegação 1:0..1).
    /// </summary>
    public virtual DetalheFgtsDb? DetalheFgts { get; set; }

    /// <summary>
    /// Detalhe dos Consignados (navegação 1:0..1).
    /// </summary>
    public virtual DetalheConsignadosDb? DetalheConsignados { get; set; }
}
