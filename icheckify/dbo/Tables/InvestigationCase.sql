CREATE TABLE [dbo].[InvestigationCase] (
    [InvestigationId]            NVARCHAR (450) NOT NULL,
    [Name]                       NVARCHAR (MAX) NOT NULL,
    [Description]                NVARCHAR (MAX) NOT NULL,
    [LineOfBusinessId]           NVARCHAR (450) NULL,
    [InvestigationServiceTypeId] NVARCHAR (450) NULL,
    [InvestigationCaseStatusId]  NVARCHAR (450) NULL,
    [Created]                    DATETIME2 (7)  NOT NULL,
    [Updated]                    DATETIME2 (7)  NULL,
    [UpdatedBy]                  NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_InvestigationCase] PRIMARY KEY CLUSTERED ([InvestigationId] ASC),
    CONSTRAINT [FK_InvestigationCase_InvestigationCaseStatus_InvestigationCaseStatusId] FOREIGN KEY ([InvestigationCaseStatusId]) REFERENCES [dbo].[InvestigationCaseStatus] ([InvestigationCaseStatusId]),
    CONSTRAINT [FK_InvestigationCase_InvestigationServiceType_InvestigationServiceTypeId] FOREIGN KEY ([InvestigationServiceTypeId]) REFERENCES [dbo].[InvestigationServiceType] ([InvestigationServiceTypeId]),
    CONSTRAINT [FK_InvestigationCase_LineOfBusiness_LineOfBusinessId] FOREIGN KEY ([LineOfBusinessId]) REFERENCES [dbo].[LineOfBusiness] ([LineOfBusinessId])
);


GO
CREATE NONCLUSTERED INDEX [IX_InvestigationCase_InvestigationCaseStatusId]
    ON [dbo].[InvestigationCase]([InvestigationCaseStatusId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_InvestigationCase_InvestigationServiceTypeId]
    ON [dbo].[InvestigationCase]([InvestigationServiceTypeId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_InvestigationCase_LineOfBusinessId]
    ON [dbo].[InvestigationCase]([LineOfBusinessId] ASC);

