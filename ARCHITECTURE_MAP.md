# ARCHITECTURE MAP - Sistema de Folha de Pagamento

**Versão:** 1.0  
**Data:** Dezembro 2025  
**Status:** Documento Oficial - Fonte da Verdade do Projeto  
**Escopo:** Arquitetura do sistema de folha de pagamento complexo (Monólito Modular Clean Architecture + DDD)

---

## 1. Visão Geral do Sistema

### 1.1 Objetivo do Software

Este sistema é uma **plataforma integrada de processamento de folha de pagamento** projetada para empresas de médio a grande porte, com capacidade de lidar com complexidades tributárias, consignações e integração com ecossistemas governamentais (eSocial, INSS, Banco Central) e contábeis.

**Não é um CRUD simples porque:**

- **Determinismo Crítico:** Cálculos devem ser reproduzíveis byte-a-byte, respeitando vigências de regras e legislação
- **Imutabilidade de Resultados:** Uma folha processada não pode ser alterada retroativamente; apenas reprocessada com rastreamento completo
- **Complexidade Tributária:** IRRF com alíquotas progressivas, INSS com limites de contribuição, FGTS com regras de deposito, múltiplos consignados com margens
- **Histórico Legal:** Cada processamento é um artefato imutável que servirá em auditorias, reclamações trabalhistas e contencioso fiscal
- **Processamento Assíncrono em Escala:** Competências de centenas de funcionários não podem ser calculadas em requisição HTTP síncrona
- **Versionamento de Cálculo:** Legislação muda. O sistema deve permitir recálculos retroativos sem perder a memória do que foi processado quando

### 1.2 Público-alvo

- Departamentos de RH/Folha de empresas médias e grandes
- Consultores e auditores (validação de cálculos)
- Contadores (integração contábil)
- Órgãos reguladores (conformidade eSocial)
- Analistas de dados (relatórios, inteligência de negócio)

### 1.3 Filosofia Arquitetural

Este sistema segue os princípios de:

- **Clean Architecture:** Regras de negócio isoladas de frameworks, bancos de dados e UI
- **Domain-Driven Design (DDD):** Estrutura organizada por domínios de negócio (Cadastro, Eventos, Processamento, Tributos, Consignados)
- **Monólito Modular:** Única solução entregável, mas com limites claros entre módulos para permitir evolução futura para microsserviços
- **Determinismo:** Cálculos são funções puras - mesma entrada sempre gera mesma saída
- **Rastreabilidade:** Cada decisão, cálculo e transformação é registrada e auditável
- **Versionamento Semântico:** Processamentos podem ser retomados, corrigidos e rastreados através de versões

### 1.4 Por que não é simples

| Aspecto | CRUD Simples | Folha de Pagamento |
|---------|-------------|-------------------|
| Persistência | Salvar dados | Calcular, auditar, versionar, reprocessar |
| Modificação | Editar a qualquer hora | Imutabilidade com versionamento |
| Lógica | Regras em 1-2 níveis | Regras em árvore por vigência, cascata de cálculos |
| Concorrência | Isolada por usuário | Controle de competência por empresa |
| Confiabilidade | Aceitável com falhas | Crítica: erros têm impacto legal |
| Integração | Opcional | Obrigatória: eSocial, contábil, bancos |
| Conformidade | Business | Legal/Trabalhista/Fiscal |

---

## 2. Princípios Arquiteturais Obrigatórios

Estes princípios **não são sugestões**. Toda implementação deve respeitá-los.

### 2.1 Separação de Responsabilidades

**PROIBIDO:**
- Regras de negócio em controllers, views ou DAOs
- Banco de dados diretamente em casos de uso
- UI manipulando dados de cálculo

**OBRIGATÓRIO:**
- Regras de negócio em **Entidades** e **Aggregate Roots** do domínio
- Casos de uso em **Application Services** orquestrando o domínio
- Infraestrutura (BD, cache, APIs externas) acessível via **Ports** (interfaces)
- Apresentação (API, UI) conversando com Application Services

**Estrutura:**
```
Domain Layer (regras puras)
    ↓
Application Layer (orquestração)
    ↓
Infrastructure Layer (persistência, APIs, jobs)
    ↓
Presentation Layer (API, UI)
```

### 2.2 Regra de Negócio Isolada de UI e Infraestrutura

**Princípio:** A lógica que calcula IRRF, INSS, consignados deve rodar identicamente em:
- API HTTP
- Job assíncrono
- Teste unitário
- Relatório em background

**Implementação:**
- **Domain Services** contêm toda lógica de cálculo, sem dependências externas
- **Application Services** injetam dependências (repositories, calculators) como abstrações
- **Infrastructure** implementa essas abstrações (SQL Server, cache, APIs)
- **Presentation** nunca toca em lógica de cálculo

### 2.3 Determinismo no Cálculo

**Definição:** Dados de entrada idênticos sempre geram saída idêntica, independentemente de:
- Data/hora do processamento
- Máquina executando o cálculo
- Versão do banco de dados

**Como garantir:**
- Não usar `DateTime.Now` em cálculos; usar data de competência
- Não usar valores aleatórios ou IDs sequenciais dependentes de BD
- Não usar HTTP calls ou APIs externas dentro de cálculos
- Testes devem validar entrada → saída fixa

**Exceção:** Buscar vigências de regras do BD é aceito, pois a regra é determinística para um momento do tempo.

### 2.4 Versionamento de Processamento

**Conceito:** Cada processamento de uma competência é uma versão imutável.

**Regras:**
- Processamento V1 de Jan/2025: resultado imutável
- Se há erro, gera Processamento V2, mantendo V1 como histórico
- V2 pode recalcular com legislação atualizada
- Relatórios podem mostrar V1 ou V2, mas rastreiam qual versão usaram

**Implementação:**
- Tabela de `Processamentos` com `versao`, `data_processamento`, `usuario`
- Tabelas de resultados (`ProcessamentoRubricas`, `ProcessamentoDescontos`) ligadas a `ProcessamentoId`
- Nunca deletar; apenas marcar como superado

### 2.5 Imutabilidade de Resultados

**Regra:** Uma vez processado e finalizado, um resultado não pode ser modificado ou deletado.

**Operações permitidas:**
- Consultar resultado
- Gerar nova versão (reprocessamento)
- Auditar histórico

**Operações PROIBIDAS:**
- UPDATE em tabelas de resultado
- DELETE em tabelas de resultado
- Modificar cálculos já salvos

**Implementação:**
- Tabelas de resultado com `IsFinal` ou `Status = Finalized`
- Soft-delete com `DataCancelamento` se necessário
- Índices para evitar queries em tabelas deletadas

### 2.6 Auditoria e Rastreabilidade

**Tudo que deve ser auditado:**
- Que configuração de regra foi usada (qual vigência)
- Qual entrada gerou qual saída
- Quem processou, quando, com qual versão do sistema
- Se houve reprocessamento, qual era o anterior
- Alterações em cadastros (Employee, Rubric)

**Implementação:**
- **Event Sourcing opcional:** Registrar todos os eventos de mudança
- **Audit Log obrigatório:** Tabela `AuditLogs` com `EntityId`, `EntityType`, `OldValue`, `NewValue`, `UsuarioId`, `Timestamp`
- **Calculation Memory:** Tabela `MemoriaCalculo` registrando cada passo do cálculo com valor intermediário

### 2.7 Idempotência

**Definição:** Processar uma competência N vezes deve gerar o mesmo resultado final.

**Usar para:**
- Retomar jobs interrompidos
- Reprocessar sem efeitos colaterais
- Garantir que falhas não deixem dados inconsistentes

**Implementação:**
- Usar chave única em operações de cálculo: `(CompanyId, EmployeeId, CompetenceMonth, ProcessingVersion)`
- Verificar se processamento já existe antes de criar novo
- Usar transações ACID para garantir atomicidade

---

## 3. Estrutura da Solução (Solution Structure)

### 3.1 Organização de Projetos

```
FolhadePagamento.sln
│
├── FolhadePagamento.Domain/                    # Camada de Domínio
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Aggregates/
│   ├── DomainServices/
│   └── Events/
│
├── FolhadePagamento.Application/               # Camada de Aplicação
│   ├── UseCases/
│   ├── DTOs/
│   ├── Mappers/
│   ├── Validators/
│   └── Ports/  (Interfaces de adaptadores)
│
├── FolhadePagamento.Infrastructure/            # Camada de Infraestrutura
│   ├── Persistence/  (EF Core, Repositories)
│   ├── Services/     (Integrações externas)
│   ├── Jobs/         (Hangfire/Quartz)
│   ├── Cache/
│   └── Adapters/     (Implementação de Ports)
│
├── FolhadePagamento.API/                       # Camada de Apresentação (Web API)
│   ├── Controllers/
│   ├── Middlewares/
│   └── Configuration/
│
├── FolhadePagamento.Desktop/                   # Front-end (Blazor Hybrid)
│   ├── Pages/
│   ├── Components/
│   ├── Services/
│   └── Models/
│
└── FolhadePagamento.Tests/                     # Testes
    ├── UnitTests/
    ├── IntegrationTests/
    └── ArchitectureTests/
```

### 3.2 Projeto: FolhadePagamento.Domain

**Responsabilidade:** Definir TODA a lógica de negócio, sem dependências externas.

**O que PODE existir:**
- Entidades (Employee, Rubric, Discount, etc.)
- Value Objects (Money, Percentage, DocumentNumber)
- Aggregate Roots (ProcessingAggregate)
- Domain Services (CalculationEngine, TaxCalculator)
- Domain Events (EmployeeCreated, ProcessingCompleted)
- Enums e constantes do negócio
- Interfaces de Ports (IEmployeeRepository, ITaxRuleProvider)

**O que NÃO pode existir:**
- DbContext ou qualquer ORM
- HttpClient ou chamadas externas
- UI components ou ViewModels
- Framework específico (ASP.NET, EF Core)
- DateTime.Now ou Random em cálculos
- Logging ou instrumentação direta
- Transações de banco de dados

**Exemplo conceitual:**
```csharp
// CERTO - Lógica pura
namespace FolhadePagamento.Domain.Payroll;

public class SalaryCalculator
{
    public CalculationResult Calculate(
        Employee employee,
        IList<RubricValue> rubrics,
        ITaxRuleProvider taxRules,  // Abstração, não implementação
        Competence competence)
    {
        // Calcula proventos
        // Aplica descontos
        // Calcula IRRF com regra vigente
        // Retorna resultado determinístico
    }
}

// ERRADO - Não deve estar aqui
public class SalaryCalculator
{
    var taxes = await _httpClient.GetTaxRulesAsync();  // ❌
    var now = DateTime.Now;                             // ❌
    _logger.LogInformation(...);                         // ❌
    _context.Calculations.Add(result);                   // ❌
}
```

### 3.3 Projeto: FolhadePagamento.Application

**Responsabilidade:** Orquestrar o domínio, gerenciar transações, coordenar infraestrutura.

