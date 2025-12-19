-- ============================================================================
-- ARQUIVO: 002_ProcessamentoVersao.sql
-- PROPÓSITO: Tabela principal de processamentos versionados
-- ENTIDADE DE DOMÍNIO: ProcessamentoVersao
-- ENTIDADE DE PERSISTÊNCIA: ProcessamentoVersaoDb
-- ============================================================================
-- 
-- CARACTERÍSTICAS:
--   - Entidade IMUTÁVEL após finalização
--   - Cada versão é um registro independente (V1, V2, V3...)
--   - Nunca deletada (histórico completo preservado)
--   - Self-reference para cadeia de versões (VersaoAnteriorId)
--
-- VERSIONAMENTO:
--   V3 ──(VersaoAnteriorId)──► V2 ──(VersaoAnteriorId)──► V1 ──► NULL
--
-- STATUS POSSÍVEIS:
--   - EmProcessamento: Cálculo em andamento
--   - Finalizado: Versão válida e atual
--   - Cancelado: Descartado antes de finalizar
--   - Superado: Histórico (nova versão assumiu)
--
-- IMUTABILIDADE:
--   - Após Status = 'Finalizado', apenas SuperadoEm pode ser atualizado
--   - UPDATE/DELETE bloqueados por trigger (implementação futura)
--
-- ============================================================================

CREATE TABLE dbo.ProcessamentoVersao
(
    -- ========================================================================
    -- IDENTIFICAÇÃO
    -- ========================================================================
    
    -- Identificador único do processamento (GUID gerado no Core)
    ProcessamentoVersaoId   UNIQUEIDENTIFIER    NOT NULL,
    
    -- ========================================================================
    -- CONTEXTO
    -- ========================================================================
    
    -- Funcionário processado
    FuncionarioId           UNIQUEIDENTIFIER    NOT NULL,
    
    -- Competência: Ano (ex: 2025)
    CompetenciaAno          INT                 NOT NULL,
    
    -- Competência: Mês (1-12)
    CompetenciaMes          INT                 NOT NULL,
    
    -- ========================================================================
    -- VERSIONAMENTO
    -- ========================================================================
    
    -- Número da versão (1, 2, 3...)
    VersaoNumero            INT                 NOT NULL,
    
    -- Referência à versão anterior (NULL para V1)
    VersaoAnteriorId        UNIQUEIDENTIFIER    NULL,
    
    -- ========================================================================
    -- STATUS E CICLO DE VIDA
    -- ========================================================================
    
    -- Status atual: EmProcessamento, Finalizado, Cancelado, Superado
    Status                  NVARCHAR(20)        NOT NULL,
    
    -- Timestamp de início do processamento
    IniciadoEm              DATETIME2(7)        NOT NULL,
    
    -- Timestamp de finalização (NULL se não finalizado)
    FinalizadoEm            DATETIME2(7)        NULL,
    
    -- Timestamp de quando foi superado (NULL se versão atual)
    SuperadoEm              DATETIME2(7)        NULL,
    
    -- ========================================================================
    -- REPROCESSAMENTO
    -- ========================================================================
    
    -- Código do motivo de reprocessamento (NULL para V1)
    -- Ex: CorrecaoCalculo, AtualizacaoLegislacao, CorrecaoCadastro
    MotivoReprocessamento   NVARCHAR(50)        NULL,
    
    -- Descrição detalhada do motivo (NULL para V1)
    DescricaoReprocessamento NVARCHAR(500)      NULL,
    
    -- ========================================================================
    -- AUDITORIA
    -- ========================================================================
    
    -- Usuário que executou o processamento
    UsuarioId               NVARCHAR(100)       NULL,
    
    -- Hash SHA256 do resultado para verificação de integridade
    HashResultado           NVARCHAR(64)        NULL,
    
    -- Timestamp de criação do registro no banco
    CriadoEm                DATETIME2(7)        NOT NULL    DEFAULT SYSUTCDATETIME(),
    
    -- ========================================================================
    -- CONSTRAINTS
    -- ========================================================================
    
    -- Primary Key
    CONSTRAINT PK_ProcessamentoVersao 
        PRIMARY KEY (ProcessamentoVersaoId),
    
    -- Foreign Key: Funcionário
    CONSTRAINT FK_ProcessamentoVersao_Funcionario
        FOREIGN KEY (FuncionarioId)
        REFERENCES dbo.Funcionario (FuncionarioId),
    
    -- Foreign Key: Versão Anterior (self-reference)
    CONSTRAINT FK_ProcessamentoVersao_VersaoAnterior
        FOREIGN KEY (VersaoAnteriorId)
        REFERENCES dbo.ProcessamentoVersao (ProcessamentoVersaoId),
    
    -- Unique: Não pode haver duas versões com mesmo número para mesmo funcionário/competência
    CONSTRAINT UQ_ProcessamentoVersao_FuncionarioCompetenciaVersao
        UNIQUE (FuncionarioId, CompetenciaAno, CompetenciaMes, VersaoNumero),
    
    -- Check: Mês válido (1-12)
    CONSTRAINT CK_ProcessamentoVersao_CompetenciaMes
        CHECK (CompetenciaMes >= 1 AND CompetenciaMes <= 12),
    
    -- Check: Versão deve ser positiva
    CONSTRAINT CK_ProcessamentoVersao_VersaoNumero
        CHECK (VersaoNumero >= 1),
    
    -- Check: Status válido
    CONSTRAINT CK_ProcessamentoVersao_Status
        CHECK (Status IN ('EmProcessamento', 'Finalizado', 'Cancelado', 'Superado')),
    
    -- Check: V1 não pode ter versão anterior
    CONSTRAINT CK_ProcessamentoVersao_V1SemAnterior
        CHECK (VersaoNumero > 1 OR VersaoAnteriorId IS NULL),
    
    -- Check: V2+ deve ter versão anterior
    CONSTRAINT CK_ProcessamentoVersao_V2ComAnterior
        CHECK (VersaoNumero = 1 OR VersaoAnteriorId IS NOT NULL)
);
GO

