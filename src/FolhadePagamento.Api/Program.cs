using FolhadePagamento.Api.Autorizacao;
using FolhadePagamento.Api.Extensoes;
using FolhadePagamento.Api.Middlewares;
using FolhadePagamento.Infra;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURAÇÃO DE SERVIÇOS
// ============================================================================

// Controllers
builder.Services.AddControllers();

// Infraestrutura (EF Core + Repositórios)
builder.Services.AdicionarInfraestrutura(
    builder.Configuration.GetConnectionString("FolhaPagamento")
    ?? "Server=localhost;Database=FolhaPagamento;Trusted_Connection=True;TrustServerCertificate=True;");

// Autenticação JWT
builder.Services.AdicionarAutenticacaoJwt(builder.Configuration);

// Autorização RBAC (Papéis e Policies)
builder.Services.AdicionarAutorizacaoRbac();

// Versionamento de API
builder.Services.AdicionarVersionamento();

// Swagger
builder.Services.AdicionarSwagger();

// CORS
builder.Services.AdicionarCors();

// ============================================================================
// CONSTRUIR APP
// ============================================================================

var app = builder.Build();

// ============================================================================
// CONFIGURAÇÃO DO PIPELINE
// ============================================================================

// Middleware de Auditoria (deve vir cedo no pipeline)
app.UseAuditoria();

// Swagger (apenas em desenvolvimento)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Folha de Pagamento API v1");
        options.RoutePrefix = string.Empty; // Swagger na raiz
    });
}

// HTTPS Redirection
app.UseHttpsRedirection();

// CORS
app.UseCors(app.Environment.IsDevelopment() ? "Desenvolvimento" : "Producao");

// Autenticação e Autorização
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// ============================================================================
// INICIAR APLICAÇÃO
// ============================================================================

app.Run();

// Permitir testes de integração
public partial class Program { }
