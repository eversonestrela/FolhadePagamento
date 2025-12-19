# INFRASTRUCTURE DATA MODEL - Sistema de Folha de Pagamento

**Versão:** 1.0  
**Data:** Dezembro 2025  
**Status:** Documento Conceitual - Fase de Design  
**Escopo:** Modelo de dados para persistência do Core de Folha v0.8  
**Banco Alvo:** SQL Server (implementação futura)

---

## 1. Visão Geral da Infraestrutura de Persistência

### 1.1 Propósito deste Documento

Este documento descreve **conceituamente** o modelo de dados necessário para persistir
o Core de Folha de Pagamento (v0.8), respeitando integralmente o ARCHITECTURE_MAP.md.

**O que este documento é:**
- Um guia conceitual para futura implementação em SQL Server
- Uma definição de entidades de persistência e seus relacionamentos
- Um conjunto de estratégias de versionamento, imutabilidade e auditoria
- Uma justificativa arquitetural para cada decisão

**O que este documento NÃO é:**
- DDL (CREATE TABLE, ALTER TABLE)
- Código de migrations
- Implementação de Repository
- Especificação de API ou front-end

### 1.2 Princípio Fundamental: O Banco Persiste, Não Decide

O Core de Folha (v0.8) é a **única fonte de verdade** para regras de negócio.
O banco de dados tem uma única responsabilidade:

> **Armazenar fielmente o que o Core calculou e decidiu, sem jamais modificar,
> reinterpretar ou aplicar regras de negócio próprias.**

**Implicações:**
- Nenhuma stored procedure calcula INSS, IRRF, FGTS ou consignados
- Nenhuma trigger valida margem consignável ou teto de contribuição
- Nenhum constraint aplica regras de vigência de tabelas tributárias
- Toda validação de negócio acontece **antes** da persistência, no Core

O banco é um **repositório passivo de verdades calculadas**.

### 1.3 Alinhamento com Clean Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        DOMÍNIO (Core v0.8)                      │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  - ResultadoCalculo (imutável)                          │    │
│  │  - ProcessamentoVersao (versionado)                     │    │
│  │  - HistoricoProcessamento (agregado)                    │    │
│  │  - CalculadoraFolha, CalculadoraInss, etc.              │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼ (Ports - Interfaces)
┌─────────────────────────────────────────────────────────────────┐
│                     APLICAÇÃO (Casos de Uso)                    │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  - IProcessamentoRepositorio (interface)                │    │
│  │  - IHistoricoProcessamentoRepositorio (interface)       │    │
│  │  - IFuncionarioRepositorio (interface)                  │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼ (Adapters - Implementações)
┌─────────────────────────────────────────────────────────────────┐
│                   INFRAESTRUTURA (Persistência)                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  - ProcessamentoRepositorio (Entity Framework)          │    │
│  │  - Mapeamento Entidade ↔ Tabela                         │    │
│  │  - Este documento descreve este nível                   │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       SQL SERVER (Banco)                        │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  - Tabelas físicas                                      │    │
│  │  - Índices para performance                             │    │
│  │  - Constraints de integridade referencial               │    │
│  │  - SEM regras de negócio                                │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Conceito de Processamento e Versionamento no Banco

### 2.1 O Que é um Processamento

Um **Processamento** representa uma execução específica do cálculo de folha
para um funcionário em uma competência.

**Características:**
- Cada processamento tem um **identificador único** (GUID)
- Cada processamento pertence a **uma competência** (ano-mês)
- Cada processamento pertence a **um funcionário**
- Cada processamento tem uma **versão** (V1, V2, V3...)
- Cada processamento tem um **status** (EmProcessamento, Finalizado, Cancelado, Superado)

> ⚠️ **ESCLARECIMENTO IMPORTANTE:**
>
> O conceito de "Processamento" (agregado lógico de todas as versões de uma competência)
> é **puramente conceitual** e **não existe como tabela física separada**.
>
> Fisicamente, existe apenas `ProcessamentoVersaoDb` (cada versão é um registro).
>
> O "status atual" de um processamento é **DERIVADO** — obtido buscando a última
> `ProcessamentoVersaoDb` com `Status = Finalizado` para o par (FuncionarioId, Competência).
>
> **Nunca haverá uma coluna `StatusAtual` em tabela separada de agregado.**
> Isso evita divergência entre o status "calculado" e o status "armazenado".

### 2.2 O Que é Versionamento