-- ============================================================================
-- ÍNDICES
-- ============================================================================

-- Índice principal: Buscar processamentos por funcionário e competência
CREATE NONCLUSTERED INDEX IX_ProcessamentoVersao_FuncionarioCompetencia
    ON dbo.ProcessamentoVersao (FuncionarioId, CompetenciaAno, CompetenciaMes)
    INCLUDE (VersaoNumero, Status);
GO

-- Índice: Buscar versão atual (última Finalizada)
CREATE NONCLUSTERED INDEX IX_ProcessamentoVersao_Status
    ON dbo.ProcessamentoVersao (Status)
    INCLUDE (FuncionarioId, CompetenciaAno, CompetenciaMes, VersaoNumero);
GO

-- Índice: Navegar cadeia de versões
CREATE NONCLUSTERED INDEX IX_ProcessamentoVersao_VersaoAnterior
    ON dbo.ProcessamentoVersao (VersaoAnteriorId)
    WHERE VersaoAnteriorId IS NOT NULL;
GO

-- Índice: Buscar por competência (relatórios mensais)
CREATE NONCLUSTERED INDEX IX_ProcessamentoVersao_Competencia
    ON dbo.ProcessamentoVersao (CompetenciaAno, CompetenciaMes);
GO

-- ============================================================================
-- COMENTÁRIOS
-- ============================================================================

-- Esta tabela armazena cada versão de processamento como registro imutável.
-- O conceito de "Processamento" (agregado lógico) não existe como tabela.
-- O "status atual" é DERIVADO: última versão com Status = 'Finalizado'.
-- 
-- REGRAS DE IMUTABILIDADE (implementar via trigger futuramente):
--   - Após Status = 'Finalizado' ou 'Superado': bloquear UPDATE (exceto SuperadoEm)
--   - Após Status = 'Finalizado' ou 'Superado': bloquear DELETE
