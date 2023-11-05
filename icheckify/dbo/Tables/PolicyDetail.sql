CREATE TABLE [dbo].[PolicyDetail] (
    [PolicyDetailId]             NVARCHAR (450)  NOT NULL,
    [ClientCompanyId]            NVARCHAR (450)  NOT NULL,
    [LineOfBusinessId]           NVARCHAR (450)  NULL,
    [InvestigationServiceTypeId] NVARCHAR (450)  NULL,
    [ContractNumber]             NVARCHAR (MAX)  NOT NULL,
    [ContractIssueDate]          DATETIME2 (7)   NOT NULL,
    [ClaimType]                  INT             NULL,
    [DateOfIncident]             DATETIME2 (7)   NOT NULL,
    [CauseOfLoss]                NVARCHAR (MAX)  NOT NULL,
    [SumAssuredValue]            DECIMAL (15, 2) NOT NULL,
    [CostCentreId]               NVARCHAR (450)  NOT NULL,
    [CaseEnablerId]              NVARCHAR (450)  NULL,
    [DocumentImage]              VARBINARY (MAX) NULL,
    [Comments]                   NVARCHAR (MAX)  NULL,
    CONSTRAINT [PK_PolicyDetail] PRIMARY KEY CLUSTERED ([PolicyDetailId] ASC),
    CONSTRAINT [FK_PolicyDetail_CaseEnabler_CaseEnablerId] FOREIGN KEY ([CaseEnablerId]) REFERENCES [dbo].[CaseEnabler] ([CaseEnablerId]),
    CONSTRAINT [FK_PolicyDetail_ClientCompany_ClientCompanyId] FOREIGN KEY ([ClientCompanyId]) REFERENCES [dbo].[ClientCompany] ([ClientCompanyId]) ON DELETE CASCADE,
    CONSTRAINT [FK_PolicyDetail_CostCentre_CostCentreId] FOREIGN KEY ([CostCentreId]) REFERENCES [dbo].[CostCentre] ([CostCentreId]) ON DELETE CASCADE,
    CONSTRAINT [FK_PolicyDetail_InvestigationServiceType_InvestigationServiceTypeId] FOREIGN KEY ([InvestigationServiceTypeId]) REFERENCES [dbo].[InvestigationServiceType] ([InvestigationServiceTypeId]),
    CONSTRAINT [FK_PolicyDetail_LineOfBusiness_LineOfBusinessId] FOREIGN KEY ([LineOfBusinessId]) REFERENCES [dbo].[LineOfBusiness] ([LineOfBusinessId])
);


GO
CREATE NONCLUSTERED INDEX [IX_PolicyDetail_CaseEnablerId]
    ON [dbo].[PolicyDetail]([CaseEnablerId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_PolicyDetail_ClientCompanyId]
    ON [dbo].[PolicyDetail]([ClientCompanyId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_PolicyDetail_CostCentreId]
    ON [dbo].[PolicyDetail]([CostCentreId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_PolicyDetail_InvestigationServiceTypeId]
    ON [dbo].[PolicyDetail]([InvestigationServiceTypeId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_PolicyDetail_LineOfBusinessId]
    ON [dbo].[PolicyDetail]([LineOfBusinessId] ASC);

