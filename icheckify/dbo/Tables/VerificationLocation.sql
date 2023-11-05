CREATE TABLE [dbo].[VerificationLocation] (
    [VerificationLocationId] NVARCHAR (450) NOT NULL,
    [ClaimsInvestigationId]  NVARCHAR (450) NOT NULL,
    [Addressline]            NVARCHAR (MAX) NULL,
    [PinCodeId]              NVARCHAR (450) NULL,
    [StateId]                NVARCHAR (450) NULL,
    [CountryId]              NVARCHAR (450) NOT NULL,
    [DistrictId]             NVARCHAR (450) NULL,
    CONSTRAINT [PK_VerificationLocation] PRIMARY KEY CLUSTERED ([VerificationLocationId] ASC),
    CONSTRAINT [FK_VerificationLocation_ClaimsInvestigation_ClaimsInvestigationId] FOREIGN KEY ([ClaimsInvestigationId]) REFERENCES [dbo].[ClaimsInvestigation] ([ClaimsInvestigationId]) ON DELETE CASCADE,
    CONSTRAINT [FK_VerificationLocation_Country_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [dbo].[Country] ([CountryId]) ON DELETE CASCADE,
    CONSTRAINT [FK_VerificationLocation_District_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [dbo].[District] ([DistrictId]),
    CONSTRAINT [FK_VerificationLocation_PinCode_PinCodeId] FOREIGN KEY ([PinCodeId]) REFERENCES [dbo].[PinCode] ([PinCodeId]),
    CONSTRAINT [FK_VerificationLocation_State_StateId] FOREIGN KEY ([StateId]) REFERENCES [dbo].[State] ([StateId])
);


GO
CREATE NONCLUSTERED INDEX [IX_VerificationLocation_ClaimsInvestigationId]
    ON [dbo].[VerificationLocation]([ClaimsInvestigationId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_VerificationLocation_CountryId]
    ON [dbo].[VerificationLocation]([CountryId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_VerificationLocation_DistrictId]
    ON [dbo].[VerificationLocation]([DistrictId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_VerificationLocation_PinCodeId]
    ON [dbo].[VerificationLocation]([PinCodeId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_VerificationLocation_StateId]
    ON [dbo].[VerificationLocation]([StateId] ASC);

