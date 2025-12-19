# DDL Scripts - Sistema de Folha de Pagamento

**Versão:** 1.0  
**Data:** Dezembro 2025  
**Banco Alvo:** SQL Server  

---

## ⚠️ IMPORTANTE

**Estes arquivos são artefatos versionáveis do projeto.**

- ❌ NÃO execute diretamente no banco
- ❌ NÃO use para criar banco de produção
- ✅ Use como referência para migrations
- ✅ Use para revisão de código e documentação

---

## Arquivos

| Arquivo | Entidade | Propósito |
|---------|----------|-----------|
| `001_Funcionario.sql` | FuncionarioDb | Cadastro de funcionários (mutável) |
| `002_ProcessamentoVersao.sql` | ProcessamentoVersaoDb | Processamentos versionados (imutável) |
| `003_ResultadoCalculo.sql` | ResultadoCalculoDb | Snapshot do resultado (imutável) |
| `004_DetalheInss.sql` | DetalheInssDb | Memória de cálculo INSS |
| `005_DetalheIrrf.sql` | DetalheIrrfDb | Memória de cálculo IRRF |
| `006_DetalheFgts.sql` | DetalheFgtsDb | Memória de cálculo FGTS |
| `007_DetalheConsignados.sql` | DetalheConsignadosDb | Memória de cálculo Consignados |
| `008_AuditLog.sql` | AuditLogDb | Log de auditoria geral |

---

## Ordem de Execução

Os arquivos estão numerados para indicar a ordem correta de criação
(respeitando dependências de Foreign Keys):

```
001 → 002 → 003 → 004/005/006/007 → 008
```

---

## Padrões Utilizados

### Nomenclatura

- **PascalCase** para todos os identificadores
- **Tabelas** no singular (ex: `Funcionario`, não `Funcionarios`)
- **Primary Keys:** `<Entidade>Id`
- **Foreign Keys:** `<EntidadeReferenciada>Id`
- **Schema:** `dbo`

### Constraints

- `PK_<Tabela>` - Primary Key
- `FK_<Tabela>_<TabelaReferenciada>` - Foreign Key
- `UQ_<Tabela>_<Colunas>` - Unique
- `CK_<Tabela>_<Descricao>` - Check
- `IX_<Tabela>_<Colunas>` - Index

### Tipos de Dados

| Conceito | Tipo SQL Server |
|----------|-----------------|
| GUID | UNIQUEIDENTIFIER |
| Dinheiro | DECIMAL(18,2) |
| Percentual | DECIMAL(5,2) |
| Texto curto | NVARCHAR(N) |
| Texto longo/JSON | NVARCHAR(MAX) |
| Data/Hora | DATETIME2(7) |
| Booleano | BIT |

---

## Alinhamento com INFRASTRUCTURE_DATA_MODEL.md

Estes scripts foram gerados a partir do documento conceitual
`INFRASTRUCTURE_DATA_MODEL.md` e refletem fielmente:

1. ✅ Entidades de persistência descritas
2. ✅ Relacionamentos conceituais
3. ✅ Estratégia de versionamento
4. ✅ Estratégia de imutabilidade
5. ✅ Estratégia de auditoria

---

## O Que NÃO Está Nestes Scripts

Conforme INFRASTRUCTURE_DATA_MODEL.md, o banco NÃO contém:

- ❌ Stored Procedures de cálculo
- ❌ Triggers de validação de regras de negócio
- ❌ Views materializadas de totais
- ❌ Funções de cálculo

O Core é a única fonte de verdade para regras de negócio.

---

## Próximos Passos

1. Revisar scripts com equipe de DBA
2. Criar migrations via Entity Framework
3. Implementar triggers de proteção de imutabilidade
4. Configurar política de backup e retenção