**Conceito:** Cada vez que uma competência é (re)calculada, uma nova versão é criada.

```
Funcionário: João Silva
Competência: Janeiro/2025

┌─────────────────────────────────────────────────────────────┐
│ V1 (15/01/2025 10:30)                                       │
│   Status: SUPERADO                                          │
│   Resultado: Líquido R$ 3.500,00                            │
│   Motivo superação: V2 criada                               │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼ (reprocessamento)
┌─────────────────────────────────────────────────────────────┐
│ V2 (20/01/2025 14:15)                                       │
│   Status: SUPERADO                                          │
│   Resultado: Líquido R$ 3.450,00 (correção IRRF)            │
│   Motivo: CorrecaoCalculo                                   │
│   VersaoAnteriorId: V1                                      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼ (reprocessamento)
┌─────────────────────────────────────────────────────────────┐
│ V3 (25/01/2025 09:00) ← VERSÃO ATUAL                        │
│   Status: FINALIZADO                                        │
│   Resultado: Líquido R$ 3.480,00 (atualização legislação)   │
│   Motivo: AtualizacaoLegislacao                             │
│   VersaoAnteriorId: V2                                      │
└─────────────────────────────────────────────────────────────┘
```

### 2.3 Por Que Versionar (Justificativa Arquitetural)

| Requisito | Como o Versionamento Atende |
|-----------|----------------------------|
| Auditoria | Versões anteriores preservadas para rastrear decisões |
| Conformidade Legal | Cada versão é um artefato para contencioso trabalhista |
| Correção de Erros | Erros geram nova versão, não alteram dados existentes |
| Mudança de Legislação | Recálculos retroativos mantêm histórico |
| Determinismo | Mesma entrada sempre gera mesma saída; versão registra qual entrada foi usada |

### 2.4 Chave Natural de Versionamento

A combinação que identifica univocamente um processamento no contexto de versão:

```
(FuncionarioId, Competencia, VersaoNumero)
```

**Exemplos:**
- `(GUID-123, 2025-01, 1)` = V1 de Janeiro/2025 para funcionário GUID-123
- `(GUID-123, 2025-01, 2)` = V2 de Janeiro/2025 para funcionário GUID-123
- `(GUID-456, 2025-01, 1)` = V1 de Janeiro/2025 para funcionário GUID-456

---

## 3. Entidades de Persistência (Conceituais)

### 3.1 Mapeamento Domínio → Persistência

| Entidade de Domínio | Entidade de Persistência | Descrição |
|---------------------|--------------------------|-----------|
| `ProcessamentoVersao` | `ProcessamentoVersaoDb` | Processamento versionado individual |
| `HistoricoProcessamento` | Não persiste diretamente | Agregado reconstituído de ProcessamentoVersaoDb |
| `ResultadoCalculo` | `ResultadoCalculoDb` | Snapshot do resultado (1:1 com ProcessamentoVersao) |
| `Funcionario` | `FuncionarioDb` | Dados cadastrais do funcionário |
| `Competencia` | Coluna em ProcessamentoVersaoDb | Value Object armazenado como ano/mês |

### 3.2 Entidade: ProcessamentoVersaoDb

**Propósito:** Persistir cada versão de processamento como registro imutável.

**Campos conceituais:**

| Campo | Tipo Conceitual | Descrição | Origem no Domínio |
|-------|-----------------|-----------|-------------------|
| Id | GUID | Identificador único | `ProcessamentoId.Valor` |
| FuncionarioId | GUID | FK para Funcionário | `ProcessamentoVersao.FuncionarioId` |
| CompetenciaAno | Inteiro | Ano da competência | `Competencia.Ano` |
| CompetenciaMes | Inteiro | Mês da competência | `Competencia.Mes` |
| VersaoNumero | Inteiro | Número da versão (1, 2, 3...) | `VersaoProcessamento.Numero` |
| Status | Enum/String | EmProcessamento, Finalizado, Cancelado, Superado | `StatusProcessamento` |
| IniciadoEm | DateTime | Timestamp de início | `ProcessamentoVersao.IniciadoEm` |
| FinalizadoEm | DateTime? | Timestamp de finalização | `ProcessamentoVersao.FinalizadoEm` |
| SuperadoEm | DateTime? | Quando foi superado por nova versão | `ProcessamentoVersao.SuperadoEm` |
| MotivoReprocessamento | String? | Código do motivo | `MotivoReprocessamento.Codigo` |
| DescricaoReprocessamento | String? | Descrição do motivo | `MotivoReprocessamento.Descricao` |
| VersaoAnteriorId | GUID? | Referência à versão anterior | `ProcessamentoVersao.VersaoAnteriorId` |
| UsuarioId | String? | Quem executou | `ProcessamentoVersao.UsuarioId` |
| HashResultado | String? | Hash de integridade | `ProcessamentoVersao.HashResultado` |
| CriadoEm | DateTime | Timestamp de criação do registro | Infraestrutura |

