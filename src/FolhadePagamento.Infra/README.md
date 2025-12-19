# FolhadePagamento.Infra

Camada de Infraestrutura para persistência de dados de folha de pagamento no SQL Server.

## 📋 Visão Geral

Esta camada implementa o padrão Repository para persistência dos resultados de cálculo de folha de pagamento. Segue os princípios da Clean Architecture, onde:

- **Infra NÃO contém regras de negócio**
- **Core é a única fonte de verdade**
- **Apenas persiste e consulta dados**

## 🏗️ Estrutura

```
FolhadePagamento.Infra/
├── FolhadePagamento.Infra.csproj
├── InfraExtensoes.cs                    # Extensões para DI
├── EXEMPLOS_USO.md                      # Guia de uso
├── README.md                            # Este arquivo
└── Persistencia/
    ├── FolhaDbContext.cs                # DbContext EF Core
    ├── Entidades/                       # Entidades de persistência
    │   ├── FuncionarioDb.cs
    │   ├── ProcessamentoVersaoDb.cs
    │   ├── ResultadoCalculoDb.cs
    │   └── DetalhesDb.cs
    ├── Configuracoes/                   # Fluent API configurations
    │   ├── FuncionarioDbConfiguration.cs
    │   ├── ProcessamentoVersaoDbConfiguration.cs
    │   ├── ResultadoCalculoDbConfiguration.cs
    │   └── DetalhesDbConfiguration.cs
    └── Repositorios/                    # Implementações
        ├── ProcessamentoRepositorio.cs
        ├── FuncionarioRepositorio.cs
        └── UnidadeDeTrabalho.cs
```

## 📊 Modelo de Dados

```
┌─────────────────┐     ┌─────────────────────┐     ┌──────────────────┐
│   Funcionario   │────<│ ProcessamentoVersao │────<│  ResultadoCalculo│
│     (1:N)       │     │        (1:1)        │     │       (1:1)      │
└─────────────────┘     └─────────────────────┘     └──────────────────┘
                                  │                          │
                                  │                          ├───< DetalheInss
                                  │                          ├───< DetalheIrrf
                                  │ self-ref                 ├───< DetalheFgts
                                  │ VersaoAnteriorId         └───< DetalheConsignados
                                  ▼
                        ┌─────────────────────┐
                        │ ProcessamentoVersao │
                        │     (versão N-1)    │
                        └─────────────────────┘
```

## 🔧 Configuração

### 1. Adicionar ao Program.cs

```csharp
using FolhadePagamento.Infra;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AdicionarInfraestrutura(
    builder.Configuration.GetConnectionString("FolhaPagamento")!);
```

### 2. Connection String

```json
{
  "ConnectionStrings": {
    "FolhaPagamento": "Server=localhost;Database=FolhaPagamento;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3. Criar Banco de Dados

Execute os scripts DDL na ordem:
1. `infra/database/ddl/001_Funcionario.sql`
2. `infra/database/ddl/002_ProcessamentoVersao.sql`
3. `infra/database/ddl/003_ResultadoCalculo.sql`
4. `infra/database/ddl/004_DetalheInss.sql`
5. `infra/database/ddl/005_DetalheIrrf.sql`
6. `infra/database/ddl/006_DetalheFgts.sql`
7. `infra/database/ddl/007_DetalheConsignados.sql`
8. `infra/database/ddl/008_AuditLog.sql`

Ou use o script consolidado:
```sql
-- Executar no SQL Server
:r 000_Script_Consolidado.sql
```

## 📝 Interfaces (Aplicação)

| Interface | Descrição |
|-----------|-----------|
| `IProcessamentoRepositorio` | Persistência e consulta de processamentos |
| `IFuncionarioRepositorio` | Persistência e consulta de funcionários |
| `IUnidadeDeTrabalho` | Gerenciamento de transações |

## 🔄 Fluxo de Persistência

```
Core (Domínio)        →    Aplicação           →    Infra
────────────────          ──────────────          ──────────────
CalcularFolha()     →    UsarResultado()     →    SalvarAsync()
                          ↓                       ↓
Resultado (VO)      →    DTO Persistência    →    Entidade Db
```

## ✅ Regras Importantes

| Regra | Descrição |
|-------|-----------|
| Sem Cálculos | Infra não executa nenhum cálculo |
| Valores Prontos | Todos os valores vêm do Core já calculados |
| Imutabilidade | Processamentos finalizados não são alterados |
| Versionamento | Novas versões não alteram as anteriores |
| Auditoria | Todas as versões são mantidas |

## 📦 Dependências

- .NET 8.0
- Microsoft.EntityFrameworkCore.SqlServer 8.0.x
- Microsoft.EntityFrameworkCore.Design 8.0.x
- Microsoft.Extensions.DependencyInjection.Abstractions

## 🧪 Testes

Os testes de integração para esta camada devem usar:
- SQL Server LocalDB ou container Docker
- Test fixtures com cleanup automático

```csharp
// Exemplo de setup para testes
services.AddDbContext<FolhaDbContext>(options =>
    options.UseInMemoryDatabase("TestDb"));
```

## 📚 Documentação Relacionada

- [INFRASTRUCTURE_DATA_MODEL.md](../../INFRASTRUCTURE_DATA_MODEL.md) - Modelo conceitual
- [DDL Scripts](../../infra/database/ddl/README.md) - Scripts de banco
- [EXEMPLOS_USO.md](EXEMPLOS_USO.md) - Exemplos de código

## 🔢 Versão

- Core: v0.8 (Versionamento de Processamento)
- Infra: v0.1.0
