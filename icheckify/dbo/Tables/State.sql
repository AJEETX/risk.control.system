CREATE TABLE [dbo].[State] (
    [StateId]   NVARCHAR (450) NOT NULL,
    [Name]      NVARCHAR (MAX) NOT NULL,
    [Code]      NVARCHAR (MAX) NOT NULL,
    [CountryId] NVARCHAR (450) NOT NULL,
    [Created]   DATETIME2 (7)  NOT NULL,
    [Updated]   DATETIME2 (7)  NULL,
    [UpdatedBy] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_State] PRIMARY KEY CLUSTERED ([StateId] ASC),
    CONSTRAINT [FK_State_Country_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [dbo].[Country] ([CountryId]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_State_CountryId]
    ON [dbo].[State]([CountryId] ASC);

