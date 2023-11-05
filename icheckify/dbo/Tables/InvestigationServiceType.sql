CREATE TABLE [dbo].[InvestigationServiceType] (
    [InvestigationServiceTypeId] NVARCHAR (450) NOT NULL,
    [Name]                       NVARCHAR (MAX) NOT NULL,
    [Code]                       NVARCHAR (MAX) NOT NULL,
    [LineOfBusinessId]           NVARCHAR (450) NOT NULL,
    [MasterData]                 BIT            NOT NULL,
    [Created]                    DATETIME2 (7)  NOT NULL,
    [Updated]                    DATETIME2 (7)  NULL,
    [UpdatedBy]                  NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_InvestigationServiceType] PRIMARY KEY CLUSTERED ([InvestigationServiceTypeId] ASC),
    CONSTRAINT [FK_InvestigationServiceType_LineOfBusiness_LineOfBusinessId] FOREIGN KEY ([LineOfBusinessId]) REFERENCES [dbo].[LineOfBusiness] ([LineOfBusinessId]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_InvestigationServiceType_LineOfBusinessId]
    ON [dbo].[InvestigationServiceType]([LineOfBusinessId] ASC);