**O que PODE existir:**
- Use Cases / Application Services (ProcessPayrollUseCase)
- Validators (FluentValidation)
- DTOs para entrada/saída
- Mappers (AutoMapper ou manual)
- Ports (interfaces de adaptadores)
- Query Handlers (CQRS opcional)
- Orchestration logic

**O que NÃO pode existir:**
- Lógica de negócio complexa (deve estar em Domain)
- Acesso direto a BD (usar repositories)
- Framework específico
- UI concerns

**Estrutura típica de um Use Case:**
```csharp
public class ProcessPayrollUseCase
{
    private readonly IEmployeeRepository _employees;
    private readonly IProcessingRepository _processing;
    private readonly SalaryCalculator _calculator;
    private readonly ITaxRuleProvider _taxRules;
    
    public async Task<ProcessingResult> Execute(ProcessPayrollCommand cmd)
    {
        // 1. Validar entrada
        // 2. Carregar agregados (Employee, ProcessingAggregate)
        // 3. Delegar ao Domain Service (SalaryCalculator)
        // 4. Salvar resultado via repository
        // 5. Publicar eventos
        // 6. Retornar DTO
    }
}
```

### 3.4 Projeto: FolhadePagamento.Infrastructure

**Responsabilidade:** Implementar persistência, acesso a APIs externas, jobs assíncronos.

**O que PODE existir:**
- DbContext (Entity Framework Core)
- Repositories (implementação de Port/Interface)
- Services externos (eSocial, bancos, APIs)
- Job handlers (Hangfire, Quartz)
- Cache (Redis, em memória)
- File uploads/downloads
- Email/SMS senders

**O que NÃO pode existir:**
- Lógica de negócio (deve vir da Domain via injeção)
- Controllers ou Endpoints
- UI components

**Padrão obrigatório - Repository:**
```csharp
// Implementação de port definida em Application
public class EmployeeRepository : IEmployeeRepository
{
    private readonly PayrollDbContext _context;
    
    public async Task<Employee> GetByIdAsync(EmployeeId id)
    {
        // Buscar do BD, mapear para entidade de domínio
        // Retornar agregado completo
    }
    
    public async Task SaveAsync(Employee employee)
    {
        // Salvar mutações do agregado
        // Respeitar imutabilidade de resultados
    }
}
```

### 3.5 Projeto: FolhadePagamento.API

**Responsabilidade:** Expor endpoints HTTP, validar requisições, rotear para Application Services.

**O que PODE existir:**
- Controllers (ASP.NET Core)
- Middlewares (autenticação, logging, tratamento de erros)
- Filtros (validação de entrada)
- Authorization policies
- OpenAPI/Swagger configuration
- Health checks

**O que NÃO pode existir:**
- Lógica de negócio
- Acesso a BD
- Implementação de algoritmos complexos

**Responsabilidade de cada Controller:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class PayrollController
{
    private readonly IProcessPayrollUseCase _useCase;
    
    [HttpPost]
    public async Task<IActionResult> ProcessPayroll(ProcessPayrollRequest request)
    {
        // 1. Validar request (formato, autorização)
        // 2. Mapear para comando de domínio
        // 3. Invocar use case
        // 4. Retornar resposta HTTP
    }
}
```

### 3.6 Projeto: FolhadePagamento.Desktop (Blazor Hybrid)

**Responsabilidade:** Apresentar interface para usuários, capturar entrada, comunicar com API.

**O que PODE existir:**
- Componentes Blazor reutilizáveis
- Páginas de administração, processamento, consulta
- Serviços de comunicação com API (HttpClient wrapper)
- Estado local (PreRender, componentes)
- Validação de entrada frontend
- Formatação de dados para apresentação

**O que NÃO pode existir:**
- Lógica de cálculo de folha
- Persistência direta em BD
- Regras de negócio complexas
- Acesso a APIs externas (deve ir via API backend)

**Comunicação obrigatória:**
```
Desktop (UI)
    ↓ (HTTP)
API (Controllers)
    ↓ (injeção)
Application (Use Cases)
    ↓ (injeção)
Domain (Lógica pura)
```

### 3.7 Projeto: FolhadePagamento.Tests

**Responsabilidade:** Validar comportamento em todas as camadas.

**Estrutura:**
- **UnitTests:** Testes de Domain Services, Value Objects, algoritmos
- **IntegrationTests:** Testes de end-to-end com BD real
- **ArchitectureTests:** Validar regras arquiteturais (ex: Domain não importa Infrastructure)

---

## 4. Organização por Domínios (Módulos)

O sistema é organizado em **domínios de negócio**, cada um com responsabilidades claras e limites bem definidos.

### 4.1 Domínio: CADASTRO (Foundations)

**Responsabilidade:** Manter dados mestres que alimentam o cálculo (Employee, Company, Legal Entity, Bank Data).

**O que PODE existir:**
- Entidades: Employee, Company, LegalEntity, BankAccount
- Value Objects: DocumentNumber (CPF/CNPJ), Phone, Address
- Services: EmployeeValidator, DocumentValidator
- Repositories para dados mestres
- Aggregates: EmployeeAggregate (Employee + dados relacionados)

**O que NÃO pode existir:**
- Dados calculados (folha, descontos)
- Lógica de tributos
- Cálculos de margens consignadas
- Histórico de processamento

**Dados que DEVEM ser imutáveis uma vez processados:**
- DocumentNumber (CPF não muda)
- Datas históricas (data de admissão, data de nascimento)

**Dados que PODEM ser alterados:**
- Banco e conta (para próximas folhas)
- Endereço, telefone
- Status (ativo/inativo)

**Limitações:**
- Não pode deletar Employee sem marcar como inativo
- Não pode alterar dados de um Employee que já foi processado em competências passadas

### 4.2 Domínio: EVENTOS / RUBRICAS (Earnings and Deductions)

**Responsabilidade:** Definir o que compõe a folha (Rubrics), suas fórmulas e vigências.

**O que PODE existir:**
- Entidades: Rubric, RubricFormula, RubricVigence
- Value Objects: RubricCode, RubricType (EARNING, DISCOUNT, TAX)
- Tipos de Rubricas: Salary, Bonus, Overtime, IRRF, INSS, FGTS, HealthInsurance, DentalInsurance
- Services: RubricFormulaCalculator
- Repositórios de Rubricas

**O que NÃO pode existir:**
- Valores específicos de um employee (valores calculados)
- Lógica de processamento global
- Dados de uma competência específica

**Estrutura de Rubrica:**
```
Rubric
├── RubricId
├── Code (e.g., "SAL", "IRRF", "FGTS")
├── Name
├── Type (Earning/Discount/Tax)
├── CalculationMode (Fixed, Percentage, Formula)
├── Vigences[]  // Histórico de quando esta rubrica era válida
│   ├── StartDate
│   ├── EndDate
│   └── Formula (ou referência a cálculo)
└── IsActive
```

**Vigências:** Uma Rubrica pode ter múltiplas vigências com fórmulas diferentes.
- Exemplo: IRRF muda de alíquota em abril → nova vigência
- Ao processar jan/2025, usa vigência de jan/2025
- Ao processar abr/2025, usa vigência de abr/2025

### 4.3 Domínio: PROCESSAMENTO (Processing)

**Responsabilidade:** Orquestrar o cálculo mensal, gerenciar versões, rastrear execução.

**O que PODE existir:**
- Entidades: Processing, ProcessingStatus, ProcessingVersion
- Value Objects: Competence (ano-mês), ProcessingId
- Services: ProcessingOrchestrator, CalculationPipeline
- Eventos: ProcessingStarted, ProcessingCompleted, ProcessingFailed
- Repositories para Processings

**O que NÃO pode existir:**
- Cálculos específicos (delegados para domínios de Tributos, Consignados)
- Regras de RH ou negócio específicas
- Acesso a dados de BD sem passar por repositories

**States de um Processing:**
```
Draft (criado)
  ↓
InProgress (calculando)
  ↓
Completed (sucesso)
  ├→ Certified (assinado, enviado)
  └→ ERROR (falha, precisa reprocessar)
```

**Estrutura:**
```
Processing
├── ProcessingId
├── CompanyId
├── Competence (YearMonth)
├── Version (1, 2, 3...)
├── Status
├── StartedAt
├── CompletedAt
├── ProcessedBy (usuário)
├── ErrorMessage (se Status = ERROR)
└── Items[]  // Resultado do cálculo (cada employee)
    └── ProcessingItem
        ├── EmployeeId
        ├── RubricCalculations[]
        ├── CalculationMemory (logs intermediários)
        └── Signature (quando certificado)
```

### 4.4 Domínio: TRIBUTOS (Taxes)

**Responsabilidade:** Cálculos de impostos (IRRF, INSS, FGTS) e suas regras por vigência.

**O que PODE existir:**
- Entidades: TaxRule, IRRFTable, INSSContribution, FGTSRule
- Value Objects: TaxPercentage, TaxBand
- Services: IRRFCalculator, INSSCalculator, FGTSCalculator
- Repositories para regras de tributo

**O que NÃO pode existir:**
- Dados de Employee específicos (recebe como parâmetro)
- Cálculos de consignados
- Dados de processamento (recebe como contexto)

**Regras críticas de imutabilidade:**
- Uma TaxRule vigente NÃO pode ser alterada; apenas novas vigências podem ser criadas
- Histórico completo de tabelas IRRF, INSS, FGTS deve ser mantido
- Recálculos usam a tabela vigente NA DATA DO PROCESSAMENTO ORIGINAL, não na data atual

**Exemplo de IRRF com vigências:**
```
TaxRule (IRRF)
├── 2024 (vigência 01/01 - 31/03)
│   └── Bands
│       ├── 0 - 2.xxx (0%)
│       ├── 2.xxx - 3.xxx (7.5%)
│       └── ...
├── 2024 (vigência 01/04 - 31/12)
│   └── Bands (alíquotas diferentes)
└── 2025 (vigência 01/01 - ...)
    └── Bands (atualizado)
```

### 4.5 Domínio: CONSIGNADOS (Loans and Deductions)

**Responsabilidade:** Gerenciar descontos de consignação com cálculo de margem disponível.

**O que PODE existir:**
- Entidades: ConsignedLoan, ConsignedDeduction, MarginCalculation
- Value Objects: LoanAmount, InterestRate, InstallmentCount
- Services: MarginCalculator, ConsignedDeductionCalculator
- Repositories

**O que NÃO pode existir:**
- Cálculos de outro tipo de desconto (desconto de saúde, etc.)
- Lógica de aprovação de crédito
- Regras de negócio de gestão de crédito (fora do escopo de folha)

**Restrições:**
- Desconto total de consignados NÃO pode exceder a margem disponível
- Margem disponível = limite definido pelo programa - (∑ descontos em vigor)
- Deve validar antes de incluir novo consignado

### 4.6 Domínio: RELATÓRIOS (Reporting)

**Responsabilidade:** Gerar relatórios, extratos, dados para BI.

**O que PODE existir:**
- Queries especializadas para relatórios
- Services de formatação de dados para export (PDF, Excel, XML)
- Caching para relatórios pesados
- Agregações de dados (resumos por empresa, departamento)

**O que NÃO pode existir:**
- Cálculos (dados já calculados no Processing)
- Modificação de dados
- Lógica de negócio complexa

**Dados que PODE exportar:**
- Folha processada (rubrics, descontos, líquido)
- Memória de cálculo (explicar cada valor)
- Relatórios de auditoria (quem processou, quando)
- Extratos para eSocial, INSS, etc.

### 4.7 Domínio: INTEGRAÇÕES (Integrations)

**Responsabilidade:** Comunicação com sistemas externos (eSocial, bancos, órgãos reguladores).

**O que PODE existir:**
- Adapters para APIs externas
- Transformação de dados (folha → eSocial XML)
- Sincronização de dados (double-write pattern se necessário)
- Retry logic e circuit breaker

**O que NÃO pode existir:**
- Lógica de cálculo
- Armazenamento permanente de dados da integração (apenas logs)
- Modificação de dados mestres

**Fluxo de integração:**
```
Processing Complete
    ↓
