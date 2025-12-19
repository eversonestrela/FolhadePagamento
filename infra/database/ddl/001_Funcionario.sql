-- ============================================================================
-- ARQUIVO: 001_Funcionario.sql
-- PROPÓSITO: Tabela de cadastro de funcionários
-- ENTIDADE DE DOMÍNIO: Funcionario
-- ENTIDADE DE PERSISTÊNCIA: FuncionarioDb
-- ============================================================================
-- 
-- CARACTERÍSTICAS:
--   - Entidade MUTÁVEL (cadastro pode ser alterado)
--   - Serve como contexto e vínculo histórico
--   - O Core de Cálculo NÃO depende desta tabela
--   - Cada processamento armazena snapshot do salário em ResultadoCalculo
--
-- IMPORTANTE:
--   ❌ Nunca recalcular histórico porque SalarioBase mudou
--   ❌ Nunca acoplar lógica de cálculo a "salário atual" do cadastro
--   ✅ Cada ProcessamentoVersao armazena o snapshot do salário usado
--
-- ============================================================================

CREATE TABLE dbo.Funcionario
(
    -- ========================================================================
    -- IDENTIFICAÇÃO
    -- ========================================================================
    
    -- Identificador único do funcionário (GUID gerado no Core)
    FuncionarioId           UNIQUEIDENTIFIER    NOT NULL,
    
    -- ========================================================================
    -- DADOS CADASTRAIS
    -- ========================================================================
    
    -- Nome completo do funcionário
    Nome                    NVARCHAR(200)       NOT NULL,
    
    -- Salário base contratual (atual)
    -- NOTA: Este valor pode mudar. Resultados de cálculo usam snapshot.
    SalarioBase             DECIMAL(18,2)       NOT NULL,
    
    -- Data de admissão
    DataAdmissao            DATE                NULL,
    
    -- Indica se o funcionário está ativo
    Ativo                   BIT                 NOT NULL    DEFAULT 1,
    
    -- ========================================================================
    -- AUDITORIA
    -- ========================================================================
    
    -- Timestamp de criação do registro
    CriadoEm                DATETIME2(7)        NOT NULL    DEFAULT SYSUTCDATETIME(),
    
    -- Timestamp da última atualização
    AtualizadoEm            DATETIME2(7)        NULL,
    
    -- ========================================================================
    -- CONSTRAINTS
    -- ========================================================================
    
    -- Primary Key
    CONSTRAINT PK_Funcionario 
        PRIMARY KEY (FuncionarioId),
    
    -- Salário não pode ser negativo
    CONSTRAINT CK_Funcionario_SalarioBase_NaoNegativo 
        CHECK (SalarioBase >= 0)
);
GO

-- ============================================================================
-- ÍNDICES
-- ============================================================================

-- Índice para busca por nome (consultas e relatórios)
CREATE NONCLUSTERED INDEX IX_Funcionario_Nome
    ON dbo.Funcionario (Nome);
GO

-- Índice para filtrar funcionários ativos
CREATE NONCLUSTERED INDEX IX_Funcionario_Ativo
    ON dbo.Funcionario (Ativo)
    WHERE Ativo = 1;
GO

-- ============================================================================
-- COMENTÁRIOS
-- ============================================================================

-- Esta tabela armazena dados cadastrais de funcionários.
-- É uma entidade MUTÁVEL, diferente das tabelas de resultado que são imutáveis.
-- O Core de Cálculo recebe Funcionario como parâmetro e não depende desta tabela.
-- A FK em ProcessamentoVersao serve apenas para rastreabilidade histórica.
