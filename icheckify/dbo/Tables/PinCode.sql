CREATE TABLE [dbo].[PinCode] (
    [PinCodeId]  NVARCHAR (450) NOT NULL,
    [Name]       NVARCHAR (MAX) NOT NULL,
    [Code]       NVARCHAR (MAX) NOT NULL,
    [Latitude]   NVARCHAR (MAX) NULL,
    [Longitude]  NVARCHAR (MAX) NULL,
    [DistrictId] NVARCHAR (450) NULL,
    [StateId]    NVARCHAR (450) NULL,
    [CountryId]  NVARCHAR (450) NOT NULL,
    [Created]    DATETIME2 (7)  NOT NULL,
    [Updated]    DATETIME2 (7)  NULL,
    [UpdatedBy]  NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_PinCode] PRIMARY KEY CLUSTERED ([PinCodeId] ASC),
    CONSTRAINT [FK_PinCode_Country_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [dbo].[Country] ([CountryId]) ON DELETE CASCADE,
    CONSTRAINT [FK_PinCode_District_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [dbo].[District] ([DistrictId]),
    CONSTRAINT [FK_PinCode_State_StateId] FOREIGN KEY ([StateId]) REFERENCES [dbo].[State] ([StateId])
);


GO
CREATE NONCLUSTERED INDEX [IX_PinCode_CountryId]
    ON [dbo].[PinCode]([CountryId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_PinCode_DistrictId]
    ON [dbo].[PinCode]([DistrictId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_PinCode_StateId]
    ON [dbo].[PinCode]([StateId] ASC);