**Características:**
- **Imutável após finalização** (ver Seção 6)
- **Nunca deletado** (ver Seção 6)
- **Índice único** em (FuncionarioId, CompetenciaAno, CompetenciaMes, VersaoNumero)

### 3.3 Entidade: ResultadoCalculoDb

**Propósito:** Armazenar o snapshot completo do resultado de cálculo.

**Campos conceituais:**

| Campo | Tipo Conceitual | Descrição | Origem no Domínio |
|-------|-----------------|-----------|-------------------|
| Id | GUID | Identificador único | Gerado na persistência |
| ProcessamentoVersaoId | GUID | FK para ProcessamentoVersaoDb | Relacionamento 1:1 |
| SalarioBruto | Decimal | Valor bruto | `ResultadoCalculo.SalarioBruto` |
| ValorInss | Decimal | Desconto INSS | `ResultadoCalculo.ValorInss` |
| ValorIrrf | Decimal | Desconto IRRF | `ResultadoCalculo.ValorIrrf` |
| ValorFgts | Decimal | Encargo patronal FGTS | `ResultadoCalculo.ValorFgts` |
| ValorConsignados | Decimal | Total consignados | `ResultadoCalculo.ValorConsignados` |
| TotalDescontos | Decimal | Soma de descontos | `ResultadoCalculo.TotalDescontos` |
| SalarioLiquido | Decimal | Valor líquido | `ResultadoCalculo.SalarioLiquido` |
| TotalEncargosPatronais | Decimal | FGTS + futuros | `ResultadoCalculo.TotalEncargosPatronais` |
| CustoTotalEmpregador | Decimal | Custo total empresa | `ResultadoCalculo.CustoTotalEmpregador` |
| CalculadoEm | DateTime | Timestamp do cálculo | `ResultadoCalculo.CalculadoEm` |

**Relacionamento:** 1:1 com ProcessamentoVersaoDb

### 3.4 Entidade: DetalheInssDb

**Propósito:** Armazenar memória de cálculo do INSS.

**Campos conceituais:**

| Campo | Tipo Conceitual | Descrição |
|-------|-----------------|-----------|
| Id | GUID | Identificador único |
| ResultadoCalculoId | GUID | FK para ResultadoCalculoDb |
| BaseCalculo | Decimal | Base usada no cálculo |
| TabelaIdUsada | String | Identificador da tabela INSS usada |
| AliquotaEfetiva | Decimal | Alíquota resultante |
| TetoAplicado | Boolean | Se atingiu o teto |
| ContribuicaoPorFaixaJson | JSON | Detalhes por faixa progressiva |

### 3.5 Entidade: DetalheIrrfDb

**Propósito:** Armazenar memória de cálculo do IRRF.

**Campos conceituais:**

| Campo | Tipo Conceitual | Descrição |
|-------|-----------------|-----------|
| Id | GUID | Identificador único |
| ResultadoCalculoId | GUID | FK para ResultadoCalculoDb |
| BaseCalculo | Decimal | Base após deduções |
| DeducaoInss | Decimal | Valor do INSS deduzido |
| NumeroDependentes | Inteiro | Dependentes considerados |
| DeducaoPorDependente | Decimal | Valor deduzido por dependente |
| TabelaIdUsada | String | Identificador da tabela IRRF usada |
| FaixaAplicada | String | Descrição da faixa |
| AliquotaAplicada | Decimal | Alíquota da faixa |
| ParcelaDedutivelUsada | Decimal | Parcela a deduzir |
| Isento | Boolean | Se ficou isento |

### 3.6 Entidade: DetalheFgtsDb

**Propósito:** Armazenar memória de cálculo do FGTS.

**Campos conceituais:**