Event: ProcessingCompleted
    ↓
Integrations Listener (subscribe)
    ↓
Transform to External Format (eSocial)
    ↓
Call External API
    ↓
Log Result (Success/Failure)
    ↓
If Failure: Queue for Retry
```

### 4.8 Domínio: SEGURANÇA E AUDITORIA (Security & Audit)

**Responsabilidade:** Controle de acesso, auditoria, conformidade.

**O que PODE existir:**
- User, Role, Permission entidades
- Audit Log entries
- Compliance validators
- Signature/Digital signature management
- Access control services

**O que NÃO pode existir:**
- Lógica de cálculo
- Dados de folha sem auditoria

**Obrigações de auditoria:**
- Quem processou a folha? Timestamp.
- Qual versão do sistema? Log completo.
- Qual vigência de regras foi usada? Referência explícita.
- Memória de cálculo: cada intermediário registrado.
- Alteração de dados mestres: antes/depois documentado.

---

## 5. Engine de Cálculo da Folha

### 5.1 Conceito: Pipeline Determinístico

O cálculo de folha é uma **série de transformações determinísticas**, aplicadas em ordem fixa, onde cada etapa depende da anterior.

**Premissas:**
- **Entrada:** Employee data, Rubric values, Competence, Tax rules vigentes
- **Processo:** Pipeline linear sem branches aleatórios
- **Saída:** CalculationResult (proventos, descontos, líquido, memória)
- **Garantia:** Mesma entrada → sempre mesma saída

### 5.2 Etapas do Pipeline

```
Stage 1: Load & Validate
    ├─ Validar Employee está ativo em competência
    ├─ Validar Rubricas existem
    ├─ Validar valores de entrada (não-negativos)
    └─ Se erro → ProcessingStatus = ERROR

Stage 2: Collect Earnings
    ├─ Buscar todas rubricas tipo EARNING para competência
    ├─ Aplicar fórmulas vigentes
    ├─ Somar proventos brutos
    └─ Register: EarningItems[], GrossSalary

Stage 3: Calculate Deductions (Non-Tax)
    ├─ Buscar rubricas tipo DISCOUNT
    ├─ Aplicar fórmulas (health, dental, etc.)
    ├─ Somar descontos não-tributários
    └─ Register: DeductionItems[]

Stage 4: Calculate INSS
    ├─ Buscar INSS rule vigente
    ├─ Aplicar cálculo (progressivo com limite)
    ├─ Registrar base INSS
    └─ Register: INSSValue, INSSBase

Stage 5: Calculate IRRF
    ├─ Calcular base IRRF (earnings - INSS - other deductions)
    ├─ Buscar tabela IRRF vigente
    ├─ Aplicar alíquota progressiva
    ├─ Registrar dependentes, deduções
    └─ Register: IRRFValue, IRRFBase

Stage 6: Calculate FGTS
    ├─ Buscar FGTS rule vigente
    ├─ Aplicar percentual (8% ou especial)
    ├─ Registrar separadamente
    └─ Register: FGTSValue, FGTSBase

Stage 7: Calculate Consigned Deductions
    ├─ Buscar consignados vigentes do employee
    ├─ Validar se há margem disponível
    ├─ Calcular descontos
    └─ Register: ConsignedItems[], UsedMargin

Stage 8: Calculate Net Salary
    ├─ NetSalary = GrossSalary - ∑(Deductions) - ∑(Taxes) - ∑(Consigned)
    ├─ Validar NetSalary ≥ 0
    └─ Register: NetSalary, DetailedBreakdown

Stage 9: Generate Calculation Memory
    ├─ Compor JSON/XML com todos intermediários
    ├─ Registrar decisões (qual rubrica, qual vigência)
    ├─ Incluir audit trail
    └─ Register: CalculationMemory (imutável)

Stage 10: Finalize & Store
    ├─ Marcar resultado como final
    ├─ Salvar em tabelas imutáveis
    ├─ Publicar evento ProcessingItemCalculated
    └─ Status: COMPLETED ou ERROR
```

### 5.3 Ordem de Execução (CRÍTICO)

A ordem do pipeline NÃO pode mudar, pois cada etapa depende da anterior.

**Sequência obrigatória:**
1. Earnings (bruto)
2. Deductions
3. INSS (usa bruto - deductions)
4. IRRF (usa bruto - INSS - deductions)
5. FGTS (independente)
6. Consignados (valida margem)
7. Net Salary (soma final)
8. Memory (registra tudo)
9. Store (persiste)

**Exemplo de inversão errada:**
```csharp
// ❌ ERRADO - IRRF antes de INSS
var irrf = CalculateIRRF(bruto);  // base incorreta
var inss = CalculateINSS(bruto);  // não desconta INSS da base IRRF

// ✅ CERTO - INSS depois, IRRF vê base corrigida
var inss = CalculateINSS(bruto);
var irrf = CalculateIRRF(bruto - inss);
```

### 5.4 Evitar Efeitos Colaterais

**Efeito colateral:** Cálculo modifica estado externo ou depende de estado mutável.

**Proibido:**
```csharp
// ❌ Modifica estado global
private static decimal _accumulatedInss = 0;

public decimal Calculate(Employee emp)
{
    var inss = emp.Salary * 0.08m;
    _accumulatedInss += inss;  // EFEITO COLATERAL!
    return inss;
}

// ❌ Usa DateTime.Now
public CalculationResult Calculate(Employee emp, Competence comp)
{
    if (DateTime.Now.Month == 12) // Lógica de Natal? Não!
        return SpecialCalculation();
}

// ❌ Dependência de estado compartilhado
var calculator = new SalaryCalculator();
calculator.CurrentCompetence = competence;
calculator.Calculate(emp);  // Comportamento depende de estado mutável
```

**Recomendado:**
```csharp
// ✅ Função pura: mesma entrada → mesma saída
public CalculationResult Calculate(
    Employee emp,
    IList<RubricValue> rubrics,
    Competence competence,  // Usar competência explícita
    ITaxRuleProvider taxRules)  // Passar regras como parâmetro
{
    // Sem modificações de estado externo
    // Sem DateTime.Now
    // Sem IO
}
```

### 5.5 Permitir Reprocessamento

**Reprocessamento:** Recalcular uma competência já processada (V1 → V2, V2 → V3).

**Garantias:**
- V1 inalterado no BD (imutável)
- V2 criado com novo ProcessingId, mesma Competence
- Versão incrementada automaticamente
- Resultado pode ser diferente (legislação atualizada)
- Relatórios rastreiam qual versão foi usada

### 5.6 Implementação do Cálculo de INSS (v0.3)

**Status:** ✅ Implementado e testado

O INSS brasileiro é calculado de forma **progressiva**, onde cada faixa salarial tem sua própria alíquota. O desconto é aplicado apenas sobre a parcela do salário que está dentro de cada faixa.

**Classes implementadas:**

1. **FaixaInss** (Value Object) - `FolhadePagamento.Dominio/Inss/FaixaInss.cs`
   - Representa uma faixa da tabela progressiva
   - Propriedades: `LimiteInferior`, `LimiteSuperior`, `Aliquota`
   - Método principal: `CalcularContribuicaoFaixa(Dinheiro salarioBruto)`
   - Imutável por design

2. **TabelaInss** (Value Object) - `FolhadePagamento.Dominio/Inss/TabelaInss.cs`
   - Contém múltiplas faixas e uma `Vigencia`
   - Propriedades: `Identificador`, `Descricao`, `Vigencia`, `Faixas`, `Teto`
   - Métodos: `Calcular()`, `EstaVigenteParaCompetencia()`
   - Fábrica: `CriarTabela2024()`, `CriarTabela2025()`

3. **ResultadoCalculoInss** - Resultado detalhado do cálculo com memória

4. **CalculadoraInss** (Serviço de Domínio) - `FolhadePagamento.Dominio/Inss/CalculadoraInss.cs`
   - Recebe múltiplas tabelas ordenadas por vigência
   - Seleciona automaticamente a tabela vigente para a competência
   - Método principal: `Calcular(Dinheiro salarioBruto, Competencia competencia)`

**Tabela INSS 2025 (valores oficiais):**

| Faixa | Limite Inferior | Limite Superior | Alíquota |
|-------|-----------------|-----------------|----------|
| 1     | R$ 0,00         | R$ 1.518,00     | 7,5%     |
| 2     | R$ 1.518,01     | R$ 2.793,88     | 9,0%     |
| 3     | R$ 2.793,89     | R$ 4.190,83     | 12,0%    |
| 4     | R$ 4.190,84     | R$ 8.157,41     | 14,0%    |

**Teto de contribuição:** R$ 8.157,41

**Exemplo de cálculo progressivo (salário R$ 3.000,00):**
```
Faixa 1: R$ 1.518,00 × 7,5% = R$ 113,85
Faixa 2: (R$ 3.000,00 - R$ 1.518,00) × 9% = R$ 482,00 × 9% = R$ 43,38
Total INSS: R$ 113,85 + R$ 43,38 = R$ 157,23
```

**Integração no Pipeline:**
```csharp
// Etapa 2 do Pipeline: Calcular INSS (após proventos)
if (_calculadoraInss?.ExisteTabelaVigente(competencia) == true)
{
    var detalheInss = _calculadoraInss.Calcular(salarioBruto, competencia);
    valorInss = detalheInss.ValorInss;
}
```

**Garantias de determinismo:**
- Sem `DateTime.Now` - competência passada explicitamente
- Tabelas de INSS são Value Objects imutáveis
- Mesmo salário + mesma competência → sempre mesmo resultado
- Vigência garante que regras antigas sejam preservadas

**Cobertura de testes:** 58 testes específicos para INSS (FaixaInss, TabelaInss, CalculadoraInss, integração)

**Implementação:**
```csharp
public class ProcessingAggregate
{
    public ProcessingId Id { get; private set; }
    public CompanyId CompanyId { get; private set; }
    public Competence Competence { get; private set; }
    public int Version { get; private set; }  // 1, 2, 3...
    public ProcessingStatus Status { get; private set; }
    public IList<ProcessingItem> Items { get; private set; }
    
