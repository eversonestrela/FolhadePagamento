using FolhadePagamento.Infra;
using FolhadePagamento.Jobs.Servicos;
using FolhadePagamento.Jobs.Workers;

var builder = Host.CreateApplicationBuilder(args);

// ============================================================================
// CONFIGURAÇÃO DE SERVIÇOS
// ============================================================================

// Infraestrutura (EF Core + Repositórios)
builder.Services.AdicionarInfraestrutura(
    builder.Configuration.GetConnectionString("FolhaPagamento")
    ?? "Server=localhost;Database=FolhaPagamento;Trusted_Connection=True;TrustServerCertificate=True;");

// Serviço de processamento
builder.Services.AddScoped<IProcessadorLote, ProcessadorLote>();

// Worker de background
builder.Services.AddHostedService<ProcessadorLoteWorker>();

// ============================================================================
// CONSTRUIR E EXECUTAR
// ============================================================================

var host = builder.Build();
host.Run();
