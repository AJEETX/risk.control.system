CREATE TABLE [dbo].[District] (
    [DistrictId] NVARCHAR (450) NOT NULL,
    [Name]       NVARCHAR (MAX) NOT NULL,
    [Code]       NVARCHAR (MAX) NOT NULL,
    [StateId]    NVARCHAR (450) NULL,
    [CountryId]  NVARCHAR (450) NOT NULL,
    [Created]    DATETIME2 (7)  NOT NULL,
    [Updated]    DATETIME2 (7)  NULL,
    [UpdatedBy]  NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_District] PRIMARY KEY CLUSTERED ([DistrictId] ASC),
    CONSTRAINT [FK_District_Country_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [dbo].[Country] ([CountryId]) ON DELETE CASCADE,
    CONSTRAINT [FK_District_State_StateId] FOREIGN KEY ([StateId]) REFERENCES [dbo].[State] ([StateId])
);


GO
CREATE NONCLUSTERED INDEX [IX_District_CountryId]
    ON [dbo].[District]([CountryId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_District_StateId]
    ON [dbo].[District]([StateId] ASC);

