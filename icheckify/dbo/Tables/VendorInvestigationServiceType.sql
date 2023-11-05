CREATE TABLE [dbo].[VendorInvestigationServiceType] (
    [VendorInvestigationServiceTypeId] NVARCHAR (450)  NOT NULL,
    [InvestigationServiceTypeId]       NVARCHAR (450)  NOT NULL,
    [LineOfBusinessId]                 NVARCHAR (450)  NULL,
    [CountryId]                        NVARCHAR (450)  NULL,
    [StateId]                          NVARCHAR (450)  NULL,
    [DistrictId]                       NVARCHAR (450)  NULL,
    [Price]                            DECIMAL (10, 2) NOT NULL,
    [VendorId]                         NVARCHAR (450)  NOT NULL,
    [Deleted]                          BIT             NOT NULL,
    [Created]                          DATETIME2 (7)   NOT NULL,
    [Updated]                          DATETIME2 (7)   NULL,
    [UpdatedBy]                        NVARCHAR (MAX)  NULL,
    CONSTRAINT [PK_VendorInvestigationServiceType] PRIMARY KEY CLUSTERED ([VendorInvestigationServiceTypeId] ASC),
    CONSTRAINT [FK_VendorInvestigationServiceType_Country_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [dbo].[Country] ([CountryId]),
    CONSTRAINT [FK_VendorInvestigationServiceType_District_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [dbo].[District] ([DistrictId]),
    CONSTRAINT [FK_VendorInvestigationServiceType_InvestigationServiceType_InvestigationServiceTypeId] FOREIGN KEY ([InvestigationServiceTypeId]) REFERENCES [dbo].[InvestigationServiceType] ([InvestigationServiceTypeId]) ON DELETE CASCADE,
    CONSTRAINT [FK_VendorInvestigationServiceType_LineOfBusiness_LineOfBusinessId] FOREIGN KEY ([LineOfBusinessId]) REFERENCES [dbo].[LineOfBusiness] ([LineOfBusinessId]),
    CONSTRAINT [FK_VendorInvestigationServiceType_State_StateId] FOREIGN KEY ([StateId]) REFERENCES [dbo].[State] ([StateId]),
    CONSTRAINT [FK_VendorInvestigationServiceType_Vendor_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Vendor] ([VendorId]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_VendorInvestigationServiceType_CountryId]
    ON [dbo].[VendorInvestigationServiceType]([CountryId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_VendorInvestigationServiceType_DistrictId]
    ON [dbo].[VendorInvestigationServiceType]([DistrictId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_VendorInvestigationServiceType_InvestigationServiceTypeId]
    ON [dbo].[VendorInvestigationServiceType]([InvestigationServiceTypeId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_VendorInvestigationServiceType_LineOfBusinessId]
    ON [dbo].[VendorInvestigationServiceType]([LineOfBusinessId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_VendorInvestigationServiceType_StateId]
    ON [dbo].[VendorInvestigationServiceType]([StateId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_VendorInvestigationServiceType_VendorId]
    ON [dbo].[VendorInvestigationServiceType]([VendorId] ASC);

