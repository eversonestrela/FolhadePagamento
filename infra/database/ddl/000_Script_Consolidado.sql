-- ============================================================================
-- SCRIPT CONSOLIDADO DE CRIAÇÃO DO BANCO DE DADOS
-- Sistema de Folha de Pagamento - Core v0.8
-- ============================================================================
--
-- PROPÓSITO:
--   Este script consolida todos os DDLs para criação do banco de dados
--   em uma única execução, respeitando a ordem de dependências.
--
-- DATA: Dezembro 2025
-- BANCO ALVO: SQL Server
--
-- ============================================================================
-- ⚠️ AVISOS IMPORTANTES
-- ============================================================================
--
-- 1. Este script é um ARTEFATO VERSIONÁVEL do projeto
-- 2. NÃO execute sem aprovação do DBA
-- 3. NÃO execute em produção sem backup
-- 4. Teste primeiro em ambiente de desenvolvimento
--
-- ============================================================================
-- ORDEM DE EXECUÇÃO (baseada em dependências de FK)
-- ============================================================================
--
-- NÍVEL 0 - Tabelas sem dependências:
--   001_Funcionario.sql      → Cadastro de funcionários
--   008_AuditLog.sql         → Log de auditoria (independente)
--
-- NÍVEL 1 - Depende do Nível 0:
--   002_ProcessamentoVersao.sql → Depende de Funcionario
--
-- NÍVEL 2 - Depende do Nível 1:
--   003_ResultadoCalculo.sql → Depende de ProcessamentoVersao
--
-- NÍVEL 3 - Depende do Nível 2:
--   004_DetalheInss.sql       → Depende de ResultadoCalculo
--   005_DetalheIrrf.sql       → Depende de ResultadoCalculo
--   006_DetalheFgts.sql       → Depende de ResultadoCalculo
--   007_DetalheConsignados.sql → Depende de ResultadoCalculo
--
-- ============================================================================
-- DIAGRAMA DE DEPENDÊNCIAS
-- ============================================================================
--
--   Funcionario ─────────────────┐
--                                │
--   AuditLog (independente)      │
--                                ▼
--                    ProcessamentoVersao
--                           │
--                           ▼
--                    ResultadoCalculo
--                           │
--         ┌─────────┬───────┼───────┬─────────┐
--         ▼         ▼       ▼       ▼         ▼
--   DetalheInss DetalheIrrf DetalheFgts DetalheConsignados
--
-- ============================================================================


-- ============================================================================
-- NÍVEL 0: TABELAS SEM DEPENDÊNCIAS
-- ============================================================================

PRINT '=== NÍVEL 0: Criando tabelas sem dependências ===';
GO

-- ----------------------------------------------------------------------------
-- 001_Funcionario.sql - Cadastro de funcionários (mutável)
-- ----------------------------------------------------------------------------
PRINT 'Criando tabela: dbo.Funcionario';
GO

:r .\001_Funcionario.sql
GO

-- ----------------------------------------------------------------------------
-- 008_AuditLog.sql - Log de auditoria (independente)
-- ----------------------------------------------------------------------------
PRINT 'Criando tabela: dbo.AuditLog';
GO

:r .\008_AuditLog.sql
GO


-- ============================================================================
-- NÍVEL 1: DEPENDE DO NÍVEL 0
-- ============================================================================

PRINT '=== NÍVEL 1: Criando tabelas que dependem do Nível 0 ===';
GO

-- ----------------------------------------------------------------------------
-- 002_ProcessamentoVersao.sql - Processamentos versionados (imutável)
-- Dependências: Funcionario
-- ----------------------------------------------------------------------------
PRINT 'Criando tabela: dbo.ProcessamentoVersao';
GO

:r .\002_ProcessamentoVersao.sql
GO


-- ============================================================================
-- NÍVEL 2: DEPENDE DO NÍVEL 1
-- ============================================================================

PRINT '=== NÍVEL 2: Criando tabelas que dependem do Nível 1 ===';
GO

-- ----------------------------------------------------------------------------
-- 003_ResultadoCalculo.sql - Snapshot do resultado (imutável)
-- Dependências: ProcessamentoVersao
-- ----------------------------------------------------------------------------
PRINT 'Criando tabela: dbo.ResultadoCalculo';
GO

:r .\003_ResultadoCalculo.sql
GO


-- ============================================================================
-- NÍVEL 3: DEPENDE DO NÍVEL 2
-- ============================================================================

PRINT '=== NÍVEL 3: Criando tabelas de detalhamento ===';
GO

-- ----------------------------------------------------------------------------
-- 004_DetalheInss.sql - Memória de cálculo INSS
-- Dependências: ResultadoCalculo
-- ----------------------------------------------------------------------------
PRINT 'Criando tabela: dbo.DetalheInss';
GO

:r .\004_DetalheInss.sql
GO

-- ----------------------------------------------------------------------------
-- 005_DetalheIrrf.sql - Memória de cálculo IRRF
-- Dependências: ResultadoCalculo
-- ----------------------------------------------------------------------------
PRINT 'Criando tabela: dbo.DetalheIrrf';
GO

:r .\005_DetalheIrrf.sql
GO

-- ----------------------------------------------------------------------------
-- 006_DetalheFgts.sql - Memória de cálculo FGTS
-- Dependências: ResultadoCalculo
-- ----------------------------------------------------------------------------
PRINT 'Criando tabela: dbo.DetalheFgts';
GO

:r .\006_DetalheFgts.sql
GO

-- ----------------------------------------------------------------------------
-- 007_DetalheConsignados.sql - Memória de cálculo Consignados
-- Dependências: ResultadoCalculo
-- ----------------------------------------------------------------------------
PRINT 'Criando tabela: dbo.DetalheConsignados';
GO

:r .\007_DetalheConsignados.sql
GO


-- ============================================================================
-- VERIFICAÇÃO FINAL
-- ============================================================================

PRINT '=== Verificando tabelas criadas ===';
GO

SELECT 
    t.name AS Tabela,
    s.name AS [Schema],
    t.create_date AS CriadaEm
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'dbo'
  AND t.name IN (
      'Funcionario',
      'ProcessamentoVersao',
      'ResultadoCalculo',
      'DetalheInss',
      'DetalheIrrf',
      'DetalheFgts',
      'DetalheConsignados',
      'AuditLog'
  )
ORDER BY t.create_date;
GO

PRINT '=== Script consolidado executado com sucesso ===';
GO


-- ============================================================================
-- NOTAS DE EXECUÇÃO
-- ============================================================================
--
-- COMO EXECUTAR (via SQLCMD):
--   sqlcmd -S <servidor> -d <banco> -i 000_Script_Consolidado.sql
--
-- OU via SQL Server Management Studio:
--   1. Abrir este arquivo
--   2. Habilitar modo SQLCMD (Query → SQLCMD Mode)
--   3. Executar (F5)
--
-- PREREQUISITOS:
--   - Banco de dados já deve existir
--   - Usuário deve ter permissão CREATE TABLE
--   - Arquivos DDL devem estar na mesma pasta
--
-- ============================================================================
