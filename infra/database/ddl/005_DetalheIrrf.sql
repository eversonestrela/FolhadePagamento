-- ============================================================================
-- ARQUIVO: 005_DetalheIrrf.sql
-- PROPÓSITO: Tabela de memória de cálculo do IRRF
-- ENTIDADE DE DOMÍNIO: ResultadoCalculoIrrf
-- ENTIDADE DE PERSISTÊNCIA: DetalheIrrfDb
-- ============================================================================
-- 
-- CARACTERÍSTICAS:
--   - Entidade IMUTÁVEL (memória de cálculo auditável)
--   - Relacionamento 1:0..1 com ResultadoCalculo
--   - Armazena COMO o IRRF foi calculado, não apenas o valor final
--
-- MEMÓRIA DE CÁLCULO:
--   - Qual tabela IRRF foi usada
--   - Base de cálculo (após deduções)
--   - Número de dependentes considerados
--   - Faixa e alíquota aplicadas
--   - Se ficou isento
--
-- ============================================================================

CREATE TABLE dbo.DetalheIrrf
(
    -- ========================================================================
    -- IDENTIFICAÇÃO
    -- ========================================================================
    
    -- Identificador único do detalhe
    DetalheIrrfId           UNIQUEIDENTIFIER    NOT NULL,
    
    -- Resultado ao qual este detalhe pertence (1:0..1)
    ResultadoCalculoId      UNIQUEIDENTIFIER    NOT NULL,
    
    -- ========================================================================
    -- MEMÓRIA DE CÁLCULO - BASE
    -- ========================================================================
    
    -- Base de cálculo após deduções (Bruto - INSS - Dependentes)
    BaseCalculo             DECIMAL(18,2)       NOT NULL,
    
    -- Valor do INSS deduzido da base
    DeducaoInss             DECIMAL(18,2)       NOT NULL,
    
    -- ========================================================================
    -- MEMÓRIA DE CÁLCULO - DEPENDENTES
    -- ========================================================================
    
    -- Número de dependentes considerados
    NumeroDependentes       INT                 NOT NULL,
    
    -- Valor de dedução por dependente (da tabela vigente)
    DeducaoPorDependente    DECIMAL(18,2)       NOT NULL,
    
    -- ========================================================================
    -- MEMÓRIA DE CÁLCULO - TABELA E FAIXA
    -- ========================================================================
    
    -- Identificador da tabela IRRF usada (ex: "IRRF-2025-V1")
    TabelaIdUsada           NVARCHAR(50)        NOT NULL,
    
    -- Descrição da faixa aplicada (ex: "De R$ 4.664,69 até R$ 6.629,66")
    FaixaAplicada           NVARCHAR(200)       NULL,
    
    -- Alíquota da faixa aplicada (percentual)
    AliquotaAplicada        DECIMAL(5,2)        NOT NULL,
    
    -- Parcela a deduzir da faixa
    ParcelaDedutivelUsada   DECIMAL(18,2)       NOT NULL,
    
    -- ========================================================================
    -- RESULTADO
    -- ========================================================================
    
    -- Indica se o funcionário ficou isento
    Isento                  BIT                 NOT NULL,
    
    -- ========================================================================
    -- CONSTRAINTS
    -- ========================================================================
    
    -- Primary Key
    CONSTRAINT PK_DetalheIrrf 
        PRIMARY KEY (DetalheIrrfId),
    
    -- Foreign Key: Resultado (1:0..1)
    CONSTRAINT FK_DetalheIrrf_ResultadoCalculo
        FOREIGN KEY (ResultadoCalculoId)
        REFERENCES dbo.ResultadoCalculo (ResultadoCalculoId),
    
    -- Unique: Apenas um detalhe IRRF por resultado
    CONSTRAINT UQ_DetalheIrrf_ResultadoCalculo
        UNIQUE (ResultadoCalculoId),
    
    -- Check: Base não pode ser negativa
    CONSTRAINT CK_DetalheIrrf_BaseCalculo_NaoNegativa
        CHECK (BaseCalculo >= 0),
    
    -- Check: Deduções não podem ser negativas
    CONSTRAINT CK_DetalheIrrf_DeducaoInss_NaoNegativa
        CHECK (DeducaoInss >= 0),
    
    CONSTRAINT CK_DetalheIrrf_DeducaoPorDependente_NaoNegativa
        CHECK (DeducaoPorDependente >= 0),
    
    -- Check: Número de dependentes não pode ser negativo
    CONSTRAINT CK_DetalheIrrf_NumeroDependentes_NaoNegativo
        CHECK (NumeroDependentes >= 0),
    
    -- Check: Alíquota válida (0% a 100%)
    CONSTRAINT CK_DetalheIrrf_AliquotaAplicada
        CHECK (AliquotaAplicada >= 0 AND AliquotaAplicada <= 100),
    
    -- Check: Parcela dedutível não pode ser negativa
    CONSTRAINT CK_DetalheIrrf_ParcelaDedutivelUsada_NaoNegativa
        CHECK (ParcelaDedutivelUsada >= 0)
);
GO

-- ============================================================================
-- ÍNDICES
-- ============================================================================

-- Índice: Buscar detalhe por resultado
CREATE NONCLUSTERED INDEX IX_DetalheIrrf_ResultadoCalculo
    ON dbo.DetalheIrrf (ResultadoCalculoId);
GO

-- Índice: Buscar por tabela usada (auditoria)
CREATE NONCLUSTERED INDEX IX_DetalheIrrf_TabelaIdUsada
    ON dbo.DetalheIrrf (TabelaIdUsada);
GO

-- Índice: Buscar isentos (relatórios)
CREATE NONCLUSTERED INDEX IX_DetalheIrrf_Isento
    ON dbo.DetalheIrrf (Isento)
    WHERE Isento = 1;
GO

-- ============================================================================
-- COMENTÁRIOS
-- ============================================================================

-- Esta tabela armazena a memória de cálculo do IRRF para auditoria.
-- Anos depois, é possível explicar:
--   - Quantos dependentes foram considerados
--   - Qual tabela estava vigente
--   - Qual faixa foi aplicada
--   - Por que o funcionário ficou isento (ou não)
--
-- TabelaIdUsada é um identificador (string), NÃO uma FK:
--   - Evita acoplamento com tabela de vigências
--   - Snapshot permanece válido mesmo se tabela original for corrigida
