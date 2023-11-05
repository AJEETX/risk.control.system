CREATE TABLE [dbo].[InvestigationCaseOutcome] (
    [InvestigationCaseOutcomeId] NVARCHAR (450) NOT NULL,
    [Name]                       NVARCHAR (MAX) NOT NULL,
    [Code]                       NVARCHAR (MAX) NOT NULL,
    [Created]                    DATETIME2 (7)  NOT NULL,
    [Updated]                    DATETIME2 (7)  NULL,
    [UpdatedBy]                  NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_InvestigationCaseOutcome] PRIMARY KEY CLUSTERED ([InvestigationCaseOutcomeId] ASC)
);

