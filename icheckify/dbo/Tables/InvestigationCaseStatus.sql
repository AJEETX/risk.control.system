CREATE TABLE [dbo].[InvestigationCaseStatus] (
    [InvestigationCaseStatusId] NVARCHAR (450) NOT NULL,
    [Name]                      NVARCHAR (MAX) NOT NULL,
    [Code]                      NVARCHAR (MAX) NOT NULL,
    [MasterData]                BIT            NOT NULL,
    [Created]                   DATETIME2 (7)  NOT NULL,
    [Updated]                   DATETIME2 (7)  NULL,
    [UpdatedBy]                 NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_InvestigationCaseStatus] PRIMARY KEY CLUSTERED ([InvestigationCaseStatusId] ASC)
);

