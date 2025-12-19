-- ============================================================================
-- ARQUIVO: 004_DetalheInss.sql
-- PROPÓSITO: Tabela de memória de cálculo do INSS
-- ENTIDADE DE DOMÍNIO: ResultadoCalculoInss
-- ENTIDADE DE PERSISTÊNCIA: DetalheInssDb
-- ============================================================================
-- 
-- CARACTERÍSTICAS:
--   - Entidade IMUTÁVEL (memória de cálculo auditável)
--   - Relacionamento 1:0..1 com ResultadoCalculo
--   - Armazena COMO o INSS foi calculado, não apenas o valor final
--
-- MEMÓRIA DE CÁLCULO:
--   - Qual tabela INSS foi usada
--   - Qual base de cálculo
--   - Se atingiu o teto
--   - Contribuição por faixa progressiva (JSON)
--
-- JSON (ContribuicaoPorFaixaJson):
--   Usado para estrutura interna variável (1 a 4 faixas).
--   NUNCA armazena valores finais de negócio em JSON.
--
-- ============================================================================

CREATE TABLE dbo.DetalheInss
(
    -- ========================================================================
    -- IDENTIFICAÇÃO
    -- ========================================================================
    
    -- Identificador único do detalhe
    DetalheInssId           UNIQUEIDENTIFIER    NOT NULL,
    
    -- Resultado ao qual este detalhe pertence (1:0..1)
    ResultadoCalculoId      UNIQUEIDENTIFIER    NOT NULL,
    
    -- ========================================================================
    -- MEMÓRIA DE CÁLCULO
    -- ========================================================================
    
    -- Base de cálculo usada (normalmente = SalarioBruto)
    BaseCalculo             DECIMAL(18,2)       NOT NULL,
    
    -- Identificador da tabela INSS usada (ex: "INSS-2025-V1")
    -- NOTA: Armazenamos identificador, não FK (snapshot)
    TabelaIdUsada           NVARCHAR(50)        NOT NULL,
    
    -- Alíquota efetiva resultante (percentual)
    AliquotaEfetiva         DECIMAL(5,2)        NOT NULL,
    
    -- Indica se o teto de contribuição foi aplicado
    TetoAplicado            BIT                 NOT NULL,
    
    -- Detalhes de contribuição por faixa (estrutura variável)
    -- Formato: [{"faixa": 1, "limite": 1518.00, "aliquota": 7.5, "contribuicao": 113.85}, ...]
    ContribuicaoPorFaixaJson NVARCHAR(MAX)      NULL,
    
    -- ========================================================================
    -- CONSTRAINTS
    -- ========================================================================
    
    -- Primary Key
    CONSTRAINT PK_DetalheInss 
        PRIMARY KEY (DetalheInssId),
    
    -- Foreign Key: Resultado (1:0..1)
    CONSTRAINT FK_DetalheInss_ResultadoCalculo
        FOREIGN KEY (ResultadoCalculoId)
        REFERENCES dbo.ResultadoCalculo (ResultadoCalculoId),
    
    -- Unique: Apenas um detalhe INSS por resultado
    CONSTRAINT UQ_DetalheInss_ResultadoCalculo
        UNIQUE (ResultadoCalculoId),
    
    -- Check: Base não pode ser negativa
    CONSTRAINT CK_DetalheInss_BaseCalculo_NaoNegativa
        CHECK (BaseCalculo >= 0),
    
    -- Check: Alíquota válida (0% a 100%)
    CONSTRAINT CK_DetalheInss_AliquotaEfetiva
        CHECK (AliquotaEfetiva >= 0 AND AliquotaEfetiva <= 100),
    
    -- Check: JSON válido (se preenchido)
    CONSTRAINT CK_DetalheInss_ContribuicaoPorFaixaJson
        CHECK (ContribuicaoPorFaixaJson IS NULL OR ISJSON(ContribuicaoPorFaixaJson) = 1)
);
GO

-- ============================================================================
-- ÍNDICES
-- ============================================================================

-- Índice: Buscar detalhe por resultado
CREATE NONCLUSTERED INDEX IX_DetalheInss_ResultadoCalculo
    ON dbo.DetalheInss (ResultadoCalculoId);
GO

-- Índice: Buscar por tabela usada (auditoria)
CREATE NONCLUSTERED INDEX IX_DetalheInss_TabelaIdUsada
    ON dbo.DetalheInss (TabelaIdUsada);
GO

-- ============================================================================
-- COMENTÁRIOS
-- ============================================================================

-- Esta tabela armazena a memória de cálculo do INSS para auditoria.
-- Anos depois, é possível explicar exatamente por que o INSS foi X.
-- 
-- TabelaIdUsada é um identificador (string), NÃO uma FK:
--   - Evita acoplamento com tabela de vigências
--   - Snapshot permanece válido mesmo se tabela original for corrigida
--
-- ContribuicaoPorFaixaJson usa JSON porque:
--   - Número de faixas varia (1 a 4)
--   - Evita explosão de tabelas filhas
--   - Valores finais (ValorInss) estão em coluna tipada em ResultadoCalculo
