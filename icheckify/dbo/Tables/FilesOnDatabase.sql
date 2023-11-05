CREATE TABLE [dbo].[FilesOnDatabase] (
    [Id]          INT             IDENTITY (1, 1) NOT NULL,
    [Data]        VARBINARY (MAX) NOT NULL,
    [Name]        NVARCHAR (MAX)  NOT NULL,
    [FileType]    NVARCHAR (MAX)  NOT NULL,
    [Extension]   NVARCHAR (MAX)  NOT NULL,
    [Description] NVARCHAR (MAX)  NOT NULL,
    [UploadedBy]  NVARCHAR (MAX)  NOT NULL,
    [CreatedOn]   DATETIME2 (7)   NULL,
    [Saved]       BIT             NULL,
    CONSTRAINT [PK_FilesOnDatabase] PRIMARY KEY CLUSTERED ([Id] ASC)
);

