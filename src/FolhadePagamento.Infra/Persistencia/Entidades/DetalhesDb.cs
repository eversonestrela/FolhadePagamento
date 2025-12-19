namespace FolhadePagamento.Infra.Persistencia.Entidades;

/// <summary>
/// Entidade de persistência para Detalhe do INSS.
/// Mapeada para a tabela dbo.DetalheInss.
/// Memória de cálculo auditável.
/// </summary>
public class DetalheInssDb
{
    public Guid DetalheInssId { get; set; }
    public Guid ResultadoCalculoId { get; set; }
    public decimal BaseCalculo { get; set; }
    public string TabelaIdUsada { get; set; } = string.Empty;
    public decimal AliquotaEfetiva { get; set; }
    public bool TetoAplicado { get; set; }
    public string? ContribuicaoPorFaixaJson { get; set; }

    public virtual ResultadoCalculoDb? ResultadoCalculo { get; set; }
}

/// <summary>
/// Entidade de persistência para Detalhe do IRRF.
/// Mapeada para a tabela dbo.DetalheIrrf.
/// Memória de cálculo auditável.
/// </summary>
public class DetalheIrrfDb
{
    public Guid DetalheIrrfId { get; set; }
    public Guid ResultadoCalculoId { get; set; }
    public decimal BaseCalculo { get; set; }
    public decimal DeducaoInss { get; set; }
    public int NumeroDependentes { get; set; }
    public decimal DeducaoPorDependente { get; set; }
    public string TabelaIdUsada { get; set; } = string.Empty;
    public string? FaixaAplicada { get; set; }
    public decimal AliquotaAplicada { get; set; }
    public decimal ParcelaDedutivelUsada { get; set; }
    public bool Isento { get; set; }

    public virtual ResultadoCalculoDb? ResultadoCalculo { get; set; }
}

/// <summary>
/// Entidade de persistência para Detalhe do FGTS.
/// Mapeada para a tabela dbo.DetalheFgts.
/// Memória de cálculo auditável.
/// </summary>
public class DetalheFgtsDb
{
    public Guid DetalheFgtsId { get; set; }
    public Guid ResultadoCalculoId { get; set; }
    public decimal BaseCalculo { get; set; }
    public string TabelaIdUsada { get; set; } = string.Empty;
    public decimal AliquotaAplicada { get; set; }
    public string TipoContribuinte { get; set; } = "Normal";

    public virtual ResultadoCalculoDb? ResultadoCalculo { get; set; }
}

/// <summary>
/// Entidade de persistência para Detalhe dos Consignados.
/// Mapeada para a tabela dbo.DetalheConsignados.
/// Memória de cálculo auditável.
/// </summary>
public class DetalheConsignadosDb
{
    public Guid DetalheConsignadosId { get; set; }
    public Guid ResultadoCalculoId { get; set; }
    public decimal SalarioBaseConsiderado { get; set; }
    public decimal PercentualMargem { get; set; }
    public decimal MargemTotal { get; set; }
    public decimal MargemUtilizada { get; set; }
    public decimal MargemDisponivel { get; set; }
    public int TotalContratosAtivos { get; set; }
    public string? DescontosJson { get; set; }

    public virtual ResultadoCalculoDb? ResultadoCalculo { get; set; }
}