| Campo | Tipo Conceitual | Descrição |
|-------|-----------------|-----------|
| Id | GUID | Identificador único |
| ResultadoCalculoId | GUID | FK para ResultadoCalculoDb |
| BaseCalculo | Decimal | Base do FGTS |
| TabelaIdUsada | String | Identificador da tabela FGTS usada |
| AliquotaAplicada | Decimal | Alíquota usada (normalmente 8%) |
| TipoContribuinte | String | Normal, Aprendiz, Doméstico |

### 3.7 Entidade: DetalheConsignadosDb

**Propósito:** Armazenar memória de cálculo dos consignados.

**Campos conceituais:**

| Campo | Tipo Conceitual | Descrição |
|-------|-----------------|-----------|
| Id | GUID | Identificador único |
| ResultadoCalculoId | GUID | FK para ResultadoCalculoDb |
| SalarioBaseConsiderado | Decimal | Base para margem |
| PercentualMargem | Decimal | Percentual da margem |
| MargemTotal | Decimal | Valor máximo descontável |
| MargemUtilizada | Decimal | Valor efetivamente usado |
| MargemDisponivel | Decimal | Saldo restante |
| TotalContratosAtivos | Inteiro | Quantidade de contratos |
| DescontosJson | JSON | Array de descontos por contrato |

### 3.8 Entidade: FuncionarioDb (Cadastro)

**Propósito:** Persistir dados cadastrais do funcionário.

**Campos conceituais:**

| Campo | Tipo Conceitual | Descrição |
|-------|-----------------|-----------|
| Id | GUID | Identificador único |
| Nome | String | Nome completo |
| SalarioBase | Decimal | Salário contratual |
| DataAdmissao | Date? | Data de admissão |
| Ativo | Boolean | Se está ativo |
| CriadoEm | DateTime | Timestamp de criação |
| AtualizadoEm | DateTime? | Timestamp de atualização |

**Observação:** Esta é uma entidade **mutável** (cadastro), diferente das
entidades de resultado que são **imutáveis**.

> ⚠️ **IMPORTANTE: Isolamento do Core**
>
> O **Core de Cálculo NÃO depende de FuncionarioDb**.
>
> O Core recebe um objeto `Funcionario` (entidade de domínio) como entrada,
> que pode ter sido carregado de FuncionarioDb, de um arquivo, ou de qualquer fonte.
>
> FuncionarioDb existe apenas para:
> - **Contexto:** Manter cadastro para consultas e relatórios
> - **Vínculo histórico:** FK em ProcessamentoVersaoDb para rastreabilidade
>
> **Consequências:**
> - ❌ Nunca recalcular histórico porque FuncionarioDb.SalarioBase mudou
> - ❌ Nunca acoplar lógica de cálculo a "salário atual" do cadastro
> - ✅ Cada ProcessamentoVersaoDb armazena o snapshot do salário usado (em ResultadoCalculoDb)

---

## 4. Relacionamentos Conceituais

### 4.1 Diagrama de Relacionamentos

```
┌─────────────────────┐
│   FuncionarioDb     │
│  (Cadastro Mutável) │
└──────────┬──────────┘
           │
           │ 1:N (um funcionário tem várias versões de processamento)
           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    ProcessamentoVersaoDb                        │
│                     (Imutável após finalização)                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ Id (PK)                                                 │    │
│  │ FuncionarioId (FK)                                      │    │
│  │ CompetenciaAno, CompetenciaMes                          │    │
│  │ VersaoNumero                                            │    │
│  │ Status                                                  │    │
│  │ VersaoAnteriorId (FK self-reference) ─────────────┐     │    │
│  │ ...                                               │     │    │
│  └───────────────────────────────────────────────────│─────┘    │
│                                                      │          │
│                                                      └──────────┤
│                   (uma versão pode apontar para versão anterior)│
└─────────────────────────────────────────────────────────────────┘
           │
           │ 1:1 (cada versão tem exatamente um resultado)
           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    ResultadoCalculoDb                           │
│                     (Imutável - snapshot)                       │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ Id (PK)                                                 │    │
│  │ ProcessamentoVersaoId (FK, unique)                      │    │
│  │ SalarioBruto, ValorInss, ValorIrrf, ...                 │    │
│  │ SalarioLiquido, CustoTotalEmpregador                    │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
           │
           ├── 1:0..1 (resultado pode ter detalhe INSS)
           │           ▼
           │   ┌─────────────────┐
           │   │  DetalheInssDb  │
           │   └─────────────────┘
           │
           ├── 1:0..1 (resultado pode ter detalhe IRRF)
           │           ▼
           │   ┌─────────────────┐
           │   │  DetalheIrrfDb  │
           │   └─────────────────┘
           │
           ├── 1:0..1 (resultado pode ter detalhe FGTS)
           │           ▼
           │   ┌─────────────────────┐
           │   │  DetalheFgtsDb      │
           │   └─────────────────────┘
           │
           └── 1:0..1 (resultado pode ter detalhe Consignados)
                       ▼
               ┌─────────────────────────┐
               │  DetalheConsignadosDb   │
               └─────────────────────────┘
```

