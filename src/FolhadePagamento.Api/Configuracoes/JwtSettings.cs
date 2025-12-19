namespace FolhadePagamento.Api.Configuracoes;

/// <summary>
/// Configurações para autenticação JWT.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Chave secreta para assinatura do token.
    /// IMPORTANTE: Em produção, usar secrets manager ou Azure Key Vault.
    /// </summary>
    public string ChaveSecreta { get; set; } = string.Empty;

    /// <summary>
    /// Emissor do token (issuer).
    /// </summary>
    public string Emissor { get; set; } = "FolhadePagamento.Api";

    /// <summary>
    /// Audiência do token (audience).
    /// </summary>
    public string Audiencia { get; set; } = "FolhadePagamento.Clientes";

    /// <summary>
    /// Tempo de expiração do token em minutos.
    /// </summary>
    public int ExpiracaoMinutos { get; set; } = 60;
}
