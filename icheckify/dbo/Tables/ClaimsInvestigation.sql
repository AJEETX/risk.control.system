CREATE TABLE [dbo].[ClaimsInvestigation] (
    [ClaimsInvestigationId]        NVARCHAR (450) NOT NULL,
    [VendorId]                     NVARCHAR (450) NULL,
    [PolicyDetailId]               NVARCHAR (450) NULL,
    [CustomerDetailId]             NVARCHAR (450) NULL,
    [InvestigationCaseStatusId]    NVARCHAR (450) NULL,
    [InvestigationCaseSubStatusId] NVARCHAR (450) NULL,
    [CurrentUserEmail]             NVARCHAR (MAX) NULL,
    [CurrentClaimOwner]            NVARCHAR (MAX) NULL,
    [IsReviewCase]                 BIT            NOT NULL,
    [IsReady2Assign]               BIT            NOT NULL,
    [Deleted]                      BIT            NOT NULL,
    [ClientCompanyId]              NVARCHAR (450) NULL,
    [Created]                      DATETIME2 (7)  NOT NULL,
    [Updated]                      DATETIME2 (7)  NULL,
    [UpdatedBy]                    NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_ClaimsInvestigation] PRIMARY KEY CLUSTERED ([ClaimsInvestigationId] ASC),
    CONSTRAINT [FK_ClaimsInvestigation_ClientCompany_ClientCompanyId] FOREIGN KEY ([ClientCompanyId]) REFERENCES [dbo].[ClientCompany] ([ClientCompanyId]),
    CONSTRAINT [FK_ClaimsInvestigation_CustomerDetail_CustomerDetailId] FOREIGN KEY ([CustomerDetailId]) REFERENCES [dbo].[CustomerDetail] ([CustomerDetailId]),
    CONSTRAINT [FK_ClaimsInvestigation_InvestigationCaseStatus_InvestigationCaseStatusId] FOREIGN KEY ([InvestigationCaseStatusId]) REFERENCES [dbo].[InvestigationCaseStatus] ([InvestigationCaseStatusId]),
    CONSTRAINT [FK_ClaimsInvestigation_InvestigationCaseSubStatus_InvestigationCaseSubStatusId] FOREIGN KEY ([InvestigationCaseSubStatusId]) REFERENCES [dbo].[InvestigationCaseSubStatus] ([InvestigationCaseSubStatusId]),
    CONSTRAINT [FK_ClaimsInvestigation_PolicyDetail_PolicyDetailId] FOREIGN KEY ([PolicyDetailId]) REFERENCES [dbo].[PolicyDetail] ([PolicyDetailId]),
    CONSTRAINT [FK_ClaimsInvestigation_Vendor_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Vendor] ([VendorId])
);


GO
CREATE NONCLUSTERED INDEX [IX_ClaimsInvestigation_ClientCompanyId]
    ON [dbo].[ClaimsInvestigation]([ClientCompanyId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ClaimsInvestigation_CustomerDetailId]
    ON [dbo].[ClaimsInvestigation]([CustomerDetailId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ClaimsInvestigation_InvestigationCaseStatusId]
    ON [dbo].[ClaimsInvestigation]([InvestigationCaseStatusId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ClaimsInvestigation_InvestigationCaseSubStatusId]
    ON [dbo].[ClaimsInvestigation]([InvestigationCaseSubStatusId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ClaimsInvestigation_PolicyDetailId]
    ON [dbo].[ClaimsInvestigation]([PolicyDetailId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ClaimsInvestigation_VendorId]
    ON [dbo].[ClaimsInvestigation]([VendorId] ASC);