### 4.2 Relacionamentos Chave

| De | Para | Cardinalidade | Descrição |
|----|------|---------------|-----------|
| FuncionarioDb | ProcessamentoVersaoDb | 1:N | Funcionário tem múltiplos processamentos |
| ProcessamentoVersaoDb | ProcessamentoVersaoDb | N:1 | Versão aponta para versão anterior (self-reference) |
| ProcessamentoVersaoDb | ResultadoCalculoDb | 1:1 | Cada versão tem exatamente um resultado |
| ResultadoCalculoDb | DetalheInssDb | 1:0..1 | Resultado pode ter detalhamento INSS |
| ResultadoCalculoDb | DetalheIrrfDb | 1:0..1 | Resultado pode ter detalhamento IRRF |
| ResultadoCalculoDb | DetalheFgtsDb | 1:0..1 | Resultado pode ter detalhamento FGTS |
| ResultadoCalculoDb | DetalheConsignadosDb | 1:0..1 | Resultado pode ter detalhamento Consignados |

### 4.3 Self-Reference: Cadeia de Versões

A coluna `VersaoAnteriorId` em `ProcessamentoVersaoDb` cria uma cadeia linked list:

```
V3 ──(VersaoAnteriorId)──► V2 ──(VersaoAnteriorId)──► V1 ──(VersaoAnteriorId)──► NULL
```

**Benefícios:**
- Rastreabilidade completa de reprocessamentos
- Navegação bidirecional (V1→V2→V3 ou V3→V2→V1)
- Cada versão conhece sua origem

---

## 5. Estratégia de Versionamento

### 5.1 Regras de Versionamento

| Regra | Descrição | Implementação |
|-------|-----------|---------------|
| Versão sempre incremental | V1 → V2 → V3 | `VersaoNumero = MAX(VersaoNumero) + 1` para mesmo Funcionário+Competência |
| Sem gaps | Não pode haver V1, V3 sem V2 | Constraint ou validação no Application Layer |
| Versão anterior obrigatória para V2+ | V2 deve ter VersaoAnteriorId | Validação no Core antes de persistir |
| Status Superado automático | Quando V2 é finalizada, V1 vira Superado | Core marca SuperadoEm antes de persistir |

### 5.2 Estados de Versão

```
┌───────────────────┐
│  EmProcessamento  │ ← Estado inicial (cálculo em andamento)
└─────────┬─────────┘
          │
          ├──────────────────────────────────┐
          ▼                                  ▼
┌───────────────────┐              ┌───────────────────┐
│    Finalizado     │              │    Cancelado      │
│  (versão válida)  │              │ (descartado)      │
└─────────┬─────────┘              └───────────────────┘
          │
          │ (quando nova versão é finalizada)
          ▼
┌───────────────────┐
│     Superado      │ ← Histórico (nova versão assumiu)
│  (obsoleto)       │
└───────────────────┘
```

### 5.3 Consulta de Versão Atual

**Conceito:** A versão atual de uma competência é a última Finalizada (não Superada).

```
Para Funcionário X, Competência 2025-01:

SELECT TOP 1 * 
FROM ProcessamentoVersaoDb
WHERE FuncionarioId = X
  AND CompetenciaAno = 2025 
  AND CompetenciaMes = 1
  AND Status = 'Finalizado'
ORDER BY VersaoNumero DESC
```

**Observação:** O conceito acima é para referência. A query exata será
definida na implementação de Repository.

---

## 6. Estratégia de Imutabilidade

### 6.1 Princípio Core

> **Uma vez que um ProcessamentoVersao é FINALIZADO, ele NUNCA pode ser alterado ou deletado.**

Isso reflete diretamente o design do Core v0.8:
- `ProcessamentoVersao.Finalizar()` retorna **nova instância** (imutabilidade de objeto)
- Resultado só pode ser consultado, nunca modificado

### 6.2 Operações Permitidas vs Proibidas

