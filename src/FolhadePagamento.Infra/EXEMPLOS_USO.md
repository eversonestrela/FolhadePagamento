# Exemplos de Uso - FolhadePagamento.Infra

Este documento demonstra como usar a camada de infraestrutura para persistir e consultar processamentos de folha de pagamento.

## Configuração

### 1. Configurar Serviços (Program.cs ou Startup.cs)

```csharp
using FolhadePagamento.Infra;

var builder = WebApplication.CreateBuilder(args);

// Adicionar infraestrutura com connection string
builder.Services.AdicionarInfraestrutura(
    builder.Configuration.GetConnectionString("FolhaPagamento")!);

var app = builder.Build();
```

### 2. Connection String (appsettings.json)

```json
{
  "ConnectionStrings": {
    "FolhaPagamento": "Server=localhost;Database=FolhaPagamento;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

## Exemplos de Uso

### Salvar Novo Processamento

```csharp
using FolhadePagamento.Aplicacao.Portas;

public class ProcessarFolhaService
{
    private readonly IProcessamentoRepositorio _repositorio;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public ProcessarFolhaService(
        IProcessamentoRepositorio repositorio,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _repositorio = repositorio;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    public async Task ProcessarAsync(Guid funcionarioId, int ano, int mes)
    {
        // 1. Iniciar transação
        await _unidadeDeTrabalho.IniciarTransacaoAsync();

        try
        {
            // 2. Obter próximo número de versão
            var versao = await _repositorio.ObterProximoNumeroVersaoAsync(
                funcionarioId, ano, mes);

            // 3. Executar cálculos no Core (não mostrado aqui)
            // var resultado = _servicoCalculo.Calcular(funcionario, dependentes, consignados);

            // 4. Montar DTO de persistência
            var processamento = new ProcessamentoPersistencia
            {
                ProcessamentoVersaoId = Guid.NewGuid(),
                FuncionarioId = funcionarioId,
                CompetenciaAno = ano,
                CompetenciaMes = mes,
                VersaoNumero = versao,
                VersaoAnteriorId = null, // primeira versão
                Status = "Finalizado",
                IniciadoEm = DateTime.UtcNow,
                FinalizadoEm = DateTime.UtcNow,
                Resultado = new ResultadoPersistencia
                {
                    ResultadoCalculoId = Guid.NewGuid(),
                    SalarioBruto = 10000m,
                    ValorInss = 1000m,
                    ValorIrrf = 500m,
                    ValorFgts = 800m,
                    ValorConsignados = 200m,
                    TotalDescontos = 1700m, // INSS + IRRF + Consignados
                    SalarioLiquido = 8300m,
                    TotalEncargosPatronais = 800m, // FGTS
                    CustoTotalEmpregador = 10800m,
                    CalculadoEm = DateTime.UtcNow,
                    DetalheInss = new DetalheInssPersistencia
                    {
                        DetalheInssId = Guid.NewGuid(),
                        BaseCalculo = 10000m,
                        TabelaIdUsada = "INSS_2024",
                        AliquotaEfetiva = 10m,
                        TetoAplicado = false,
                        ContribuicaoPorFaixaJson = "[{\"Faixa\":1,\"Base\":1320,\"Aliquota\":7.5,\"Valor\":99}]"
                    },
                    DetalheIrrf = new DetalheIrrfPersistencia
                    {
                        DetalheIrrfId = Guid.NewGuid(),
                        BaseCalculo = 9000m,
                        DeducaoInss = 1000m,
                        NumeroDependentes = 2,
                        DeducaoPorDependente = 189.59m,
                        TabelaIdUsada = "IRRF_2024",
                        FaixaAplicada = "Faixa 3",
                        AliquotaAplicada = 15m,
                        ParcelaDedutivelUsada = 354.80m,
                        Isento = false
                    }
                }
            };

            // 5. Salvar
            await _repositorio.SalvarProcessamentoAsync(processamento);

            // 6. Confirmar transação
            await _unidadeDeTrabalho.ConfirmarAsync();
        }
        catch
        {
            // 7. Em caso de erro, reverter
            await _unidadeDeTrabalho.ReverterAsync();
            throw;
        }
    }
}
```

### Reprocessamento (Nova Versão)

```csharp
public async Task ReprocessarAsync(
    Guid funcionarioId, 
    int ano, 
    int mes, 
    string motivo,
    string descricao)
{
    await _unidadeDeTrabalho.IniciarTransacaoAsync();

    try
    {
        // 1. Obter versão atual
        var versaoAtual = await _repositorio.ObterVersaoAtualAsync(funcionarioId, ano, mes);

        // 2. Marcar como superada
        if (versaoAtual is not null)
        {
            await _repositorio.MarcarComoSuperadoAsync(
                versaoAtual.ProcessamentoVersaoId,
                DateTime.UtcNow);
        }

        // 3. Obter próximo número de versão
        var novaVersao = await _repositorio.ObterProximoNumeroVersaoAsync(
            funcionarioId, ano, mes);

        // 4. Executar cálculos no Core com novos parâmetros
        // ...

        // 5. Salvar nova versão
        var processamento = new ProcessamentoPersistencia
        {
            ProcessamentoVersaoId = Guid.NewGuid(),
            FuncionarioId = funcionarioId,
            CompetenciaAno = ano,
            CompetenciaMes = mes,
            VersaoNumero = novaVersao,
            VersaoAnteriorId = versaoAtual?.ProcessamentoVersaoId,
            Status = "Finalizado",
            IniciadoEm = DateTime.UtcNow,
            FinalizadoEm = DateTime.UtcNow,
            MotivoReprocessamento = motivo,
            DescricaoReprocessamento = descricao,
            Resultado = // ... resultado do novo cálculo
        };

        await _repositorio.SalvarProcessamentoAsync(processamento);

        await _unidadeDeTrabalho.ConfirmarAsync();
    }
    catch
    {
        await _unidadeDeTrabalho.ReverterAsync();
        throw;
    }
}
```

### Consultas

```csharp
// Obter versão atual
var atual = await _repositorio.ObterVersaoAtualAsync(funcionarioId, 2024, 1);

// Obter histórico de versões
var historico = await _repositorio.ObterHistoricoVersoesAsync(funcionarioId, 2024, 1);

// Listar folha da competência
var folhaMes = await _repositorio.ListarPorCompetenciaAsync(2024, 1);

// Verificar se já existe processamento
var existe = await _repositorio.ExisteProcessamentoAsync(funcionarioId, 2024, 1);
```

## Fluxo de Reprocessamento

```
┌─────────────────────────────────────────────────────────────────┐
│                    FLUXO DE REPROCESSAMENTO                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. Obter Versão Atual                                          │
│     └─> ObterVersaoAtualAsync(func, ano, mes)                   │
│                                                                 │
│  2. Marcar como Superada                                        │
│     └─> MarcarComoSuperadoAsync(id, dataHora)                   │
│     └─> Status: "Finalizado" → "Superado"                       │
│     └─> SuperadoEm: data/hora atual                             │
│                                                                 │
│  3. Calcular Nova Versão (Core)                                 │
│     └─> Nenhuma lógica de cálculo na Infra!                     │
│                                                                 │
│  4. Obter Próximo Número                                        │
│     └─> ObterProximoNumeroVersaoAsync(func, ano, mes)           │
│     └─> Ex: V1 → V2                                             │
│                                                                 │
│  5. Salvar Nova Versão                                          │
│     └─> SalvarProcessamentoAsync(processamento)                 │
│     └─> VersaoAnteriorId = ID da V1                             │
│                                                                 │
│  Estado Final:                                                  │
│  ┌──────────┐         ┌──────────┐                              │
│  │   V1     │◄────────│   V2     │                              │
│  │ Superado │         │Finalizado│                              │
│  └──────────┘         └──────────┘                              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Regras Importantes

1. **Sem Regras de Negócio**: A Infra apenas persiste valores já calculados
2. **Core é a Verdade**: Todos os cálculos vêm do Core
3. **Imutabilidade**: Processamentos finalizados não são alterados (exceto Status para Superado)
4. **Transações**: Use IUnidadeDeTrabalho para operações que envolvem múltiplas tabelas
5. **Histórico**: Todas as versões são mantidas para auditoria