    public void Reprocess(CalculationResult[] newResults)
    {
        // Só permitir se versão anterior está COMPLETED
        if (this.Status != ProcessingStatus.Completed)
            throw new InvalidOperationException("Cannot reprocess incomplete processing");
        
        // Criar nova versão - NUNCA modifica this
        var newProcessing = new ProcessingAggregate(
            ProcessingId.New(),
            this.CompanyId,
            this.Competence,
            this.Version + 1,  // Versão incrementa
            newResults);
        
        return newProcessing;  // Retorna novo agregado, deixa original intacto
    }
}
```

---

## 6. Modelo Mental de Dados

### 6.1 Conceito: Vigência

**Vigência:** Período de tempo em que uma regra é válida.

**Premissa fundamental:** Legislação muda. Cada mudança começa em uma data.

**Exemplo:**
```
Alíquota INSS 2024:
├─ 01/01/2024 - 31/03/2024: 8.0%
├─ 01/04/2024 - 30/06/2024: 8.5%
├─ 01/07/2024 - 31/12/2024: 9.0%
└─ 01/01/2025 - ??: 9.0% (até novo decreto)

Tabela IRRF 2024:
├─ 01/01/2024 - 31/03/2024: Tabela A
├─ 01/04/2024 - 31/12/2024: Tabela B (reajustada)
└─ 01/01/2025: Tabela C
```

**Implementação em BD:**
```sql
CREATE TABLE TaxRuleVigence (
    RuleVigenceId INT PRIMARY KEY,
    RuleId INT,                    -- Qual regra (INSS, IRRF)
    StartDate DATE NOT NULL,       -- Início da vigência
    EndDate DATE,                  -- Fim (NULL = indefinida)
    Percentage DECIMAL(5,2),       -- Valor da regra
    CreatedAt DATETIME,
    CreatedBy NVARCHAR(MAX),
    CONSTRAINT UQ_Rule_DateRange UNIQUE (RuleId, StartDate),
    CONSTRAINT CK_EndDateAfterStart CHECK (EndDate IS NULL OR EndDate > StartDate)
);

-- Nunca deletar; apenas adicionar nova vigência
-- Histórico completo preservado para auditoria
```

**Regra obrigatória:**
- Não há "overlap" de vigências (uma regra não pode ter 2 vigências no mesmo período)
- Para encontrar vigência em uso: `SELECT * FROM TaxRuleVigence WHERE RuleId = X AND StartDate <= @Competence AND (EndDate IS NULL OR EndDate >= @Competence)`

### 6.2 Diferença: Cadastro vs Resultado

**Cadastro (Master Data):** Dados que alimentam o cálculo, mutáveis.

| Entidade | Tipo | Mutável? | Persistência |
|----------|------|----------|--------------|
| Employee | Cadastro | Sim (até processado) | Tabela `Employees` |
| Company | Cadastro | Sim | Tabela `Companies` |
| Rubric | Cadastro | Não (vigências) | Tabela `Rubrics` + vigências |
| TaxRule | Cadastro | Não (vigências) | Tabela `TaxRules` + vigências |

**Resultado (Calculated Data):** Dados gerados pelo processamento, imutáveis.

| Entidade | Tipo | Mutável? | Persistência |
|----------|------|----------|--------------|
| Processing | Resultado | Não | Tabela `Processings` (IsFinal=true) |
| ProcessingItem | Resultado | Não | Tabela `ProcessingItems` |
| ProcessingRubricValue | Resultado | Não | Tabela `ProcessingRubricValues` |
| CalculationMemory | Resultado | Não | Tabela `CalculationMemories` (JSON/XML) |

**Fluxo:**
```
Cadastro (Employee, Rubric, TaxRule)
    ↓
Vigências validadas para Competência
    ↓
Calculate (função pura)
    ↓
Resultado (ProcessingItem, ProcessingRubricValue)
    ↓
Resultado finalizado → IsFinal = true
    ↓
Imutável a partir daqui
```

### 6.3 Tabelas Mutáveis vs Tabelas Imutáveis

**Tabelas Mutáveis (podem sofrer UPDATE/DELETE):**
- `Employees` (alterar endereço, banco, até antes de processar)
- `Companies` (alterar CNPJ, razão social)
- `RubricFormulas` (adicionar nova fórmula)
- `ConsignedLoans` (ativo/inativo antes de descontar)
- `Users`, `Roles` (segurança)

**Restrição:** Se Employee/Rubric foi processado em competência passada, não pode alterar dados que afetam aquele cálculo retroativamente.

**Tabelas Imutáveis (INSERT only, nunca UPDATE/DELETE):**
- `Processings` (histórico de processamentos)
- `ProcessingItems` (itens calculados)
- `ProcessingRubricValues` (valores de cada rubrica)
- `ProcessingTaxValues` (valores de cada imposto)
- `CalculationMemories` (memória explicativa)
- `AuditLogs` (rastreamento de alterações)
- `EventLog` (eventos de domínio)

**Implementação:**
```sql
-- ❌ Permitir DELETE/UPDATE
CREATE TABLE Processings (
    ProcessingId INT PRIMARY KEY,
    CompanyId INT,
    Competence DATE,
    Status NVARCHAR(50),
    -- Sem trigger de proteção
);

-- ✅ Proteger imutabilidade
CREATE TABLE Processings (
    ProcessingId INT PRIMARY KEY,
    CompanyId INT,
    Competence DATE,
    Status NVARCHAR(50),
    IsFinal BIT DEFAULT 0,
    CreatedAt DATETIME NOT NULL,
    CHECK (IsFinal = 1)  -- Uma vez final, nunca muda
);

CREATE TRIGGER Processings_PreventModification
ON Processings
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    IF EXISTS(SELECT 1 FROM INSERTED WHERE IsFinal = 1)
        THROW 50001, 'Cannot modify finalized processing', 1;
    IF EXISTS(SELECT 1 FROM DELETED WHERE IsFinal = 1)
        THROW 50001, 'Cannot delete finalized processing', 1;
END;
```

### 6.4 Versionamento de Processamento

**Conceito:** Um processamento da mesma competência pode ter múltiplas versões.

**Exemplos de quando criar nova versão:**
- V1 processado em jan/2025 (erro)
- V2 recalculado em jan/2025 (correção de fórmula IRRF)
- V3 reprocessado em fev/2025 (legislação mudou)
- V4 reprocessado em mar/2025 (auditoria solicitou)

**Estrutura:**
```sql
CREATE TABLE Processings (
    ProcessingId UNIQUEIDENTIFIER PRIMARY KEY,
    CompanyId INT NOT NULL,
    Competence DATE NOT NULL,          -- jan/2025, fev/2025, etc.
    Version INT NOT NULL,              -- 1, 2, 3...
    Status NVARCHAR(50) NOT NULL,      -- Draft, InProgress, Completed, Error
    ProcessedBy NVARCHAR(MAX),
    ProcessedAt DATETIME NOT NULL,
    CalculationStartedAt DATETIME,
    CalculationCompletedAt DATETIME,
    ErrorMessage NVARCHAR(MAX),
    IsFinal BIT DEFAULT 0,
    CONSTRAINT UQ_Company_Competence_Version UNIQUE (CompanyId, Competence, Version)
);
```

**Regras:**
- Não pode haver gap em versões (1, 2, 4 é inválido; deve ser 1, 2, 3)
- Versão anterior sempre fica com status SUPERSEDED
- Relatórios podem mostrar V1 ou V2, mas rastreiam qual versão
- Sempre salvar qual versão foi usada para cada documento emitido (RPA, folha, etc.)

**Reprocessamento:**
```csharp
public class ProcessPayrollUseCase
{
    public async Task<Processing> Execute(ProcessCompetenceCommand cmd)
    {
        var company = await _companyRepo.GetByIdAsync(cmd.CompanyId);
        var competence = cmd.Competence;
        
        // Buscar último processamento
        var lastProcessing = await _processingRepo
            .GetLastProcessing(cmd.CompanyId, competence);
        
        if (lastProcessing == null)
        {
            // Primeiro processamento
            newProcessing = CreateNewProcessing(company, competence, version: 1);
        }
        else
        {
            // Reprocessamento: próxima versão
            newProcessing = CreateNewProcessing(
                company,
                competence,
                version: lastProcessing.Version + 1);
            
            // Marcar anterior como superseded
            lastProcessing.MarkAsSuperseded();
        }
        
        // Executar pipeline
        var result = await _calculationEngine.Calculate(newProcessing);
        
        // Salvar resultado
        await _processingRepo.SaveAsync(newProcessing);
        
        return newProcessing;
    }
}
```

### 6.5 Relação: Competência, Processamento, Itens Calculados

**Competência:** Mês de referência (jan/2025, fev/2025, etc.).

**Processamento:** Execução do cálculo de uma competência para uma empresa.

**Item Calculado:** Resultado de cálculo para um employee em um processamento.

**Hierarquia:**
```
Processing (1)
├─ CompanyId: ACME Inc.
├─ Competence: jan/2025
├─ Version: 1
└─ ProcessingItems[]  (múltiplos employees)
    ├─ ProcessingItem (1)
    │   ├─ EmployeeId: EMP001
    │   ├─ GrossSalary: 5.000
    │   ├─ Deductions: 500
    │   ├─ INSS: 400
    │   ├─ IRRF: 300
    │   ├─ NetSalary: 3.800
    │   └─ CalculationMemoryId: CALC001
    │
    └─ ProcessingItem (2)
        ├─ EmployeeId: EMP002
        ├─ GrossSalary: 3.000
        ├─ ...
```

**Queries típicas:**
```sql
-- Buscar folha processada de uma empresa em um mês
SELECT * FROM ProcessingItems pi
INNER JOIN Processings p ON pi.ProcessingId = p.ProcessingId
WHERE p.CompanyId = @CompanyId 
  AND p.Competence = @Competence
  AND p.Version = (
      SELECT MAX(Version)
      FROM Processings
      WHERE CompanyId = @CompanyId AND Competence = @Competence
  );

-- Histórico de versões de uma competência
SELECT * FROM Processings
WHERE CompanyId = @CompanyId AND Competence = @Competence
ORDER BY Version DESC;

-- Buscar memória de cálculo de um item
SELECT cm.CalculationMemoryJSON
FROM CalculationMemories cm
INNER JOIN ProcessingItems pi ON cm.ProcessingItemId = pi.ProcessingItemId
WHERE pi.ProcessingItemId = @ProcessingItemId;
```

**Imutabilidade garantida:**
- Uma vez finalizado, `Processing.IsFinal = true` e `Status = Completed`
- `ProcessingItems` não podem ser alterados, apenas novos criados em nova versão
- `CalculationMemories` são imutáveis (histórico)

---

---

## 7. Processamento Assíncrono

### 7.1 Por que o Cálculo NÃO Deve Ser Síncrono

**Problema de sincronicidade:**
```
HTTP POST /api/payroll/process
    ↓
Calcular 500 employees (LENTO!)
    ↓
Esperar 5+ minutos
    ↓
HTTP Timeout (504 Gateway Timeout)
    ↓