| Operação | Permitida? | Justificativa |
|----------|------------|---------------|
| INSERT novo processamento | ✅ Sim | Criação de V1 ou reprocessamento (V2+) |
| UPDATE status para Superado | ✅ Sim | Única exceção: marcar que foi superado |
| UPDATE qualquer outro campo | ❌ Não | Violaria imutabilidade |
| DELETE processamento finalizado | ❌ Não | Perda de histórico, ilegal |
| DELETE processamento cancelado | ⚠️ Depende | Permitido se nunca foi finalizado |

### 6.3 Implementação de Imutabilidade (Conceitual)

**Opção 1: Trigger de Proteção**

Conceito de trigger que impede UPDATE/DELETE em registros finalizados:
- Se Status = 'Finalizado' ou 'Superado', bloquear UPDATE (exceto SuperadoEm)
- Se Status = 'Finalizado' ou 'Superado', bloquear DELETE

**Opção 2: Permissões Granulares**

- Role de aplicação não tem UPDATE/DELETE em tabelas de resultado
- Apenas INSERT permitido
- Administrador tem acesso em emergências (com log)

**Opção 3: Append-Only Tables**

- Tabelas de resultado configuradas para INSERT-only
- Temporal Tables do SQL Server para histórico automático

**Recomendação:** Combinar opções 1 e 2 para defesa em profundidade.

### 6.4 Garantia de Integridade: Hash

O Core v0.8 calcula um `HashResultado` que pode ser verificado:

```
HashResultado = SHA256(
  FuncionarioId + 
  Competencia + 
  Versao + 
  SalarioBruto + 
  ValorInss + 
  ValorIrrf + 
  ... + 
  SalarioLiquido
)
```

**Uso:** Em auditorias, verificar se hash armazenado bate com recálculo do hash.
Se não bater, o registro foi adulterado fora do sistema.

---

## 7. Estratégia de Auditoria

### 7.1 O Que Deve Ser Auditado

| Item | Por Que | Como |
|------|---------|------|
| Quem processou | Responsabilização | Campo `UsuarioId` em ProcessamentoVersaoDb |
| Quando processou | Timeline de eventos | Campos `IniciadoEm`, `FinalizadoEm` |
| Qual versão | Rastreabilidade | Campo `VersaoNumero` |
| Por que reprocessou | Entender mudanças | Campo `MotivoReprocessamento` |
| Resultado anterior | Comparação | Campo `VersaoAnteriorId` |
| Memória de cálculo | Reproduzir decisões | Tabelas de Detalhe (INSS, IRRF, etc.) |

### 7.2 Entidade: AuditLogDb (Opcional mas Recomendada)

**Propósito:** Registrar operações de persistência para auditoria geral.

**Campos conceituais:**

| Campo | Tipo | Descrição |
|-------|------|-----------|
| Id | GUID | Identificador único |
| Timestamp | DateTime | Quando ocorreu |
| Operacao | String | INSERT, UPDATE, DELETE |
| Entidade | String | Nome da entidade afetada |
| EntidadeId | String | ID do registro afetado |
| ValorAnterior | JSON? | Estado antes (para UPDATE) |
| ValorNovo | JSON? | Estado depois |
| UsuarioId | String | Quem executou |
| Origem | String | API, Job, Console |
| CorrelationId | GUID? | Para rastrear operações relacionadas |

### 7.3 Memória de Cálculo como Auditoria

As tabelas de detalhe (DetalheInssDb, DetalheIrrfDb, etc.) servem como
**memória de cálculo auditável**:

- Qual tabela INSS foi usada? `TabelaIdUsada`
- Quantos dependentes foram considerados no IRRF? `NumeroDependentes`
- Qual margem consignável foi aplicada? `PercentualMargem`, `MargemTotal`

**Benefício:** Anos depois, é possível explicar exatamente por que
o salário líquido foi X e não Y.

### 7.4 Quando Usar JSON vs Tabelas de Detalhe

> ⚠️ **REGRA DE OURO PARA JSON:**
>
> **JSON é usado APENAS para estruturas internas variáveis, NUNCA para valores finais de negócio.**

