# Folha de Pagamento - Front-End

SPA em React + TypeScript que consome a API de Folha de Pagamento.

## ⚠️ Importante

**Este front-end NÃO contém lógica de negócio.**

- ❌ Não calcula INSS, IRRF, FGTS
- ❌ Não conhece regras de cálculo
- ✅ Apenas exibe dados da API
- ✅ Respeita RBAC definido na API

## 🛠️ Tecnologias

- React 18.2
- TypeScript 5.2
- Vite 5.0
- Axios 1.6
- React Router DOM 6.21

## 📁 Estrutura

```
src/
├── components/       # Componentes reutilizáveis
│   ├── Layout.tsx    # Layout principal
│   └── RotaProtegida.tsx
├── contexts/         # Contextos React
│   └── AuthContext.tsx
├── pages/            # Páginas da aplicação
│   ├── Dashboard/
│   ├── Funcionarios/
│   ├── Lotes/
│   ├── Login/
│   └── Processamentos/
├── services/         # Serviços de API
│   ├── apiClient.ts
│   ├── autenticacaoService.ts
│   ├── funcionarioService.ts
│   ├── loteService.ts
│   └── processamentoService.ts
└── types/            # Tipos TypeScript
    └── api.ts
```

## 🚀 Execução Local

### Pré-requisitos

- Node.js 18+ 
- npm ou yarn
- API rodando em `https://localhost:7001`

### Instalação

```bash
# Instalar dependências
npm install

# Copiar arquivo de ambiente
cp .env.example .env

# Iniciar servidor de desenvolvimento
npm run dev
```

Acesse: http://localhost:5173

## 🐳 Execução com Docker

### Desenvolvimento (com hot reload)

```bash
# Subir container de desenvolvimento
docker compose up

# Ou em background
docker compose up -d
```

Acesse: http://localhost:5173

### Rebuild após alterações no package.json

```bash
docker compose build --no-cache
docker compose up
```

## 🔐 Autenticação

O sistema usa JWT para autenticação. Token é armazenado em `sessionStorage`.

### Credenciais de Demonstração

| Papel | Login | Senha |
|-------|-------|-------|
| Administrador | admin | admin123 |
| Operador | operador | operador123 |
| Consulta | consulta | consulta123 |

## 📋 Telas

### Login
- Formulário de autenticação
- Exibe credenciais de demo
- Trata sessão expirada

### Dashboard
- Cards de resumo
- Lotes ativos com progresso
- Ações rápidas por papel
- Auto-refresh a cada 10s

### Funcionários
- Listagem com busca
- CRUD completo (baseado em permissões)
- Modal para criar/editar

### Processamentos
- Filtro por competência
- Listagem de processamentos
- Detalhe com cálculos (exibidos da API)

### Lotes
- Listagem por competência
- Detalhe com itens
- Barra de progresso
- Cancelamento (apenas admin)

## 🔒 RBAC

O front-end respeita as permissões definidas na API:

| Papel | Permissões |
|-------|------------|
| Administrador | Todas |
| Operador | Criar/listar funcionários, criar/listar lotes |
| Consulta | Apenas visualização |

Botões e ações são exibidos condicionalmente baseado nas permissões.

## ⚙️ Variáveis de Ambiente

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| VITE_API_URL | URL base da API | https://localhost:7001 |

## 📦 Build de Produção

```bash
npm run build
```

Arquivos gerados em `dist/`.

## 🧪 Lint

```bash
npm run lint
```

## 📝 Licença

MIT