Erro do usuário
```

**Soluções:**
1. **Requisição HTTP cria job assíncrono** (Hangfire, Quartz)
2. **Retorna ProcessingId e status imediatamente**
3. **Cliente consulta progresso** via `/api/payroll/{processingId}/status`
4. **Job executa em background** sem bloquear requisição

**Benefícios:**
- Requisição HTTP responde em <100ms
- Cálculo acontece em worker thread/background job
- Permite retry automático em caso de falha
- Histórico de execução auditável
- Escalabilidade: múltiplas jobs em paralelo (com controle de concorrência)

### 7.2 Como Funciona o Disparo de Jobs

**Fluxo:**

```
1. API Recebe Requisição
   POST /api/payroll/process
   {
       "companyId": 1,
       "competence": "2025-01"
   }

2. Application Service (Síncrono)
   - Validar entrada
   - Verificar competência não foi processada (ou increment Version)
   - Criar Processing com Status = Draft
   - **Enfileirar Job** via IJobScheduler
   - Retornar ProcessingId

3. Response HTTP (instantâneo)
   {
       "processingId": "550e8400-e29b-41d4-a716-446655440000",
       "status": "Enqueued",
       "statusUrl": "/api/payroll/550e8400.../status"
   }

4. Background Job (Assíncrono)
   Hangfire/Quartz pega job da fila
   
   - Update Processing.Status = InProgress
   - Update Processing.StartedAt = Now
   
   Para cada Employee na Company:
     - Carregar dados (Employee, Rubricas, TaxRules)
     - Executar SalaryCalculator (função pura)
     - Salvar ProcessingItem
     - Register CalculationMemory
   
   - Update Processing.Status = Completed
   - Update Processing.CompletedAt = Now
   - Publicar evento ProcessingCompleted
   
5. Cliente Consulta Status
   GET /api/payroll/550e8400.../status
   
   {
       "processingId": "550e8400-e29b-41d4-a716-446655440000",
       "status": "InProgress",
       "progress": {
           "totalEmployees": 500,
           "processedEmployees": 250,
           "percentage": 50
       }
   }
   
   Ou, ao final:
   {
       "status": "Completed",
       "itemsProcessed": 500,
       "completedAt": "2025-01-15T14:30:00Z"
   }

6. Event Subscription
   Ao ProcessingCompleted:
   - Integrations Listener: Dispara exportação para eSocial
   - Notifications: Avisa que folha está pronta
   - AuditLog: Registra conclusão
```

### 7.3 Controle de Concorrência por Empresa/Competência

**Problema:** Dois usuários disparam processamento da mesma competência simultaneamente.

**Solução: Distributed Lock**

```csharp
public class ProcessPayrollUseCase
{
    private readonly IDistributedLockProvider _lockProvider;
    private readonly IJobScheduler _jobScheduler;
    
    public async Task<ProcessingResult> Execute(ProcessPayrollCommand cmd)
    {
        // Chave única: Company + Competence + não pode processar em paralelo
        var lockKey = $"payroll:process:{cmd.CompanyId}:{cmd.Competence:yyyy-MM}";
        
        using (var @lock = await _lockProvider.AcquireLockAsync(lockKey, timeoutSeconds: 30))
        {
            if (@lock == null)
                throw new ConcurrencyException("Another processing is in progress for this company/competence");
            
            // Dentro do lock: seguro
            var lastProcessing = await _processingRepo.GetLastProcessing(
                cmd.CompanyId, 
                cmd.Competence);
            
            if (lastProcessing?.Status == ProcessingStatus.InProgress)
                throw new InvalidOperationException("Processing already in progress");
            
            // Criar novo processing
            var newProcessing = new ProcessingAggregate(
                ProcessingId.New(),
                cmd.CompanyId,
                cmd.Competence,
                version: (lastProcessing?.Version ?? 0) + 1);
            
            await _processingRepo.SaveAsync(newProcessing);
            
            // Enfileirar job
            var jobId = await _jobScheduler.ScheduleAsync<ProcessPayrollJob>(
                new ProcessPayrollJobArgs
                {
                    ProcessingId = newProcessing.Id,
                    CompanyId = cmd.CompanyId,
                    Competence = cmd.Competence
                });
            
            return new ProcessingResult
            {
                ProcessingId = newProcessing.Id,
                Status = ProcessingStatus.Enqueued,
                JobId = jobId
            };
        }
    }
}
```

**Implementação do Lock:**
- **Redis:** Usar `SET NX EX` (SET if Not eXists with EXpiry)
- **SQL Server:** Usar aplicação com ROWLOCK + retry
- **Hangfire:** Possui built-in distributed lock

### 7.4 Tratamento de Falhas e Retomada

**Cenários de falha:**
1. Erro em cálculo de um employee → Salvar erro, continuar próximo
2. Erro crítico (BD indisponível) → Job falha, enfileirar retry
3. Job morto/timeout → Hangfire reexecuta automaticamente

**Implementação:**

```csharp
public class ProcessPayrollJob
{
    private readonly IProcessingRepository _processingRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly SalaryCalculator _calculator;
    private readonly ILogger<ProcessPayrollJob> _logger;
    
    public async Task Execute(ProcessPayrollJobArgs args, IJobCancellationToken cancellationToken)
    {
        var processing = await _processingRepo.GetByIdAsync(args.ProcessingId);
        
        try
        {
            processing.Start();
            await _processingRepo.SaveAsync(processing);
            
            var employees = await _employeeRepo.GetByCompanyAsync(args.CompanyId);
            var totalCount = employees.Count;
            var processedCount = 0;
            
            foreach (var employee in employees)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                try
                {
                    var result = await CalculateEmployee(employee, args.Competence);
                    var item = ProcessingItem.Create(processing.Id, employee.Id, result);
                    processing.AddItem(item);
                    
                    processedCount++;
                    
                    // Log progresso a cada 50 employees
                    if (processedCount % 50 == 0)
                        _logger.LogInformation(
                            "Processing {ProcessingId}: {Processed}/{Total}",
                            args.ProcessingId, processedCount, totalCount);
                }
                catch (CalculationException ex)
                {
                    // Erro de cálculo (ex: dados inválidos) → logar, continuar
                    _logger.LogWarning(
                        "Calculation error for employee {EmployeeId}: {Error}",
                        employee.Id, ex.Message);
                    
                    var errorItem = ProcessingItem.CreateError(
                        processing.Id, employee.Id, ex.Message);
                    processing.AddItem(errorItem);
                }
            }
            
            processing.Complete();
            await _processingRepo.SaveAsync(processing);
            
            // Publicar evento para subscribers (integrações, notificações)
            await _eventBus.PublishAsync(new ProcessingCompletedEvent(processing.Id));
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            // Erro crítico (falha na BD, etc.) → Job falha, Hangfire retry
            processing.MarkAsError(ex.Message);
            await _processingRepo.SaveAsync(processing);
            
            _logger.LogError(ex, "Critical error in processing {ProcessingId}", args.ProcessingId);
            throw;  // Deixa Hangfire reexecutar
        }
    }
    
    private async Task<CalculationResult> CalculateEmployee(Employee emp, Competence comp)
    {
        // Buscar dados de entrada
        var rubrics = await _rubricRepo.GetRubricValuesAsync(emp.CompanyId, comp);
        var taxRules = await _taxRuleProvider.GetRulesAsync(comp);
        
        // Executar cálculo (função pura)
        var result = _calculator.Calculate(emp, rubrics, taxRules, comp);
        
        return result;
    }
}
```

**Hangfire Configuration:**
```csharp
// Startup
services.AddHangfire(config => 
    config.UseSqlServerStorage("ConnectionString")
          .UseRecommendedSerializerSettings());

// Job retry policy
GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3 });

// Background job server
app.UseHangfireServer();
```

---

## 8. APIs e Casos de Uso

### 8.1 Filosofia de Endpoints

**Princípio:** API expõe **casos de uso de negócio**, não operações CRUD.

**Errado (CRUD puro):**
```
GET    /api/employees/{id}
POST   /api/employees
PUT    /api/employees/{id}
DELETE /api/employees/{id}
GET    /api/processings
POST   /api/processings
PUT    /api/processings/{id}
```

**Correto (Casos de Uso):**
```
GET    /api/payroll/{processingId}/status
POST   /api/payroll/process-competence         # Iniciar processamento
GET    /api/payroll/{processingId}/items       # Consultar resultados
GET    /api/payroll/{processingId}/memory/{itemId}  # Memória de cálculo

POST   /api/employees                           # Cadastrar employee
PUT    /api/employees/{id}                      # Atualizar employee (antes processar)
POST   /api/employees/{id}/deactivate           # Desativar (operação de negócio)

GET    /api/rubrics                             # Listar rubricas
POST   /api/rubrics                             # Criar nova rubrica
POST   /api/rubrics/{id}/new-vigence            # Adicionar vigência
```

### 8.2 Exemplos Conceituais de Casos de Uso

**UC1: Processar Folha de Uma Competência**

```csharp
[HttpPost("api/payroll/process-competence")]
public async Task<IActionResult> ProcessCompetence(
    [FromBody] ProcessCompetenceRequest request,
    [FromServices] IProcessPayrollUseCase useCase)
{
    var command = new ProcessPayrollCommand
    {
        CompanyId = request.CompanyId,
        Competence = YearMonth.Parse(request.Competence),
        ProcessedBy = User.GetUserId()
    };
    
    try
    {
        var result = await useCase.Execute(command);
        
        return Accepted(new
        {
            processingId = result.ProcessingId,
            status = result.Status,
            statusUrl = $"/api/payroll/{result.ProcessingId}/status"
        });
    }
    catch (ConcurrencyException ex)
    {
        return Conflict(new { error = ex.Message });
    }
}
```

**UC2: Consultar Status de Processamento**

```csharp
[HttpGet("api/payroll/{processingId}/status")]
public async Task<IActionResult> GetProcessingStatus(
    [FromRoute] Guid processingId,
    [FromServices] IGetProcessingStatusQuery query)
{
    var result = await query.Execute(processingId);
    
    return Ok(new
    {
        processingId = result.Id,
        status = result.Status.ToString(),
        competence = result.Competence.ToString(),
        version = result.Version,
        startedAt = result.StartedAt,
        completedAt = result.CompletedAt,
        itemsProcessed = result.Items.Count,
        itemsWithError = result.Items.Count(x => x.HasError)
    });
}
```

**UC3: Consultar Memória de Cálculo de um Item**

```csharp
[HttpGet("api/payroll/{processingId}/items/{itemId}/memory")]
public async Task<IActionResult> GetCalculationMemory(
    [FromRoute] Guid processingId,
    [FromRoute] Guid itemId,
    [FromServices] IGetCalculationMemoryQuery query)
{
    var memory = await query.Execute(processingId, itemId);
    
    return Ok(new
    {
        processingItemId = itemId,
        employeeId = memory.EmployeeId,
        competence = memory.Competence,
        calculationSteps = memory.Steps.Select(step => new
        {
            stage = step.StageName,
            description = step.Description,
            inputs = step.Inputs,
            output = step.Output,
            vigenceUsed = step.VigenceReference
        }),
        finalResult = memory.FinalResult
    });
}
```

**UC4: Reprocessar Competência com Nova Legislação**

```csharp
[HttpPost("api/payroll/{processingId}/reprocess")]
public async Task<IActionResult> ReprocessCompetence(
    [FromRoute] Guid processingId,
    [FromServices] IReprocessPayrollUseCase useCase)
{
    var command = new ReprocessPayrollCommand
    {
        ProcessingId = processingId,
        ProcessedBy = User.GetUserId()
    };
    
    var result = await useCase.Execute(command);
    
    return Accepted(new
    {
        newProcessingId = result.Id,
        version = result.Version,
        previousVersion = result.Version - 1
    });
}
```

### 8.3 O Que a API Faz e O Que NÃO Faz

**A API FAZ:**
- Validar formato de requisição (JSON schema, tipos)
- Autenticar usuário (JWT, Windows auth)
- Autorizar ação (roles, permissions)
- Mapear DTO → Domain Commands
- Chamar Application Service
- Retornar resposta HTTP com status correto
- Log de auditoria (quem fez o quê, quando)

**A API NÃO FAZ:**
- Calcular folha (delegado para Domain)
- Validar regras de negócio complexas (delegado para Domain)
- Acesso a BD (delegado para Application Service → Repository)
- Modificação de dados de processamento finalizado (bloqueado em Domain)
- Decisões de integração (delegado para evento)

### 8.4 Separação: Comando e Consulta (CQRS - Opcional)

**CQRS (Command Query Responsibility Segregation):** Separar operações de escrita (Commands) das de leitura (Queries).

**Pattern opcional, mas recomendado para folha:**

```csharp
// COMMANDS (Escrita)
public interface ICommand { }
public class ProcessPayrollCommand : ICommand
{
    public int CompanyId { get; set; }
    public YearMonth Competence { get; set; }
    public string ProcessedBy { get; set; }
}

