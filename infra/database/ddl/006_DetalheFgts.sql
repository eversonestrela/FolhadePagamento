-- ============================================================================
-- ARQUIVO: 006_DetalheFgts.sql
-- PROPÓSITO: Tabela de memória de cálculo do FGTS
-- ENTIDADE DE DOMÍNIO: ResultadoCalculoFgts
-- ENTIDADE DE PERSISTÊNCIA: DetalheFgtsDb
-- ============================================================================
-- 
-- CARACTERÍSTICAS:
--   - Entidade IMUTÁVEL (memória de cálculo auditável)
--   - Relacionamento 1:0..1 com ResultadoCalculo
--   - Armazena COMO o FGTS foi calculado
--
-- IMPORTANTE:
--   - FGTS é ENCARGO PATRONAL (pago pelo empregador)
--   - NÃO desconta do salário do funcionário
--   - Alíquota padrão: 8%
--   - Aprendiz: 2%
--   - Doméstico: 8% + 3,2% antecipação rescisória
--
-- ============================================================================

CREATE TABLE dbo.DetalheFgts
(
    -- ========================================================================
    -- IDENTIFICAÇÃO
    -- ========================================================================
    
    -- Identificador único do detalhe
    DetalheFgtsId           UNIQUEIDENTIFIER    NOT NULL,
    
    -- Resultado ao qual este detalhe pertence (1:0..1)
    ResultadoCalculoId      UNIQUEIDENTIFIER    NOT NULL,
    
    -- ========================================================================
    -- MEMÓRIA DE CÁLCULO
    -- ========================================================================
    
    -- Base de cálculo do FGTS (normalmente = SalarioBruto)
    BaseCalculo             DECIMAL(18,2)       NOT NULL,
    
    -- Identificador da tabela/regra FGTS usada (ex: "FGTS-2025-V1")
    TabelaIdUsada           NVARCHAR(50)        NOT NULL,
    
    -- Alíquota aplicada (percentual)
    -- Normalmente 8%, mas pode ser 2% (aprendiz) ou 11,2% (doméstico)
    AliquotaAplicada        DECIMAL(5,2)        NOT NULL,
    
    -- Tipo de contribuinte que determina a alíquota
    -- Valores: Normal, Aprendiz, Domestico
    TipoContribuinte        NVARCHAR(20)        NOT NULL,
    
    -- ========================================================================
    -- CONSTRAINTS
    -- ========================================================================
    
    -- Primary Key
    CONSTRAINT PK_DetalheFgts 
        PRIMARY KEY (DetalheFgtsId),
    
    -- Foreign Key: Resultado (1:0..1)
    CONSTRAINT FK_DetalheFgts_ResultadoCalculo
        FOREIGN KEY (ResultadoCalculoId)
        REFERENCES dbo.ResultadoCalculo (ResultadoCalculoId),
    
    -- Unique: Apenas um detalhe FGTS por resultado
    CONSTRAINT UQ_DetalheFgts_ResultadoCalculo
        UNIQUE (ResultadoCalculoId),
    
    -- Check: Base não pode ser negativa
    CONSTRAINT CK_DetalheFgts_BaseCalculo_NaoNegativa
        CHECK (BaseCalculo >= 0),
    
    -- Check: Alíquota válida (0% a 100%)
    CONSTRAINT CK_DetalheFgts_AliquotaAplicada
        CHECK (AliquotaAplicada >= 0 AND AliquotaAplicada <= 100),
    
    -- Check: Tipo de contribuinte válido
    CONSTRAINT CK_DetalheFgts_TipoContribuinte
        CHECK (TipoContribuinte IN ('Normal', 'Aprendiz', 'Domestico'))
);
GO

-- ============================================================================
-- ÍNDICES
-- ============================================================================

-- Índice: Buscar detalhe por resultado
CREATE NONCLUSTERED INDEX IX_DetalheFgts_ResultadoCalculo
    ON dbo.DetalheFgts (ResultadoCalculoId);
GO

-- Índice: Buscar por tabela usada (auditoria)
CREATE NONCLUSTERED INDEX IX_DetalheFgts_TabelaIdUsada
    ON dbo.DetalheFgts (TabelaIdUsada);
GO

-- Índice: Buscar por tipo de contribuinte (relatórios)
CREATE NONCLUSTERED INDEX IX_DetalheFgts_TipoContribuinte
    ON dbo.DetalheFgts (TipoContribuinte);
GO

-- ============================================================================
-- COMENTÁRIOS
-- ============================================================================

-- Esta tabela armazena a memória de cálculo do FGTS para auditoria.
-- 
-- FGTS é encargo patronal:
--   - Valor aparece em ResultadoCalculo.ValorFgts
--   - NÃO é descontado de TotalDescontos
--   - É somado em TotalEncargosPatronais
--   - Impacta CustoTotalEmpregador
--
-- TabelaIdUsada é um identificador (string), NÃO uma FK:
--   - Evita acoplamento com tabela de vigências
--   - Snapshot permanece válido mesmo se tabela original for corrigida