| Tipo de Dado | Armazenamento | Justificativa |
|--------------|---------------|---------------|
| ValorInss, ValorIrrf, SalarioLiquido | **Coluna tipada (Decimal)** | Valores finais de negócio, precisam ser queryable e indexáveis |
| ContribuicaoPorFaixaJson (array de faixas) | **JSON** | Estrutura interna variável (pode ter 1, 2, 3 ou 4 faixas) |
| DescontosJson (array de contratos) | **JSON** | Lista dinâmica de contratos (quantidade variável) |
| TabelaIdUsada, AliquotaAplicada | **Coluna tipada** | Valores fixos e queryáveis para auditoria |

**Consequências:**
- ❌ Nunca armazenar `SalarioLiquido` em JSON
- ❌ Nunca criar tabela filha para cada faixa INSS (explosão de registros)
- ✅ JSON para listas internas de tamanho variável
- ✅ Colunas tipadas para valores de negócio consultáveis

---

## 8. Estratégia de Armazenamento de Resultado (Snapshot)

### 8.1 Conceito de Snapshot

O `ResultadoCalculoDb` é um **snapshot completo** do resultado de cálculo:

- Não depende de dados externos para ser interpretado
- Contém todos os valores finais (bruto, descontos, líquido)
- Pode ser lido isoladamente sem joins complexos

### 8.2 Por Que Snapshot (e não Referências)

**Opção rejeitada: Armazenar apenas referências**
```
ResultadoCalculoDb
├── TabelaInssIdUsada  → FK para TabelaInssDb
├── TabelaIrrfIdUsada  → FK para TabelaIrrfDb
└── FuncionarioIdUsado → FK para FuncionarioDb
```

**Problema:** Se TabelaInssDb for alterada (por erro), todos os resultados
históricos seriam afetados. Viola imutabilidade.

**Opção escolhida: Snapshot de valores**
```
ResultadoCalculoDb
├── ValorInss = R$ 400,00  (valor congelado)
├── ValorIrrf = R$ 300,00  (valor congelado)
└── DetalheInssDb.TabelaIdUsada = "INSS-2025-V1"  (identificador, não FK)
```

**Benefício:** Mesmo que TabelaInssDb original seja corrigida, o resultado
histórico permanece intacto e verificável.

### 8.3 Desnormalização Intencional

Para garantir imutabilidade e performance, algumas informações são
propositalmente desnormalizadas:

| Informação | Onde está normalizada | Onde está desnormalizada | Por Que |
|------------|----------------------|--------------------------|---------|
| Nome da tabela INSS | TabelaInssDb | DetalheInssDb.TabelaIdUsada | Histórico |
| Competência | - | ProcessamentoVersaoDb (Ano, Mês) | Consulta rápida |
| Valores finais | Cálculo | ResultadoCalculoDb | Snapshot |

### 8.4 JSON para Detalhes Complexos

Alguns campos usam JSON para flexibilidade:

- `DetalheInssDb.ContribuicaoPorFaixaJson` - Array de faixas INSS
- `DetalheConsignadosDb.DescontosJson` - Array de contratos descontados

**Formato conceitual:**
```json
// ContribuicaoPorFaixaJson
[
  {"faixa": 1, "limite": 1518.00, "aliquota": 7.5, "contribuicao": 113.85},
  {"faixa": 2, "limite": 2793.88, "aliquota": 9.0, "contribuicao": 114.83}
]

// DescontosJson
[
  {"contratoId": "GUID-1", "parcela": 5, "totalParcelas": 24, "valor": 150.00},
  {"contratoId": "GUID-2", "parcela": 12, "totalParcelas": 36, "valor": 200.00}
]
```

---

## 9. Justificativa Arquitetural de Cada Decisão

### 9.1 Tabela de Decisões

| Decisão | Alternativas Consideradas | Escolha | Justificativa |
|---------|--------------------------|---------|---------------|
| GUID como PK | INT IDENTITY | GUID | Geração no Core, sem dependência de BD |
| Competência como Ano+Mês | DATE, VARCHAR | INT+INT | Consultas eficientes, sem ambiguidade |
| Status como String | TINYINT | String (enum) | Legibilidade, debugging |
| Resultado em tabela separada | Inline em Processamento | Separada | Normalização, clareza |
| Detalhes em tabelas separadas | JSON único | Tabelas | Queryable, indexável |
| Self-reference para versões | Tabela separada de links | Self-ref | Simplicidade, menos joins |
| Hash de integridade | Sem hash | Com hash | Detecção de adulteração |
| Snapshot de valores | Referências FK | Snapshot | Imutabilidade verdadeira |

### 9.2 Alinhamento com ARCHITECTURE_MAP.md