public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    Task Execute(TCommand command);
}

// QUERIES (Leitura)
public interface IQuery<TResult> { }
public class GetProcessingStatusQuery : IQuery<ProcessingStatusResult>
{
    public Guid ProcessingId { get; set; }
}

public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> Execute(TQuery query);
}

// Handlers
public class ProcessPayrollCommandHandler : ICommandHandler<ProcessPayrollCommand>
{
    // Orquestração, persistência
    public async Task Execute(ProcessPayrollCommand command)
    {
        var processing = new ProcessingAggregate(...);
        await _jobScheduler.ScheduleAsync(processing);
    }
}

public class GetProcessingStatusQueryHandler : IQueryHandler<GetProcessingStatusQuery, ProcessingStatusResult>
{
    // Leitura otimizada, sem efeitos colaterais
    public async Task<ProcessingStatusResult> Execute(GetProcessingStatusQuery query)
    {
        var processing = await _processingRepo.GetByIdAsync(query.ProcessingId);
        return new ProcessingStatusResult { ... };
    }
}
```

**Benefícios:**
- Separação clara entre escrita e leitura
- Queries podem ser otimizadas sem afetar Commands
- Facilita caching em queries
- Testes mais simples

---

## 9. Front-end (Blazor Hybrid)

### 9.1 Papel do Front-end

**Responsabilidade:** Interface para usuários, captura de entrada, apresentação de dados, navegação.

**NÃO é responsável por:**
- Cálculo de folha
- Regras de negócio
- Persistência direta
- Validações complexas de domínio

**Fluxo:**
```
User Input
    ↓
Blazor Component (validação superficial)
    ↓
HTTP POST → API
    ↓
Application Service (validação profunda)
    ↓
Domain Service (cálculo)
    ↓
HTTP Response (DTO)
    ↓
Blazor Page (apresentar resultado)
```

### 9.2 Comunicação com a API

**Serviço HTTP reutilizável:**

```csharp
public interface IPayrollApiClient
{
    Task<ProcessingStatusResponse> GetProcessingStatusAsync(Guid processingId);
    Task<ProcessingResponse> ProcessCompetenceAsync(int companyId, string competence);
    Task<CalculationMemoryResponse> GetCalculationMemoryAsync(Guid processingId, Guid itemId);
}

public class PayrollApiClient : IPayrollApiClient
{
    private readonly HttpClient _httpClient;
    
    public PayrollApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.folhapagamento.local");
    }
    
    public async Task<ProcessingResponse> ProcessCompetenceAsync(int companyId, string competence)
    {
        var request = new { companyId, competence };
        var response = await _httpClient.PostAsJsonAsync("/api/payroll/process-competence", request);
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsAsync<ProcessingResponse>();
    }
}
```

**Registro em Startup:**
```csharp
services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri("https://api.folhapagamento.local") });
    
services.AddScoped<IPayrollApiClient, PayrollApiClient>();
```

### 9.3 O Que PODE e NÃO PODE no Front-end

**PODE:**
- Formatação de dados para apresentação (currency, date)
- Validação de formato de entrada (regex, length, type)
- Estado local de componentes (tab ativo, filtros)
- Navegação entre páginas
- Cache local (não crítico)
- Polling para atualizar status
- Tratamento de erros HTTP com mensagens user-friendly

**NÃO PODE:**
- Calcular IRRF, INSS, consignados
- Acesso direto a BD
- Bypass de validação do backend
- Modificar dados finalizados
- Lógica de negócio complexa

**Exemplo de Componente:**

```csharp
@page "/payroll/process"
@inject IPayrollApiClient ApiClient
@inject NavigationManager Navigation

<h3>Processar Folha</h3>

<EditForm Model="@model" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    
    <div>
        <label>Empresa:</label>
        <InputSelect @bind-Value="model.CompanyId">
            @foreach (var company in Companies)
            {
                <option value="@company.Id">@company.Name</option>
            }
        </InputSelect>
        <ValidationMessage For="@(() => model.CompanyId)" />
    </div>
    
    <div>
        <label>Competência:</label>
        <InputText @bind-Value="model.Competence" placeholder="yyyy-MM" />
        <ValidationMessage For="@(() => model.Competence)" />
    </div>
    
    <button type="submit" disabled="@isProcessing">
        @(isProcessing ? "Processando..." : "Processar Folha")
    </button>
</EditForm>

