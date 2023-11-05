CREATE TABLE [dbo].[FilesOnFileSystem] (
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [FilePath]    NVARCHAR (MAX) NOT NULL,
    [Name]        NVARCHAR (MAX) NOT NULL,
    [FileType]    NVARCHAR (MAX) NOT NULL,
    [Extension]   NVARCHAR (MAX) NOT NULL,
    [Description] NVARCHAR (MAX) NOT NULL,
    [UploadedBy]  NVARCHAR (MAX) NOT NULL,
    [CreatedOn]   DATETIME2 (7)  NULL,
    [Saved]       BIT            NULL,
    CONSTRAINT [PK_FilesOnFileSystem] PRIMARY KEY CLUSTERED ([Id] ASC)
);

