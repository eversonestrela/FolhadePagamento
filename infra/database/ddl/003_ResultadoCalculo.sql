-- ============================================================================
-- ARQUIVO: 003_ResultadoCalculo.sql
-- PROPÓSITO: Tabela de snapshot do resultado de cálculo
-- ENTIDADE DE DOMÍNIO: ResultadoCalculo
-- ENTIDADE DE PERSISTÊNCIA: ResultadoCalculoDb
-- ============================================================================
-- 
-- CARACTERÍSTICAS:
--   - Entidade IMUTÁVEL (snapshot congelado)
--   - Relacionamento 1:1 com ProcessamentoVersao
--   - Contém todos os valores finais do cálculo
--   - Não depende de dados externos para ser interpretado
--
-- SNAPSHOT vs REFERÊNCIAS:
--   ❌ Não usamos FK para TabelaInss, TabelaIrrf (evita acoplamento)
--   ✅ Armazenamos valores congelados (independente de mudanças futuras)
--
-- IMPORTANTE:
--   - Mesmo que TabelaInss seja corrigida, este snapshot permanece intacto
--   - Auditoria pode recalcular hash e verificar integridade
--
-- ============================================================================

CREATE TABLE dbo.ResultadoCalculo
(
    -- ========================================================================
    -- IDENTIFICAÇÃO
    -- ========================================================================
    
    -- Identificador único do resultado
    ResultadoCalculoId      UNIQUEIDENTIFIER    NOT NULL,
    
    -- Processamento ao qual este resultado pertence (1:1)
    ProcessamentoVersaoId   UNIQUEIDENTIFIER    NOT NULL,
    
    -- ========================================================================
    -- VALORES DO CÁLCULO (SNAPSHOT)
    -- ========================================================================
    
    -- Salário bruto (proventos)
    SalarioBruto            DECIMAL(18,2)       NOT NULL,
    
    -- Desconto de INSS (contribuição do funcionário)
    ValorInss               DECIMAL(18,2)       NOT NULL,
    
    -- Desconto de IRRF (imposto de renda)
    ValorIrrf               DECIMAL(18,2)       NOT NULL,
    
    -- Valor de FGTS (encargo patronal, não desconta do funcionário)
    ValorFgts               DECIMAL(18,2)       NOT NULL,
    
    -- Total de consignados descontados
    ValorConsignados        DECIMAL(18,2)       NOT NULL,
    
    -- Total de descontos (INSS + IRRF + Consignados + outros)
    -- NOTA: FGTS NÃO entra aqui (é encargo patronal)
    TotalDescontos          DECIMAL(18,2)       NOT NULL,
    
    -- Salário líquido (Bruto - TotalDescontos)
    SalarioLiquido          DECIMAL(18,2)       NOT NULL,
    
    -- ========================================================================
    -- ENCARGOS PATRONAIS
    -- ========================================================================
    
    -- Total de encargos patronais (FGTS + futuros)
    TotalEncargosPatronais  DECIMAL(18,2)       NOT NULL,
    
    -- Custo total do funcionário para o empregador
    -- = SalarioBruto + TotalEncargosPatronais
    CustoTotalEmpregador    DECIMAL(18,2)       NOT NULL,
    
    -- ========================================================================
    -- AUDITORIA
    -- ========================================================================
    
    -- Timestamp de quando o cálculo foi realizado
    -- Fornecido pelo Core (não usa SYSUTCDATETIME para garantir determinismo)
    CalculadoEm             DATETIME2(7)        NOT NULL,
    
    -- ========================================================================
    -- CONSTRAINTS
    -- ========================================================================
    
    -- Primary Key
    CONSTRAINT PK_ResultadoCalculo 
        PRIMARY KEY (ResultadoCalculoId),
    
    -- Foreign Key: Processamento (1:1)
    CONSTRAINT FK_ResultadoCalculo_ProcessamentoVersao
        FOREIGN KEY (ProcessamentoVersaoId)
        REFERENCES dbo.ProcessamentoVersao (ProcessamentoVersaoId),
    
    -- Unique: Apenas um resultado por processamento
    CONSTRAINT UQ_ResultadoCalculo_ProcessamentoVersao
        UNIQUE (ProcessamentoVersaoId),
    
    -- Check: Valores não podem ser negativos
    CONSTRAINT CK_ResultadoCalculo_SalarioBruto_NaoNegativo
        CHECK (SalarioBruto >= 0),
    
    CONSTRAINT CK_ResultadoCalculo_ValorInss_NaoNegativo
        CHECK (ValorInss >= 0),
    
    CONSTRAINT CK_ResultadoCalculo_ValorIrrf_NaoNegativo
        CHECK (ValorIrrf >= 0),
    
    CONSTRAINT CK_ResultadoCalculo_ValorFgts_NaoNegativo
        CHECK (ValorFgts >= 0),
    
    CONSTRAINT CK_ResultadoCalculo_ValorConsignados_NaoNegativo
        CHECK (ValorConsignados >= 0),
    
    CONSTRAINT CK_ResultadoCalculo_TotalDescontos_NaoNegativo
        CHECK (TotalDescontos >= 0),
    
    CONSTRAINT CK_ResultadoCalculo_SalarioLiquido_NaoNegativo
        CHECK (SalarioLiquido >= 0)
);
GO

-- ============================================================================
-- ÍNDICES
-- ============================================================================

-- Índice: Buscar resultado por processamento
CREATE NONCLUSTERED INDEX IX_ResultadoCalculo_ProcessamentoVersao
    ON dbo.ResultadoCalculo (ProcessamentoVersaoId);
GO

-- ============================================================================
-- COMENTÁRIOS
-- ============================================================================

-- Esta tabela armazena o snapshot completo do resultado de cálculo.
-- É IMUTÁVEL - uma vez inserida, nunca é alterada ou deletada.
-- Contém valores congelados, independentes de mudanças em tabelas de referência.
-- 
-- O Core é responsável por:
--   - Calcular todos os valores
--   - Garantir que SalarioLiquido = SalarioBruto - TotalDescontos
--   - Fornecer CalculadoEm (timestamp determinístico)
--
-- O banco NÃO valida a fórmula (regra de negócio pertence ao Core).