| Princípio do ARCHITECTURE_MAP | Como Este Modelo Atende |
|-------------------------------|------------------------|
| "Banco NÃO executa regras de negócio" | Nenhuma stored procedure de cálculo; apenas INSERT/SELECT |
| "Resultados são imutáveis" | Trigger/permissões bloqueiam UPDATE/DELETE após finalização |
| "Versionamento de processamento" | Campo VersaoNumero, self-reference VersaoAnteriorId |
| "Cada processamento é um artefato imutável" | ProcessamentoVersaoDb + ResultadoCalculoDb como snapshot |
| "Auditoria completa" | Campos de timestamp, usuário, motivo, memória de cálculo |
| "Reprocessamento gera nova versão" | INSERT de nova versão, nunca UPDATE da anterior |
| "Rastreabilidade de vigências" | TabelaIdUsada nos detalhes (qual tabela foi usada) |

---

## 10. O Que Explicitamente NÃO Deve Ser Feito no Bancos

### 10.1 Lista de Proibições

| ❌ Proibido | Por Que | Onde Deve Estar |
|------------|---------|-----------------|
| Stored Procedure que calcula INSS | Banco não tem regras de negócio | `CalculadoraInss` no Core |
| Trigger que valida margem consignável | Banco não valida regras | `CalculadoraConsignados` no Core |
| Constraint CHECK de alíquota máxima | Regra muda com legislação | Tabelas de vigência no Core |
| Função que calcula salário líquido | Lógica de cálculo é do Core | `CalculadoraFolha` no Core |
| Trigger que auto-atualiza versão | Versionamento é do Core | `HistoricoProcessamento` no Core |
| View materializada de totais | Cálculo é do Core | Serviço de relatórios |
| Scheduler de reprocessamento | Orquestração é da aplicação | Job (Hangfire/Quartz) |
| Validação de CPF/CNPJ | Regra de domínio | Value Objects no Core |
| Cálculo de diferença entre versões | Lógica de comparação | `DiferencaVersoes` no Core |

### 10.2 O Que o Banco PODE Fazer

| ✅ Permitido | Justificativa |
|-------------|---------------|
| Integridade referencial (FK) | Garantir consistência de relacionamentos |
| Índices para performance | Otimização de consultas |
| Constraints de NOT NULL | Garantir dados completos |
| Unique constraints | Evitar duplicatas acidentais |
| Triggers de proteção de imutabilidade | Impedir violação de regra já decidida pelo Core |
| Particionamento por competência | Performance em grandes volumes |
| Compressão de dados | Economia de espaço |

### 10.3 Regra de Ouro

> **Se a regra pode mudar com legislação ou decisão de negócio,
> ela NÃO pode estar no banco de dados.**

Exemplos:
- Alíquota INSS muda? → Core (TabelaInss com vigência)
- Percentual margem consignável muda? → Core (parametrizado)
- Formato de cálculo IRRF muda? → Core (CalculadoraIrrf)

---

## 11. Considerações para Implementação Futura

### 11.1 Performance

- **Particionamento:** Considerar particionar por CompetenciaAno para queries históricas
- **Índices:** Índices em (FuncionarioId, CompetenciaAno, CompetenciaMes)
- **Compressão:** Colunas JSON podem ser comprimidas

### 11.2 Escalabilidade

- **Read Replicas:** Para relatórios, considerar réplicas de leitura
- **Arquivamento:** Competências antigas podem ir para storage frio

### 11.3 Segurança

- **Criptografia:** Dados sensíveis (CPF, salário) devem usar TDE ou column encryption
- **Mascaramento:** Dynamic Data Masking para ambientes não-produção

### 11.4 Migração

- **Entity Framework Migrations:** Usar para evolução de schema
- **Versionamento de Schema:** Manter histórico de alterações

---

## 12. Conclusão

Este modelo de dados foi projetado para:

1. **Persistir fielmente** o que o Core v0.8 calcula
2. **Garantir imutabilidade** de resultados finalizados
3. **Manter versionamento completo** para auditoria
4. **Não violar** nenhum princípio do ARCHITECTURE_MAP.md
5. **Preparar** para implementação futura em SQL Server

O banco de dados é um **repositório passivo** que armazena verdades
calculadas pelo Core, sem jamais aplicar regras de negócio próprias.

---

**Documento preparado por:** Sistema de Folha de Pagamento - Arquitetura  
**Próximo passo:** Implementação de DDL e Entity Framework Mappings (fase futura)