@code {
    private ProcessPayrollModel model = new();
    private bool isProcessing = false;
    private IEnumerable<CompanyDto> Companies { get; set; } = new List<CompanyDto>();
    
    protected override async Task OnInitializedAsync()
    {
        Companies = await ApiClient.GetCompaniesAsync();
    }
    
    private async Task HandleSubmit()
    {
        isProcessing = true;
        try
        {
            var response = await ApiClient.ProcessCompetenceAsync(
                model.CompanyId,
                model.Competence);
            
            // Redirecionar para status
            Navigation.NavigateTo($"/payroll/{response.ProcessingId}/status");
        }
        catch (HttpRequestException ex)
        {
            // Mostrar erro ao usuário
            await ShowErrorAsync($"Erro ao processar: {ex.Message}");
        }
        finally
        {
            isProcessing = false;
        }
    }
}
```

### 9.4 Boas Práticas de UI para Sistemas de Folha

1. **Sempre mostrar versão de processamento**
   - "Folha jan/2025 - Versão 2 (reprocessado em 15/01)"
   - Usuário sabe qual folha está consultando

2. **Link para memória de cálculo**
   - Cada linha de rubrica clicável → detalhes de cálculo
   - Auditoria e transparência

3. **Status em tempo real**
   - Progress bar enquanto processa
   - Atualização via polling (a cada 2-5s)
   - Mensagem "Processando employee 250 de 500"

4. **Filtros inteligentes**
   - Filtrar por competência, status, versão
   - Buscar por employee
   - Exportar (PDF, Excel)

5. **Histórico acessível**
   - Tab com versões anteriores
   - Comparação V1 vs V2
   - Rastreamento de mudanças

6. **Proteção contra acções acidentais**
   - Confirmação antes de reprocessar
   - Aviso se versão anterior não está finalizada
   - Disable botões quando não aplicável

---

## 10. Auditoria, Logs e Memória de Cálculo

### 10.1 O Que Deve Ser Auditado

**Tabela de Auditoria Obrigatória:**

```sql
CREATE TABLE AuditLogs (
    AuditLogId BIGINT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(MAX),
    EntityType NVARCHAR(100),           -- "Employee", "Processing", "TaxRule"
    EntityId NVARCHAR(MAX),             -- ID da entidade
    Action NVARCHAR(50),                -- "CREATE", "UPDATE", "DELETE", "VIEW"
    OldValue NVARCHAR(MAX),             -- JSON antes
    NewValue NVARCHAR(MAX),             -- JSON depois
    Timestamp DATETIME NOT NULL,
    IPAddress NVARCHAR(50),
    UserAgent NVARCHAR(MAX),
    ReasonForChange NVARCHAR(MAX),      -- Por que foi alterado?
    
    INDEX IX_EntityType_EntityId (EntityType, EntityId),
    INDEX IX_Timestamp (Timestamp DESC),
    INDEX IX_UserId (UserId)
);
```

**O que auditar obrigatoriamente:**

| Operação | O Quê | Por Quê |
|----------|-------|---------|
| Criar Processing | ProcessingId, Competence, Version | Rastrear quem iniciou processamento |
| Finalizar Processing | ProcessingId, Status, Items count | Histórico de processamentos finalizados |
| Alterar Employee | EmployeeId, OldValue, NewValue | Rastrear mudanças em cadastro |
| Alterar Rubric | RubricId, NewVigence | Legislação mudou, precisa histórico |
| Consultar Processing | ProcessingId, UserId | Quem acessou dados sensíveis |
| Alterar TaxRule | RuleId, NewVigence | Auditoria fiscal crítica |
| Reprocessar | ProcessingId, PreviousVersion, NewVersion | Rastrear reprocessamentos |

**O que NÃO precisa auditar:**
- Consultas de leitura em histórico (muito volume)
- Logs de sistema (heartbeat, health check)
- Dados privados sensíveis (salários, no OldValue/NewValue se possível)

### 10.2 Como Explicar um Valor Calculado

**Memória de Cálculo (Calculation Memory):** Documento imutável descrevendo cada passo do cálculo.

```sql
CREATE TABLE CalculationMemories (
    CalculationMemoryId UNIQUEIDENTIFIER PRIMARY KEY,
    ProcessingItemId UNIQUEIDENTIFIER NOT NULL,
    EmployeeId INT NOT NULL,
    CompetenceMonth DATE NOT NULL,
    ProcessingVersion INT NOT NULL,
    CalculationJSON NVARCHAR(MAX) NOT NULL,  -- JSON com todos os passos
    CreatedAt DATETIME NOT NULL,
    
    CONSTRAINT FK_ProcessingItem FOREIGN KEY (ProcessingItemId)
        REFERENCES ProcessingItems(ProcessingItemId),
    INDEX IX_EmployeeId_Competence (EmployeeId, CompetenceMonth)
);
```

**Estrutura do JSON de Memória:**

```json
{
  "processingItemId": "550e8400-e29b-41d4-a716-446655440000",
  "employeeId": 1001,
  "employeeName": "João Silva",
  "cpf": "123.456.789-00",
  "competence": "2025-01",
  "processingVersion": 1,
  "stages": [
    {
      "stageNumber": 1,
      "stageName": "Load Earnings",
      "rubrics": [
        {
          "rubricId": 100,
          "rubricCode": "SAL",
          "rubricName": "Salário",
          "formulaUsed": "Base: R$ 5000 (vigência 01/01/2025)",
          "value": 5000,
          "vigenceId": 5001,
          "vigenceStartDate": "2025-01-01",
          "vigenceEndDate": null
        }
      ],
      "subtotal": 5000
    },
    {
      "stageNumber": 2,
      "stageName": "Deductions",
      "rubrics": [
        {
          "rubricId": 200,
          "rubricCode": "VR",
          "rubricName": "Vale Refeição",
          "value": 300
        }
      ],
      "subtotal": 300
    },
    {
      "stageNumber": 4,
      "stageName": "INSS Calculation",
      "details": {
        "base": 4700,
        "ruleVigence": "01/01/2025",
        "contribution": "8.0%",
        "value": 376,
        "limitApplied": false,
        "explanation": "8% de R$ 4700 = R$ 376"
      }
    },
    {
      "stageNumber": 5,
      "stageName": "IRRF Calculation",
      "details": {
        "base": 4324,
        "baseCalculation": "Salário bruto (5000) - INSS (376) - VR (300) = 4324",
        "tableVigence": "2025-01 (Table A, effective 01/01/2025)",
        "dependents": 1,
        "deductionPerDependent": 189.59,
        "taxableBase": 4134.41,
        "bands": [
          {
            "band": "0 - 2.112,00",
            "rate": "0%",
            "amount": 0
          },
          {
            "band": "2.112,01 - 4.134,41",
            "rate": "7.5%",
            "amount": 151.68
          }
        ],
        "value": 151.68
      }
    },
    {
      "stageNumber": 6,
      "stageName": "FGTS Calculation",
      "details": {
        "base": 5000,
        "percentage": "8%",
        "value": 400,
        "note": "FGTS calculated over gross salary"
      }
    },
    {
      "stageNumber": 7,
      "stageName": "Consigned Deductions",
      "consigneds": [
        {
          "loanId": 500,
          "loanDescription": "Empréstimo Consignado Banco X",
          "installmentNumber": 48,
          "monthlyDeduction": 250,
          "available": true
        }
      ],
      "subtotal": 250
    },
    {
      "stageNumber": 8,
      "stageName": "Net Salary Calculation",
      "breakdown": {
        "grossSalary": 5000,
        "less": {
          "deductions": 300,
          "inss": 376,
          "irrf": 151.68,
          "consigneds": 250
        },
        "netSalary": 3922.32
      }
    }
  ],
  "finalResult": {
    "grossSalary": 5000,
    "deductions": 300,
    "inss": 376,
    "irrf": 151.68,
    "fgts": 400,
    "consigneds": 250,
    "netSalary": 3922.32
  },
  "auditTrail": {
    "calculatedAt": "2025-01-15T10:30:00Z",
    "calculatedBy": "System Job ProcessPayroll",
    "vigencesUsed": [
      {
        "ruleName": "INSS",
        "vigenceId": 4001,
        "startDate": "2025-01-01"
      },
      {
        "ruleName": "IRRF",
        "vigenceId": 5002,
        "startDate": "2025-01-01"
      }
    ]
  }
}
```

**Exposição da Memória via API:**

```csharp
[HttpGet("api/payroll/{processingId}/items/{itemId}/memory")]
public async Task<IActionResult> GetCalculationMemory(Guid processingId, Guid itemId)
{
    var memory = await _memoryRepo.GetByItemIdAsync(itemId);
    
    return Ok(JsonConvert.DeserializeObject(memory.CalculationJSON));
}
```

### 10.3 Como Garantir Confiabilidade Jurídica e Contábil

**Requisitos legais:**

1. **Imutabilidade:** Resultado processado não pode ser alterado
   - Implementar com CONSTRAINT CHECK ou TRIGGER
   - Teste: tentar UPDATE em tabela de resultado deve falhar

2. **Rastreabilidade completa:** Quem, quando, qual versão
   - AuditLog com UserId, Timestamp, ProcessingVersion
   - CalculationMemory com vigências usadas
   - Teste: conseguir reconstruir cálculo com mesmos dados

3. **Certificação digital (opcional mas recomendado)**
   - Assinar processamento com certificado A1/A3
   - Adicionar campo `DigitalSignature` em Processing
   - Hash imutável do resultado

4. **Compliance com legislação**
   - IRRF segue tabelas oficiais do governo
   - INSS segue teto de contribuição
   - FGTS depositado em 8 dias úteis
   - Teste: validar contra legislação vigente

5. **Conservação de histórico**
   - Nunca deletar dados históricos
   - Manter por no mínimo 5 anos (retenção legal)
   - Backup regular

**Implementação de certificação:**

```csharp
public class CertifyProcessingUseCase
{
    public async Task<CertificationResult> Execute(CertifyProcessingCommand cmd)
    {
        var processing = await _repo.GetByIdAsync(cmd.ProcessingId);
        
        if (processing.Status != ProcessingStatus.Completed)
            throw new InvalidOperationException("Only completed processings can be certified");
        
        // Gerar hash imutável
        var payload = JsonConvert.SerializeObject(new {
            processing.Id,
            processing.CompanyId,
            processing.Competence,
            processing.Version,
            ItemIds = processing.Items.Select(x => x.Id).OrderBy(x => x)
        });
        
        var hash = SHA256.ComputeHash(Encoding.UTF8.GetBytes(payload));
        
        // Assinar com certificado digital
        var signature = await _digitalSignatureService.SignAsync(hash, cmd.CertificateThumbprint);
        
        // Salvar certificação
        processing.Certify(signature, DateTime.UtcNow, cmd.SignedBy);
        await _repo.SaveAsync(processing);
        
        return new CertificationResult
        {
            ProcessingId = processing.Id,
            CertifiedAt = DateTime.UtcNow,
            Signature = signature,
            Hash = Convert.ToBase64String(hash)
        };
    }
}
```

---

## 11. Regras de Evolução do Sistema

### 11.1 Como Adicionar Novos Eventos / Rubricas

**Processo seguro para não quebrar histórico:**

**Passo 1: Definir nova Rubrica**

```csharp
// Domain/Events/NewRubricAdded.cs
public class NewRubricAddedEvent : DomainEvent
{
    public RubricId RubricId { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public RubricType Type { get; set; }
    public DateTime EffectiveDate { get; set; }
}

// Domain/Entities/Rubric.cs
public class Rubric
{
    public RubricId Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public RubricType Type { get; private set; }
    public IList<RubricVigence> Vigences { get; private set; } = new List<RubricVigence>();
    
    public static Rubric Create(string code, string name, RubricType type, DateTime effectiveDate)
    {
        var rubric = new Rubric 
        { 
            Id = RubricId.New(),
            Code = code,
            Name = name,
            Type = type
        };
        
        // Primeira vigência
        rubric.Vigences.Add(RubricVigence.Create(
            effectiveDate: effectiveDate,
            endDate: null,  // Indefinida
            formula: null));  // Será definida depois
        
        rubric.AddDomainEvent(new NewRubricAddedEvent { ... });
        return rubric;
    }
}
```

**Passo 2: Garantir compatibilidade retroativa**

```csharp
public class SalaryCalculator
{
    public CalculationResult Calculate(
        Employee emp,
        IList<RubricValue> rubrics,
        Competence competence,
        ITaxRuleProvider taxRules)
    {
        // Rubrica nova? Se competência antiga, não incluir
        var rubricsTouse = rubrics.Where(r => 
        {
            var vigence = r.Rubric.GetVigenceFor(competence);
            return vigence != null;  // Só usar se houver vigência para essa competência
        }).ToList();
        
        // Cálculo continua normal
        // Folhas antigas não são afetadas
    }
}
```

**Passo 3: Testar retroativamente**

```csharp
[Test]
public async Task WhenAddingNewRubric_OldProcessingsShouldNotBeAffected()
{
    // Processar jan/2025
    var result1 = await calculator.Calculate(emp, rubricsJan2025, Competence.FromYearMonth(2025, 1));
    var total1 = result1.GrossSalary;
    
    // Adicionar nova rubrica com vigência a partir de fev/2025
    var newRubric = Rubric.Create("BONUS", "Bônus", RubricType.Earning, DateTime.Parse("2025-02-01"));
    
    // Reprocessar jan/2025 com nova rubrica no catálogo
    var result1_Again = await calculator.Calculate(emp, rubricsJan2025, Competence.FromYearMonth(2025, 1));
    
    // Resultado deve ser IDÊNTICO (rubrica sem vigência em jan não é usada)
    Assert.AreEqual(total1, result1_Again.GrossSalary);
}
```

### 11.2 Como Alterar Cálculo Sem Quebrar Histórico

**Cenário:** Descobriu-se que cálculo de IRRF estava errado. Como corrigir?

**Passo 1: Criar nova versão da regra**

```csharp
// Criar nova vigência corrigida
var currentVigence = taxRules.GetVigenceFor(competence);  // Vigência com erro

// Encerrar vigência com erro
currentVigence.EndDate = DateTime.Parse("2025-01-14");

// Criar nova vigência corrigida (a partir de 2025-01-15)
var correctedVigence = TaxRuleVigence.Create(
    ruleId: taxRules.Id,
    startDate: DateTime.Parse("2025-01-15"),
    endDate: null,
    percentage: 7.5m,  // Valor corrigido
    reason: "Correção de cálculo - Bug fix #1234"
);

await _taxRuleRepository.SaveAsync(correctedVigence);
```

**Passo 2: Marcar processamentos anteriores para reprocessamento**

```csharp
// Encontrar todos processamentos afetados
var affectedProcessings = await _processingRepo.FindByCompetenceRangeAsync(
    startDate: DateTime.Parse("2025-01-01"),
    endDate: DateTime.Parse("2025-01-14"));

foreach (var processing in affectedProcessings)
{
    processing.MarkForReprocessing(reason: "IRRF calculation bug fix");
    await _processingRepo.SaveAsync(processing);
    
    // Enfileirar novo job com versão incrementada
    await _jobScheduler.ScheduleAsync<ReprocessPayrollJob>(
        new ReprocessPayrollJobArgs { ProcessingId = processing.Id });
}
```

**Passo 3: Garantir auditoria completa**

```csharp
// AuditLog registra mudança
await _auditLog.RecordAsync(new AuditEntry
{
    EntityType = "TaxRule",
    EntityId = taxRules.Id.ToString(),
    Action = "CORRECTED",
    OldValue = JsonConvert.SerializeObject(currentVigence),
    NewValue = JsonConvert.SerializeObject(correctedVigence),
    ReasonForChange = "Bug fix #1234 - IRRF calculation error",
    UserId = User.Id,
    Timestamp = DateTime.UtcNow
});

// CalculationMemory da nova versão vai mencionar a correção
// "Vigência 2025-01-15 (corrigida via bug fix #1234)"
```

### 11.3 Como Criar Novas Integrações

**Padrão: Event-Driven Integrations**

```csharp
// 1. Processing completa → evento publicado
public class ProcessingCompletedEvent : DomainEvent
{
    public ProcessingId ProcessingId { get; set; }
    public int CompanyId { get; set; }
    public Competence Competence { get; set; }
}

// 2. Handler de integração subscreve evento
public class eSocialIntegrationHandler
{
    private readonly IeSocialApiClient _eSocialClient;
    
