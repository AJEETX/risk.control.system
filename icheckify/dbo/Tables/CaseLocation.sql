CREATE TABLE [dbo].[CaseLocation] (
    [CaseLocationId]               BIGINT          IDENTITY (1, 1) NOT NULL,
    [BeneficiaryName]              NVARCHAR (MAX)  NOT NULL,
    [BeneficiaryRelationId]        BIGINT          NOT NULL,
    [BeneficiaryContactNumber]     BIGINT          NOT NULL,
    [BeneficiaryIncome]            INT             NULL,
    [ProfilePictureUrl]            NVARCHAR (MAX)  NULL,
    [ProfilePicture]               VARBINARY (MAX) NULL,
    [BeneficiaryDateOfBirth]       DATETIME2 (7)   NOT NULL,
    [CountryId]                    NVARCHAR (450)  NULL,
    [StateId]                      NVARCHAR (450)  NULL,
    [DistrictId]                   NVARCHAR (450)  NULL,
    [PinCodeId]                    NVARCHAR (450)  NULL,
    [Addressline]                  NVARCHAR (MAX)  NOT NULL,
    [Addressline2]                 NVARCHAR (MAX)  NULL,
    [ClaimsInvestigationId]        NVARCHAR (450)  NOT NULL,
    [VendorId]                     NVARCHAR (450)  NULL,
    [InvestigationCaseSubStatusId] NVARCHAR (450)  NULL,
    [AssignedAgentUserEmail]       NVARCHAR (MAX)  NULL,
    [IsReviewCaseLocation]         BIT             NOT NULL,
    [Created]                      DATETIME2 (7)   NOT NULL,
    [Updated]                      DATETIME2 (7)   NULL,
    [UpdatedBy]                    NVARCHAR (MAX)  NULL,
    CONSTRAINT [PK_CaseLocation] PRIMARY KEY CLUSTERED ([CaseLocationId] ASC),
    CONSTRAINT [FK_CaseLocation_BeneficiaryRelation_BeneficiaryRelationId] FOREIGN KEY ([BeneficiaryRelationId]) REFERENCES [dbo].[BeneficiaryRelation] ([BeneficiaryRelationId]) ON DELETE CASCADE,
    CONSTRAINT [FK_CaseLocation_ClaimsInvestigation_ClaimsInvestigationId] FOREIGN KEY ([ClaimsInvestigationId]) REFERENCES [dbo].[ClaimsInvestigation] ([ClaimsInvestigationId]) ON DELETE CASCADE,
    CONSTRAINT [FK_CaseLocation_Country_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [dbo].[Country] ([CountryId]),
    CONSTRAINT [FK_CaseLocation_District_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [dbo].[District] ([DistrictId]),
    CONSTRAINT [FK_CaseLocation_InvestigationCaseSubStatus_InvestigationCaseSubStatusId] FOREIGN KEY ([InvestigationCaseSubStatusId]) REFERENCES [dbo].[InvestigationCaseSubStatus] ([InvestigationCaseSubStatusId]),
    CONSTRAINT [FK_CaseLocation_PinCode_PinCodeId] FOREIGN KEY ([PinCodeId]) REFERENCES [dbo].[PinCode] ([PinCodeId]),
    CONSTRAINT [FK_CaseLocation_State_StateId] FOREIGN KEY ([StateId]) REFERENCES [dbo].[State] ([StateId]),
    CONSTRAINT [FK_CaseLocation_Vendor_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Vendor] ([VendorId])
);


GO
CREATE NONCLUSTERED INDEX [IX_CaseLocation_BeneficiaryRelationId]
    ON [dbo].[CaseLocation]([BeneficiaryRelationId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_CaseLocation_ClaimsInvestigationId]
    ON [dbo].[CaseLocation]([ClaimsInvestigationId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_CaseLocation_CountryId]
    ON [dbo].[CaseLocation]([CountryId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_CaseLocation_DistrictId]
    ON [dbo].[CaseLocation]([DistrictId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_CaseLocation_InvestigationCaseSubStatusId]
    ON [dbo].[CaseLocation]([InvestigationCaseSubStatusId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_CaseLocation_PinCodeId]
    ON [dbo].[CaseLocation]([PinCodeId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_CaseLocation_StateId]
    ON [dbo].[CaseLocation]([StateId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_CaseLocation_VendorId]
    ON [dbo].[CaseLocation]([VendorId] ASC);

