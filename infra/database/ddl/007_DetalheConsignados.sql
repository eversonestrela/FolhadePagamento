-- ============================================================================
-- ARQUIVO: 007_DetalheConsignados.sql
-- PROPÓSITO: Tabela de memória de cálculo dos consignados
-- ENTIDADE DE DOMÍNIO: ResultadoCalculoConsignados
-- ENTIDADE DE PERSISTÊNCIA: DetalheConsignadosDb
-- ============================================================================
-- 
-- CARACTERÍSTICAS:
--   - Entidade IMUTÁVEL (memória de cálculo auditável)
--   - Relacionamento 1:0..1 com ResultadoCalculo
--   - Armazena COMO os consignados foram calculados
--
-- CONSIGNADOS:
--   - Descontos de empréstimos consignados em folha
--   - Limitados por margem consignável (ex: 35% do salário)
--   - Array de contratos em JSON (tamanho variável)
--
-- JSON (DescontosJson):
--   Usado para lista de contratos (tamanho variável).
--   Valores finais (ValorConsignados) estão em coluna tipada.
--
-- ============================================================================

CREATE TABLE dbo.DetalheConsignados
(
    -- ========================================================================
    -- IDENTIFICAÇÃO
    -- ========================================================================
    
    -- Identificador único do detalhe
    DetalheConsignadosId    UNIQUEIDENTIFIER    NOT NULL,
    
    -- Resultado ao qual este detalhe pertence (1:0..1)
    ResultadoCalculoId      UNIQUEIDENTIFIER    NOT NULL,
    
    -- ========================================================================
    -- MEMÓRIA DE CÁLCULO - BASE E MARGEM
    -- ========================================================================
    
    -- Salário base considerado para cálculo da margem
    SalarioBaseConsiderado  DECIMAL(18,2)       NOT NULL,
    
    -- Percentual da margem consignável (ex: 35.00)
    PercentualMargem        DECIMAL(5,2)        NOT NULL,
    
    -- Valor máximo descontável (SalarioBase * PercentualMargem / 100)
    MargemTotal             DECIMAL(18,2)       NOT NULL,
    
    -- Valor efetivamente utilizado (soma dos descontos)
    MargemUtilizada         DECIMAL(18,2)       NOT NULL,
    
    -- Saldo disponível (MargemTotal - MargemUtilizada)
    MargemDisponivel        DECIMAL(18,2)       NOT NULL,
    
    -- ========================================================================
    -- MEMÓRIA DE CÁLCULO - CONTRATOS
    -- ========================================================================
    
    -- Quantidade de contratos ativos descontados
    TotalContratosAtivos    INT                 NOT NULL,
    
    -- Array de descontos por contrato (estrutura variável)
    -- Formato: [{"contratoId": "GUID", "parcela": 5, "totalParcelas": 24, "valor": 150.00}, ...]
    DescontosJson           NVARCHAR(MAX)       NULL,
    
    -- ========================================================================
    -- CONSTRAINTS
    -- ========================================================================
    
    -- Primary Key
    CONSTRAINT PK_DetalheConsignados 
        PRIMARY KEY (DetalheConsignadosId),
    
    -- Foreign Key: Resultado (1:0..1)
    CONSTRAINT FK_DetalheConsignados_ResultadoCalculo
        FOREIGN KEY (ResultadoCalculoId)
        REFERENCES dbo.ResultadoCalculo (ResultadoCalculoId),
    
    -- Unique: Apenas um detalhe Consignados por resultado
    CONSTRAINT UQ_DetalheConsignados_ResultadoCalculo
        UNIQUE (ResultadoCalculoId),
    
    -- Check: Valores não podem ser negativos
    CONSTRAINT CK_DetalheConsignados_SalarioBaseConsiderado_NaoNegativo
        CHECK (SalarioBaseConsiderado >= 0),
    
    CONSTRAINT CK_DetalheConsignados_MargemTotal_NaoNegativa
        CHECK (MargemTotal >= 0),
    
    CONSTRAINT CK_DetalheConsignados_MargemUtilizada_NaoNegativa
        CHECK (MargemUtilizada >= 0),
    
    CONSTRAINT CK_DetalheConsignados_MargemDisponivel_NaoNegativa
        CHECK (MargemDisponivel >= 0),
    
    -- Check: Percentual válido (0% a 100%)
    CONSTRAINT CK_DetalheConsignados_PercentualMargem
        CHECK (PercentualMargem >= 0 AND PercentualMargem <= 100),
    
    -- Check: Número de contratos não pode ser negativo
    CONSTRAINT CK_DetalheConsignados_TotalContratosAtivos_NaoNegativo
        CHECK (TotalContratosAtivos >= 0),
    
    -- Check: JSON válido (se preenchido)
    CONSTRAINT CK_DetalheConsignados_DescontosJson
        CHECK (DescontosJson IS NULL OR ISJSON(DescontosJson) = 1)
);
GO

-- ============================================================================
-- ÍNDICES
-- ============================================================================

-- Índice: Buscar detalhe por resultado
CREATE NONCLUSTERED INDEX IX_DetalheConsignados_ResultadoCalculo
    ON dbo.DetalheConsignados (ResultadoCalculoId);
GO

-- Índice: Buscar por quantidade de contratos (análise)
CREATE NONCLUSTERED INDEX IX_DetalheConsignados_TotalContratosAtivos
    ON dbo.DetalheConsignados (TotalContratosAtivos)
    WHERE TotalContratosAtivos > 0;
GO

-- ============================================================================
-- COMENTÁRIOS
-- ============================================================================

-- Esta tabela armazena a memória de cálculo dos consignados para auditoria.
-- Anos depois, é possível explicar:
--   - Qual margem estava disponível
--   - Quantos contratos foram descontados
--   - Quanto cada contrato consumiu da margem
--
-- DescontosJson usa JSON porque:
--   - Número de contratos varia (0 a N)
--   - Evita explosão de tabelas filhas
--   - Valor final (ValorConsignados) está em coluna tipada em ResultadoCalculo
--
-- IMPORTANTE:
--   O Core é responsável por validar que MargemUtilizada <= MargemTotal.
--   O banco NÃO valida esta regra (pertence ao Core).
