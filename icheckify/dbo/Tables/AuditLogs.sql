CREATE TABLE [dbo].[AuditLogs] (
    [Id]              BIGINT         IDENTITY (1, 1) NOT NULL,
    [UserId]          NVARCHAR (MAX) NULL,
    [Type]            NVARCHAR (MAX) NOT NULL,
    [TableName]       NVARCHAR (MAX) NULL,
    [DateTime]        DATETIME2 (7)  NOT NULL,
    [OldValues]       NVARCHAR (MAX) NULL,
    [NewValues]       NVARCHAR (MAX) NULL,
    [AffectedColumns] NVARCHAR (MAX) NULL,
    [PrimaryKey]      NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
);

