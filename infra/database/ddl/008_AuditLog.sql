-- ============================================================================
-- ARQUIVO: 008_AuditLog.sql
-- PROPÓSITO: Tabela de log de auditoria geral
-- ENTIDADE DE PERSISTÊNCIA: AuditLogDb
-- ============================================================================
-- 
-- CARACTERÍSTICAS:
--   - Entidade IMUTÁVEL (log append-only)
--   - Registra operações de persistência (INSERT, UPDATE, DELETE)
--   - Opcional mas fortemente recomendada
--   - Independente das outras tabelas
--
-- CONTEÚDO:
--   - Quem executou a operação
--   - Quando executou
--   - Qual entidade foi afetada
--   - Estado antes e depois (para UPDATE)
--   - Origem da operação (API, Job, Console)
--
-- IMPORTANTE:
--   Esta tabela é INSERT-only. Nunca deve sofrer UPDATE ou DELETE.
--
-- ============================================================================

CREATE TABLE dbo.AuditLog
(
    -- ========================================================================
    -- IDENTIFICAÇÃO
    -- ========================================================================
    
    -- Identificador único do log
    AuditLogId              UNIQUEIDENTIFIER    NOT NULL    DEFAULT NEWSEQUENTIALID(),
    
    -- ========================================================================
    -- QUANDO
    -- ========================================================================
    
    -- Timestamp da operação
    Timestamp               DATETIME2(7)        NOT NULL    DEFAULT SYSUTCDATETIME(),
    
    -- ========================================================================
    -- O QUE
    -- ========================================================================
    
    -- Tipo de operação: INSERT, UPDATE, DELETE
    Operacao                NVARCHAR(10)        NOT NULL,
    
    -- Nome da entidade afetada (ex: "ProcessamentoVersao", "Funcionario")
    Entidade                NVARCHAR(100)       NOT NULL,
    
    -- ID do registro afetado (string para suportar GUIDs)
    EntidadeId              NVARCHAR(100)       NOT NULL,
    
    -- ========================================================================
    -- ANTES E DEPOIS
    -- ========================================================================
    
    -- Estado antes da operação (JSON, para UPDATE)
    ValorAnterior           NVARCHAR(MAX)       NULL,
    
    -- Estado depois da operação (JSON, para INSERT e UPDATE)
    ValorNovo               NVARCHAR(MAX)       NULL,
    
    -- ========================================================================
    -- QUEM
    -- ========================================================================
    
    -- Identificador do usuário que executou
    UsuarioId               NVARCHAR(100)       NULL,
    
    -- ========================================================================
    -- CONTEXTO
    -- ========================================================================
    
    -- Origem da operação: API, Job, Console, Migration
    Origem                  NVARCHAR(50)        NULL,
    
    -- ID de correlação para rastrear operações relacionadas
    CorrelationId           UNIQUEIDENTIFIER    NULL,
    
    -- Informações adicionais (ex: IP, User-Agent)
    Detalhes                NVARCHAR(MAX)       NULL,
    
    -- ========================================================================
    -- CONSTRAINTS
    -- ========================================================================
    
    -- Primary Key
    CONSTRAINT PK_AuditLog 
        PRIMARY KEY (AuditLogId),
    
    -- Check: Operação válida
    CONSTRAINT CK_AuditLog_Operacao
        CHECK (Operacao IN ('INSERT', 'UPDATE', 'DELETE')),
    
    -- Check: JSON válido (se preenchido)
    CONSTRAINT CK_AuditLog_ValorAnterior
        CHECK (ValorAnterior IS NULL OR ISJSON(ValorAnterior) = 1),
    
    CONSTRAINT CK_AuditLog_ValorNovo
        CHECK (ValorNovo IS NULL OR ISJSON(ValorNovo) = 1)
);
GO

-- ============================================================================
-- ÍNDICES
-- ============================================================================

-- Índice: Buscar por timestamp (consultas cronológicas)
CREATE NONCLUSTERED INDEX IX_AuditLog_Timestamp
    ON dbo.AuditLog (Timestamp DESC);
GO

-- Índice: Buscar por entidade e ID (histórico de um registro)
CREATE NONCLUSTERED INDEX IX_AuditLog_EntidadeId
    ON dbo.AuditLog (Entidade, EntidadeId)
    INCLUDE (Timestamp, Operacao);
GO

-- Índice: Buscar por usuário (auditoria de ações)
CREATE NONCLUSTERED INDEX IX_AuditLog_Usuario
    ON dbo.AuditLog (UsuarioId)
    WHERE UsuarioId IS NOT NULL;
GO

-- Índice: Buscar por correlation ID (rastrear fluxo)
CREATE NONCLUSTERED INDEX IX_AuditLog_CorrelationId
    ON dbo.AuditLog (CorrelationId)
    WHERE CorrelationId IS NOT NULL;
GO

-- Índice: Buscar por operação e entidade (análise)
CREATE NONCLUSTERED INDEX IX_AuditLog_OperacaoEntidade
    ON dbo.AuditLog (Operacao, Entidade)
    INCLUDE (Timestamp);
GO

-- ============================================================================
-- COMENTÁRIOS
-- ============================================================================

-- Esta tabela registra todas as operações de persistência para auditoria.
-- É APPEND-ONLY: nunca sofre UPDATE ou DELETE.
--
-- Uso típico:
--   - Quem alterou o cadastro do funcionário?
--   - Quando o processamento foi criado?
--   - Qual era o valor antes da atualização?
--
-- Correlação:
--   - CorrelationId permite rastrear operações do mesmo fluxo
--   - Ex: Um reprocessamento pode gerar vários logs com mesmo CorrelationId
--
-- Retenção:
--   - Considerar política de arquivamento para logs antigos
--   - Particionar por mês/ano para melhor performance
