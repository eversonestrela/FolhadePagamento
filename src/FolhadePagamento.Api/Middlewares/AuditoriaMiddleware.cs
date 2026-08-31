using System.Diagnostics;
using System.Security.Claims;

namespace FolhadePagamento.Api.Middlewares;

/// <summary>
/// Middleware para auditoria de acesso aos endpoints.
/// 
/// Registra:
/// - Quem acessou (usuário)
/// - Quando (timestamp)
/// - Qual endpoint (método HTTP + path)
/// - Resultado (status code)
/// - Tempo de resposta
/// </summary>
public class AuditoriaMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditoriaMiddleware> _logger;

    public AuditoriaMiddleware(RequestDelegate next, ILogger<AuditoriaMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var timestamp = DateTime.UtcNow;
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        // Obter informações do request
        var metodo = context.Request.Method;
        var path = context.Request.Path.Value;
        var queryString = context.Request.QueryString.Value;
        var ipOrigem = context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

        // Adicionar correlation ID ao response header
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Obter informações do usuário (pode estar disponível após autenticação)
            var usuarioId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anônimo";
            var nomeUsuario = context.User?.Identity?.Name ?? "desconhecido";
            var estaAutenticado = context.User?.Identity?.IsAuthenticated ?? false;

            var papeis = context.User?.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList() ?? new List<string>();

            var statusCode = context.Response.StatusCode;
            var tempoMs = stopwatch.ElapsedMilliseconds;

            // Determinar nível de log baseado no status
            var logLevel = statusCode switch
            {
                >= 500 => LogLevel.Error,
                >= 400 => LogLevel.Warning,
                _ => LogLevel.Information
            };

            // Log estruturado
            _logger.Log(
                logLevel,
                "[AUDITORIA] {CorrelationId} | {Timestamp:yyyy-MM-dd HH:mm:ss.fff} | " +
                "{Usuario} ({UsuarioId}) | Autenticado: {Autenticado} | Papéis: [{Papeis}] | " +
                "{Metodo} {Path}{Query} | Status: {StatusCode} | {TempoMs}ms | IP: {IP}",
                correlationId,
                timestamp,
                nomeUsuario,
                usuarioId,
                estaAutenticado,
                string.Join(", ", papeis),
                metodo,
                path,
                queryString,
                statusCode,
                tempoMs,
                ipOrigem);

            // Log adicional para acessos não autorizados
            if (statusCode == 401)
            {
                _logger.LogWarning(
                    "[SEGURANÇA] Tentativa de acesso não autenticado | {CorrelationId} | " +
                    "{Metodo} {Path} | IP: {IP}",
                    correlationId, metodo, path, ipOrigem);
            }
            else if (statusCode == 403)
            {
                _logger.LogWarning(
                    "[SEGURANÇA] Acesso negado (sem permissão) | {CorrelationId} | " +
                    "{Usuario} ({UsuarioId}) | Papéis: [{Papeis}] | {Metodo} {Path} | IP: {IP}",
                    correlationId, nomeUsuario, usuarioId, string.Join(", ", papeis), 
                    metodo, path, ipOrigem);
            }
        }
    }
}

/// <summary>
/// Extensões para adicionar o middleware de auditoria.
/// </summary>
public static class AuditoriaMiddlewareExtensions
{
    /// <summary>
    /// Adiciona o middleware de auditoria de acesso ao pipeline.
    /// </summary>
    public static IApplicationBuilder UseAuditoria(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuditoriaMiddleware>();
    }
}
