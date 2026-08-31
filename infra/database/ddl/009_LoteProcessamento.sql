-- ============================================================================
-- DDL: Lote de Processamento e Itens
-- Sistema de Folha de Pagamento
-- ============================================================================

-- ============================================================================
-- TABELA: LoteProcessamento
-- Armazena lotes de processamento em massa
-- ============================================================================
CREATE TABLE dbo.LoteProcessamento (
    LoteId                  UNIQUEIDENTIFIER    NOT NULL,
    CompetenciaAno          INT                 NOT NULL,
    CompetenciaMes          INT                 NOT NULL,
    Status                  NVARCHAR(30)        NOT NULL,       -- Pendente, EmProcessamento, Concluido, ConcluidoComFalhas, Cancelado
    TotalItens              INT                 NOT NULL,
    ItensConcluidos         INT                 NOT NULL DEFAULT 0,
    ItensComFalha           INT                 NOT NULL DEFAULT 0,
    ItensIgnorados          INT                 NOT NULL DEFAULT 0,
    CriadoEm                DATETIME2           NOT NULL,
    IniciadoEm              DATETIME2           NULL,
    ConcluidoEm             DATETIME2           NULL,
    UsuarioId               NVARCHAR(100)       NULL,
    Observacao              NVARCHAR(500)       NULL,

    CONSTRAINT PK_LoteProcessamento PRIMARY KEY (LoteId),
    
    CONSTRAINT CK_LoteProcessamento_Status CHECK (
        Status IN ('Pendente', 'EmProcessamento', 'Concluido', 'ConcluidoComFalhas', 'Cancelado')
    ),
    
    CONSTRAINT CK_LoteProcessamento_CompetenciaMes CHECK (CompetenciaMes BETWEEN 1 AND 12),
    CONSTRAINT CK_LoteProcessamento_TotalItens CHECK (TotalItens >= 0)
);
GO

-- Índices
CREATE INDEX IX_LoteProcessamento_Competencia 
    ON dbo.LoteProcessamento (CompetenciaAno, CompetenciaMes);

CREATE INDEX IX_LoteProcessamento_Status 
    ON dbo.LoteProcessamento (Status);

CREATE INDEX IX_LoteProcessamento_CriadoEm 
    ON dbo.LoteProcessamento (CriadoEm DESC);
GO

-- ============================================================================
-- TABELA: ItemLote
-- Itens individuais de um lote (um por funcionário)
-- ============================================================================
CREATE TABLE dbo.ItemLote (
    ItemLoteId              UNIQUEIDENTIFIER    NOT NULL,
    LoteId                  UNIQUEIDENTIFIER    NOT NULL,
    FuncionarioId           UNIQUEIDENTIFIER    NOT NULL,
    Status                  NVARCHAR(20)        NOT NULL,       -- Pendente, EmProcessamento, Sucesso, Falha, Ignorado
    ProcessamentoVersaoId   UNIQUEIDENTIFIER    NULL,           -- Referência ao processamento criado
    VersaoNumero            INT                 NULL,
    MensagemErro            NVARCHAR(1000)      NULL,
    Tentativas              INT                 NOT NULL DEFAULT 0,
    IniciadoEm              DATETIME2           NULL,
    ConcluidoEm             DATETIME2           NULL,

    CONSTRAINT PK_ItemLote PRIMARY KEY (ItemLoteId),
    
    CONSTRAINT FK_ItemLote_LoteProcessamento 
        FOREIGN KEY (LoteId) REFERENCES dbo.LoteProcessamento (LoteId)
        ON DELETE CASCADE,
    
    CONSTRAINT FK_ItemLote_Funcionario 
        FOREIGN KEY (FuncionarioId) REFERENCES dbo.Funcionario (FuncionarioId)
        ON DELETE NO ACTION,
    
    CONSTRAINT FK_ItemLote_ProcessamentoVersao 
        FOREIGN KEY (ProcessamentoVersaoId) REFERENCES dbo.ProcessamentoVersao (ProcessamentoVersaoId)
        ON DELETE SET NULL,
    
    CONSTRAINT CK_ItemLote_Status CHECK (
        Status IN ('Pendente', 'EmProcessamento', 'Sucesso', 'Falha', 'Ignorado')
    )
);
GO

-- Índices
CREATE INDEX IX_ItemLote_Lote 
    ON dbo.ItemLote (LoteId);

CREATE INDEX IX_ItemLote_Funcionario 
    ON dbo.ItemLote (FuncionarioId);

CREATE INDEX IX_ItemLote_Status 
    ON dbo.ItemLote (Status);

CREATE INDEX IX_ItemLote_LoteStatus 
    ON dbo.ItemLote (LoteId, Status);
GO

-- ============================================================================
-- COMENTÁRIOS
-- ============================================================================

-- Adicionar descrições às tabelas
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Lotes de processamento em massa de folha de pagamento',
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'LoteProcessamento';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Itens individuais de um lote de processamento (um por funcionário)',
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'ItemLote';
GO
