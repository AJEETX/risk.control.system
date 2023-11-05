CREATE TABLE [dbo].[InvestigationTransaction] (
    [InvestigationTransactionId]   NVARCHAR (450) NOT NULL,
    [ClaimsInvestigationId]        NVARCHAR (450) NULL,
    [InvestigationCaseStatusId]    NVARCHAR (450) NULL,
    [InvestigationCaseSubStatusId] NVARCHAR (450) NULL,
    [Time2Update]                  INT            NULL,
    [HopCount]                     INT            NULL,
    [Sender]                       NVARCHAR (MAX) NULL,
    [Receiver]                     NVARCHAR (MAX) NULL,
    [Message]                      NVARCHAR (MAX) NULL,
    [headerIcon]                   NVARCHAR (MAX) NULL,
    [headerMessage]                NVARCHAR (MAX) NULL,
    [messageIcon]                  NVARCHAR (MAX) NULL,
    [footerIcon]                   NVARCHAR (MAX) NULL,
    [footerMessage]                NVARCHAR (MAX) NULL,
    [CurrentClaimOwner]            NVARCHAR (MAX) NULL,
    [Created]                      DATETIME2 (7)  NOT NULL,
    [Updated]                      DATETIME2 (7)  NULL,
    [UpdatedBy]                    NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_InvestigationTransaction] PRIMARY KEY CLUSTERED ([InvestigationTransactionId] ASC),
    CONSTRAINT [FK_InvestigationTransaction_ClaimsInvestigation_ClaimsInvestigationId] FOREIGN KEY ([ClaimsInvestigationId]) REFERENCES [dbo].[ClaimsInvestigation] ([ClaimsInvestigationId]),
    CONSTRAINT [FK_InvestigationTransaction_InvestigationCaseStatus_InvestigationCaseStatusId] FOREIGN KEY ([InvestigationCaseStatusId]) REFERENCES [dbo].[InvestigationCaseStatus] ([InvestigationCaseStatusId]),
    CONSTRAINT [FK_InvestigationTransaction_InvestigationCaseSubStatus_InvestigationCaseSubStatusId] FOREIGN KEY ([InvestigationCaseSubStatusId]) REFERENCES [dbo].[InvestigationCaseSubStatus] ([InvestigationCaseSubStatusId])
);


GO
CREATE NONCLUSTERED INDEX [IX_InvestigationTransaction_ClaimsInvestigationId]
    ON [dbo].[InvestigationTransaction]([ClaimsInvestigationId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_InvestigationTransaction_InvestigationCaseStatusId]
    ON [dbo].[InvestigationTransaction]([InvestigationCaseStatusId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_InvestigationTransaction_InvestigationCaseSubStatusId]
    ON [dbo].[InvestigationTransaction]([InvestigationCaseSubStatusId] ASC);

