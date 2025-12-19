namespace FolhadePagamento.Api.DTOs;

// ============================================================================
// DTOs DE REQUEST
// ============================================================================

/// <summary>
/// Request para autenticação.
/// </summary>
public record LoginRequest
{
    public required string Usuario { get; init; }
    public required string Senha { get; init; }
}

/// <summary>
/// Request para processar folha de pagamento.
/// </summary>
public record ProcessarFolhaRequest
{
    public required Guid FuncionarioId { get; init; }
    public required int CompetenciaAno { get; init; }
    public required int CompetenciaMes { get; init; }
    
    /// <summary>
    /// Número de dependentes para cálculo de IRRF.
    /// </summary>
    public int NumeroDependentes { get; init; } = 0;
}

/// <summary>
/// Request para reprocessar folha.
/// </summary>
public record ReprocessarFolhaRequest
{
    public required Guid FuncionarioId { get; init; }
    public required int CompetenciaAno { get; init; }
    public required int CompetenciaMes { get; init; }
    public required string Motivo { get; init; }
    public string? Descricao { get; init; }
    public int NumeroDependentes { get; init; } = 0;
}

/// <summary>
/// Request para criar funcionário.
/// </summary>
public record CriarFuncionarioRequest
{
    public required string Nome { get; init; }
    public required decimal SalarioBase { get; init; }
    public DateTime? DataAdmissao { get; init; }
}

/// <summary>
/// Request para atualizar funcionário.
/// </summary>
public record AtualizarFuncionarioRequest
{
    public string? Nome { get; init; }
    public decimal? SalarioBase { get; init; }
    public DateTime? DataAdmissao { get; init; }
}

// ============================================================================
// DTOs DE RESPONSE
// ============================================================================

/// <summary>
/// Response de autenticação.
/// </summary>
public record LoginResponse
{
    public required string Token { get; init; }
    public required DateTime ExpiraEm { get; init; }
    public required string TipoToken { get; init; } = "Bearer";
}

/// <summary>
/// Response padrão para erros.
/// </summary>
public record ErroResponse
{
    public required string Mensagem { get; init; }
    public string? Detalhe { get; init; }
    public string? Codigo { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Response de processamento criado.
/// </summary>
public record ProcessamentoCriadoResponse
{
    public required Guid ProcessamentoVersaoId { get; init; }
    public required int VersaoNumero { get; init; }
    public required string Status { get; init; }
    public required decimal SalarioLiquido { get; init; }
    public required DateTime ProcessadoEm { get; init; }
}

/// <summary>
/// Response de funcionário criado.
/// </summary>
public record FuncionarioCriadoResponse
{
    public required Guid FuncionarioId { get; init; }
    public required string Nome { get; init; }
    public required DateTime CriadoEm { get; init; }
}
