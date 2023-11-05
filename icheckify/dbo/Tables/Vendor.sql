CREATE TABLE [dbo].[Vendor] (
    [VendorId]              NVARCHAR (450)  NOT NULL,
    [Name]                  NVARCHAR (MAX)  NOT NULL,
    [Code]                  NVARCHAR (MAX)  NOT NULL,
    [Description]           NVARCHAR (MAX)  NOT NULL,
    [PhoneNumber]           NVARCHAR (MAX)  NOT NULL,
    [Email]                 NVARCHAR (MAX)  NOT NULL,
    [Branch]                NVARCHAR (MAX)  NOT NULL,
    [Addressline]           NVARCHAR (MAX)  NOT NULL,
    [StateId]               NVARCHAR (450)  NULL,
    [CountryId]             NVARCHAR (450)  NULL,
    [PinCodeId]             NVARCHAR (450)  NULL,
    [DistrictId]            NVARCHAR (450)  NULL,
    [BankName]              NVARCHAR (MAX)  NOT NULL,
    [BankAccountNumber]     NVARCHAR (MAX)  NOT NULL,
    [IFSCCode]              NVARCHAR (MAX)  NOT NULL,
    [City]                  NVARCHAR (MAX)  NOT NULL,
    [AgreementDate]         DATETIME2 (7)   NULL,
    [ActivatedDate]         DATETIME2 (7)   NULL,
    [DeListedDate]          DATETIME2 (7)   NULL,
    [Status]                INT             NULL,
    [DomainName]            INT             NULL,
    [DelistReason]          NVARCHAR (MAX)  NULL,
    [DocumentUrl]           NVARCHAR (MAX)  NULL,
    [DocumentImage]         VARBINARY (MAX) NULL,
    [ClientCompanyId]       NVARCHAR (450)  NULL,
    [Deleted]               BIT             NOT NULL,
    [ClaimsInvestigationId] NVARCHAR (450)  NULL,
    [Created]               DATETIME2 (7)   NOT NULL,
    [Updated]               DATETIME2 (7)   NULL,
    [UpdatedBy]             NVARCHAR (MAX)  NULL,
    CONSTRAINT [PK_Vendor] PRIMARY KEY CLUSTERED ([VendorId] ASC),
    CONSTRAINT [FK_Vendor_ClaimsInvestigation_ClaimsInvestigationId] FOREIGN KEY ([ClaimsInvestigationId]) REFERENCES [dbo].[ClaimsInvestigation] ([ClaimsInvestigationId]),
    CONSTRAINT [FK_Vendor_ClientCompany_ClientCompanyId] FOREIGN KEY ([ClientCompanyId]) REFERENCES [dbo].[ClientCompany] ([ClientCompanyId]),
    CONSTRAINT [FK_Vendor_Country_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [dbo].[Country] ([CountryId]),
    CONSTRAINT [FK_Vendor_District_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [dbo].[District] ([DistrictId]),
    CONSTRAINT [FK_Vendor_PinCode_PinCodeId] FOREIGN KEY ([PinCodeId]) REFERENCES [dbo].[PinCode] ([PinCodeId]),
    CONSTRAINT [FK_Vendor_State_StateId] FOREIGN KEY ([StateId]) REFERENCES [dbo].[State] ([StateId])
);


GO
CREATE NONCLUSTERED INDEX [IX_Vendor_ClaimsInvestigationId]
    ON [dbo].[Vendor]([ClaimsInvestigationId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Vendor_ClientCompanyId]
    ON [dbo].[Vendor]([ClientCompanyId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Vendor_CountryId]
    ON [dbo].[Vendor]([CountryId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Vendor_DistrictId]
    ON [dbo].[Vendor]([DistrictId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Vendor_PinCodeId]
    ON [dbo].[Vendor]([PinCodeId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Vendor_StateId]
    ON [dbo].[Vendor]([StateId] ASC);

