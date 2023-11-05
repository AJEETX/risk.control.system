CREATE TABLE [dbo].[InvestigationCaseSubStatus] (
    [InvestigationCaseSubStatusId] NVARCHAR (450) NOT NULL,
    [Name]                         NVARCHAR (MAX) NOT NULL,
    [Code]                         NVARCHAR (MAX) NOT NULL,
    [InvestigationCaseStatusId]    NVARCHAR (450) NULL,
    [MasterData]                   BIT            NOT NULL,
    [Created]                      DATETIME2 (7)  NOT NULL,
    [Updated]                      DATETIME2 (7)  NULL,
    [UpdatedBy]                    NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_InvestigationCaseSubStatus] PRIMARY KEY CLUSTERED ([InvestigationCaseSubStatusId] ASC),
    CONSTRAINT [FK_InvestigationCaseSubStatus_InvestigationCaseStatus_InvestigationCaseStatusId] FOREIGN KEY ([InvestigationCaseStatusId]) REFERENCES [dbo].[InvestigationCaseStatus] ([InvestigationCaseStatusId])
);


GO
CREATE NONCLUSTERED INDEX [IX_InvestigationCaseSubStatus_InvestigationCaseStatusId]
    ON [dbo].[InvestigationCaseSubStatus]([InvestigationCaseStatusId] ASC);

