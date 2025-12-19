# FolhadePagamento.Api

API REST para o Sistema de Folha de Pagamento.

## 📋 Visão Geral

Esta é a camada de apresentação HTTP do sistema de folha de pagamento, implementada com ASP.NET Core 8.0. Segue os princípios da Clean Architecture:

- **Controller NÃO calcula**
- **Controller NÃO usa DbContext diretamente**
- **Controller chama Casos de Uso e Repositórios**

## 🏗️ Estrutura

```
FolhadePagamento.Api/
├── FolhadePagamento.Api.csproj
├── Program.cs                         # Entry point + configuração
├── appsettings.json                   # Configurações produção
├── appsettings.Development.json       # Configurações desenvolvimento
├── Properties/
│   └── launchSettings.json            # Perfis de execução
├── Configuracoes/
│   └── JwtSettings.cs                 # Configurações JWT
├── Servicos/
│   └── JwtService.cs                  # Serviço de tokens
├── DTOs/
│   └── ApiDTOs.cs                     # Requests e Responses
├── Extensoes/
│   └── ApiExtensoes.cs                # Extensions para DI
└── Controllers/
    └── V1/
        ├── AutenticacaoController.cs  # Login
        ├── FuncionariosController.cs  # CRUD funcionários
        └── ProcessamentosController.cs # Processamento folha
```

## 🔐 Autenticação

### Obter Token

```bash
POST /api/v1/autenticacao/login
Content-Type: application/json

{
  "usuario": "admin",
  "senha": "admin123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiraEm": "2024-01-01T12:00:00Z",
  "tipoToken": "Bearer"
}
```

### Usar Token

```bash
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

## 📡 Endpoints

### Autenticação

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `/api/v1/autenticacao/login` | Autentica e retorna token |
| GET | `/api/v1/autenticacao/verificar` | Valida token atual |

### Funcionários

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/v1/funcionarios` | Lista funcionários ativos |
| GET | `/api/v1/funcionarios/{id}` | Obtém funcionário por ID |
| POST | `/api/v1/funcionarios` | Cria novo funcionário |
| PUT | `/api/v1/funcionarios/{id}` | Atualiza funcionário |
| DELETE | `/api/v1/funcionarios/{id}` | Desativa funcionário |
| HEAD | `/api/v1/funcionarios/{id}` | Verifica existência |

### Processamentos

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/v1/processamentos/{id}` | Obtém processamento por ID |
| GET | `/api/v1/processamentos/funcionario/{id}/competencia/{ano}/{mes}` | Versão atual |
| GET | `/api/v1/processamentos/funcionario/{id}/competencia/{ano}/{mes}/historico` | Histórico |
| GET | `/api/v1/processamentos/competencia/{ano}/{mes}` | Lista por competência |
| POST | `/api/v1/processamentos` | Processa folha |
| HEAD | `/api/v1/processamentos/funcionario/{id}/competencia/{ano}/{mes}` | Verifica existência |

## 🚀 Executar

### Desenvolvimento

```bash
cd src/FolhadePagamento.Api
dotnet run
```

Acesse: http://localhost:5100 (Swagger UI)

### Produção

```bash
dotnet run --configuration Release
```

## 🔧 Configuração

### appsettings.json

```json
{
  "ConnectionStrings": {
    "FolhaPagamento": "Server=localhost;Database=FolhaPagamento;..."
  },
  "Jwt": {
    "ChaveSecreta": "MinhaChaveSecretaDeProducao...",
    "Emissor": "FolhadePagamento.Api",
    "Audiencia": "FolhadePagamento.Clientes",
    "ExpiracaoMinutos": 60
  }
}
```

## 📝 Exemplos

### Criar Funcionário

```bash
POST /api/v1/funcionarios
Authorization: Bearer {token}
Content-Type: application/json

{
  "nome": "João Silva",
  "salarioBase": 5000.00,
  "dataAdmissao": "2024-01-01"
}
```

### Processar Folha

```bash
POST /api/v1/processamentos
Authorization: Bearer {token}
Content-Type: application/json

{
  "funcionarioId": "guid-do-funcionario",
  "competenciaAno": 2024,
  "competenciaMes": 1,
  "numeroDependentes": 2
}
```

### Consultar Histórico

```bash
GET /api/v1/processamentos/funcionario/{id}/competencia/2024/1/historico
Authorization: Bearer {token}
```

## 🔄 Versionamento

A API usa versionamento via URL:
- **v1**: `/api/v1/...`

Para adicionar novas versões:
1. Criar nova pasta `Controllers/V2/`
2. Adicionar `[ApiVersion("2.0")]`
3. Documentar no Swagger

## 🧪 Testes

```bash
# Testar autenticação
curl -X POST http://localhost:5100/api/v1/autenticacao/login \
  -H "Content-Type: application/json" \
  -d '{"usuario":"admin","senha":"admin123"}'

# Listar funcionários (com token)
curl http://localhost:5100/api/v1/funcionarios \
  -H "Authorization: Bearer {token}"
```

## 📚 Documentação

- Swagger UI: http://localhost:5100 (em desenvolvimento)
- [INFRASTRUCTURE_DATA_MODEL.md](../../INFRASTRUCTURE_DATA_MODEL.md)
- [Infra README](../FolhadePagamento.Infra/README.md)

## 🔢 Versão

- API: v1.0
- .NET: 8.0
- Core: v0.8