    public async Task Handle(ProcessingCompletedEvent @event)
    {
        try
        {
            // Buscar processamento
            var processing = await _processingRepo.GetByIdAsync(@event.ProcessingId);
            
            // Transformar para eSocial
            var eSocialPayload = _eSocialTransformer.Transform(processing);
            
            // Enviar
            var result = await _eSocialClient.SendAsync(eSocialPayload);
            
            // Registrar sucesso
            await _integrationLog.LogSuccessAsync(
                integrationName: "eSocial",
                processingId: @event.ProcessingId,
                externalId: result.Id);
        }
        catch (Exception ex)
        {
            // Registrar erro
            await _integrationLog.LogErrorAsync(
                integrationName: "eSocial",
                processingId: @event.ProcessingId,
                error: ex.Message);
            
            // Queue para retry
            await _jobScheduler.ScheduleRetryAsync<eSocialIntegrationHandler>(@event, delaySeconds: 300);
        }
    }
}

// 3. Registrar handler no startup
services.AddScoped<eSocialIntegrationHandler>();
services.Subscribe<ProcessingCompletedEvent, eSocialIntegrationHandler>();
```

**Vantagens:**
- Integração não bloqueia processamento
- Retry automático em caso de falha
- Histórico de integrações auditável
- Fácil adicionar nova integração sem alterar core

### 11.4 Como Refatorar Sem Violar o Mapa

**Regra de Ouro:** Se refatora muda o resultado de um cálculo, não refatore sem reprocessar.

**Exemplo de refatora SEGURA:**

```csharp
// ❌ ANTES - Código repetido
public decimal CalculateIRRF(decimal grossSalary, decimal inss)
{
    var base = grossSalary - inss;
    if (base <= 2112) return 0;
    if (base <= 4134) return (base - 2112) * 0.075m;
    // ... mais bandas
}

public decimal CalculateDeduction(decimal gross, decimal percentage)
{
    var value = gross * percentage;
    if (value > gross) value = gross;
    return value;
}

// ✅ DEPOIS - Extrair lógica compartilhada
public abstract class TaxCalculator
{
    protected decimal ApplyBandedTax(decimal base, IList<TaxBand> bands)
    {
        decimal tax = 0;
        foreach (var band in bands)
        {
            var taxableInBand = Math.Min(base, band.UpperLimit) - band.LowerLimit;
            if (taxableInBand > 0)
                tax += taxableInBand * band.Rate;
        }
        return tax;
    }
}

// ⚠️ Mas antes de usar, validar:
[Test]
public void RefactoredCalculatorShouldProduceSameResults()
{
    var inputs = LoadTestCasesFromRealCalculations();
    
    foreach (var input in inputs)
    {
        var oldResult = OldCalculator.Calculate(input);
        var newResult = NewCalculator.Calculate(input);
        
        Assert.AreEqual(oldResult, newResult, "Refactor broke calculation!");
    }
}
```

---

## 12. Checklist de Validação Arquitetural

Use este checklist para validar se novo código respeita este mapa.

### 12.1 Checklist Técnico

**Separação de Camadas:**
- [ ] Existe lógica de cálculo em Controllers? ❌ VIOLAÇÃO
- [ ] Existe acesso a BD em Domain Services? ❌ VIOLAÇÃO
- [ ] Existe UI logic em Application Services? ❌ VIOLAÇÃO
- [ ] Domain importa Infrastructure? ❌ VIOLAÇÃO
- [ ] Dependências vão do externo para o interno (API → Application → Domain)? ✅

**Determinismo:**
- [ ] Há chamadas a `DateTime.Now` em cálculos? ❌ VIOLAÇÃO
- [ ] Há uso de `Random` ou valores aleatórios? ❌ VIOLAÇÃO
- [ ] Há chamadas HTTP dentro de funções de cálculo? ❌ VIOLAÇÃO
- [ ] Há modificação de estado compartilhado? ❌ VIOLAÇÃO
- [ ] Teste garante: mesma entrada → mesma saída? ✅

**Imutabilidade:**
- [ ] Há UPDATE em tabelas de resultado? ❌ VIOLAÇÃO
- [ ] Há DELETE em tabelas de resultado? ❌ VIOLAÇÃO
- [ ] Há SQL TRIGGER permitindo modificação? ❌ VIOLAÇÃO
- [ ] Resultado finalizado tem `IsFinal = 1` com CHECK constraint? ✅

**Versionamento:**
- [ ] Processing novo tem incremento automático de versão? ✅
- [ ] Versão anterior é preservada como histórico? ✅
- [ ] Relatório menciona qual versão foi usada? ✅

**Vigências:**
- [ ] Regra alterada cria nova vigência, não sobrescreve? ✅
- [ ] Histórico de vigências é preservado? ✅
- [ ] Cálculo usa vigência correta para competência? ✅

**Auditoria:**
- [ ] AuditLog tem UserId, Action, OldValue, NewValue, Timestamp? ✅
- [ ] CalculationMemory registra cada passo com vigência? ✅
- [ ] Resultado imutável é rastreável? ✅

### 12.2 Checklist de Lógica de Negócio

**Pipeline de Cálculo:**
- [ ] Ordem é: Earnings → Deductions → INSS → IRRF → FGTS → Consignados → Net? ✅
- [ ] Cada etapa depende apenas da anterior? ✅
- [ ] Não há branches aleatórios no pipeline? ✅
- [ ] Reprocessamento mantém ordem e determinismo? ✅

**Processamento Assíncrono:**
- [ ] HTTP POST retorna em <100ms? ✅
- [ ] Job é enfileirado, não executado sincronamente? ✅
- [ ] Há proteção contra processamento concorrente (lock)? ✅
- [ ] Falha em um employee não quebra processamento de outros? ✅
- [ ] Job falho faz retry automático? ✅

**Dados de Entrada:**
- [ ] Employee validado como ativo em competência? ✅
- [ ] Rubricas validadas como existentes e vigentes? ✅
- [ ] Valores de entrada validados como não-negativos? ✅
- [ ] TaxRules vigentes para a competência são usadas? ✅

**Resultado:**
- [ ] GrossSalary = ∑ Earnings? ✅
- [ ] NetSalary = GrossSalary - ∑(Deductions) - ∑(Taxes) - ∑(Consignados)? ✅
- [ ] NetSalary ≥ 0? ✅
- [ ] Consignados não excedem margem? ✅
- [ ] IRRF segue tabela oficial? ✅
- [ ] INSS respeita teto? ✅
- [ ] FGTS é 8% ou especial definido? ✅

### 12.3 Checklist de Operação

**Reprocessamento:**
- [ ] Versão anterior fica intacta? ✅
- [ ] Nova versão tem Version incrementada? ✅
- [ ] Versão anterior marcada como "superseded"? ✅
- [ ] Logs mencionam qual versão foi emitida? ✅

**Integrações:**
- [ ] Integração subscreve evento, não polling? ✅
- [ ] Integração não bloqueia processamento? ✅
- [ ] Erro de integração não quebra folha? ✅
- [ ] Retry registrado com timestamp? ✅

**Relatórios:**
- [ ] Mostra versão do processamento? ✅
- [ ] Link para memória de cálculo? ✅
- [ ] Exportação mantém vigências usadas? ✅

### 12.4 Checklist de Testes

**Testes unitários:**
- [ ] Domain Services testados sem dependências externas? ✅
- [ ] Value Objects têm testes de igualdade? ✅
- [ ] Agregados testam invariantes? ✅

**Testes de integração:**
- [ ] Cálculo reproduz resultado esperado? ✅
- [ ] Refatora não quebra cálculos históricos? ✅
- [ ] Versionamento funciona corretamente? ✅

**Testes de aceitação:**
- [ ] Fluxo end-to-end: Requisição → Job → Resultado? ✅
- [ ] Reprocessamento mantém histórico? ✅
- [ ] Memória de cálculo explica resultado? ✅

**Testes de conformidade:**
- [ ] IRRF reflete tabela governamental? ✅
- [ ] INSS respeita limites legais? ✅
- [ ] FGTS depositado conforme lei? ✅

### 12.5 Checklist Antes de Deploy

**Antes de versionar código:**
- [ ] Executou checklist técnico? ✅
- [ ] Executou checklist de lógica de negócio? ✅
- [ ] Testes passam (unit + integration)? ✅
- [ ] Code review validou contra este mapa? ✅
- [ ] Documentação atualizada? ✅

**Antes de mesclar para main:**
- [ ] Nenhuma violação arquitetural? ✅
- [ ] Regressões testadas? ✅
- [ ] Performance aceitável? ✅
- [ ] Logs e auditoria funcionam? ✅

---

## Referências e Glossário

**Termos críticos:**

| Termo | Definição |
|-------|-----------|
| **Competência** | Mês de referência do processamento (jan/2025) |
| **Vigência** | Período em que uma regra é válida |
| **Processing** | Execução do cálculo para uma competência em uma empresa |
| **Processing Item** | Resultado de cálculo para um employee em um processing |
| **CalculationMemory** | Documento imutável com todos os passos do cálculo |
| **GrossSalary** | Total de proventos antes de descontos |
| **NetSalary** | Salário final após todos descontos e impostos |
| **Margem Consignada** | Percentual disponível para desconto de consignados |
| **Rubric** | Componente de folha (rubrica) - pode ser provento ou desconto |
| **Determinismo** | Propriedade de função que sempre produz mesma saída para mesma entrada |
| **Imutabilidade** | Dado que não pode ser alterado após criação |
| **Idempotência** | Propriedade de operação que pode ser repetida sem efeitos colaterais |

---

### 2.X Linguagem Oficial do Sistema

A linguagem oficial e obrigatória para implementação deste sistema é **C#**.

#### Regras:

- Todo o código de domínio, aplicação, infraestrutura, API e front-end
  deve ser escrito em **C#**.
- Não é permitido misturar linguagens no core do sistema
  (ex: Java, Kotlin, Python, JavaScript para lógica de negócio).
- Exemplos de código, padrões e abstrações devem sempre considerar
  as boas práticas da linguagem C# e do ecossistema .NET.


  ### 2.X Idioma Oficial do Sistema

Este sistema adota **PORTUGUÊS como idioma oficial e único**, incluindo:

- Classes
- Métodos
- Namespaces
- Exceções
- Comentários
- Logs
- Banco de dados
- Mensagens de erro
- Relatórios

#### Regras obrigatórias:





**STATUS:** Documento aprovado como Fonte da Verdade da Arquitetura  
**Versão:** 1.0 Completa (Seções 1-12)  
**Próximas atualizações:** Quando legislação mudar ou novas integrações forem adicionadas  
**Proprietário:** Arquiteto de Software Sênior  
**Revisão:** Anual ou conforme necessidade

