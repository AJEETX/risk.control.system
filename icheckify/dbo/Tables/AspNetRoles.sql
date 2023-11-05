CREATE TABLE [dbo].[AspNetRoles] (
    [Id]                BIGINT         IDENTITY (1, 1) NOT NULL,
    [Code]              NVARCHAR (20)  NOT NULL,
    [ApplicationUserId] BIGINT         NULL,
    [Name]              NVARCHAR (256) NULL,
    [NormalizedName]    NVARCHAR (256) NULL,
    [ConcurrencyStamp]  NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AspNetRoles_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [dbo].[AspNetUsers] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_AspNetRoles_ApplicationUserId]
    ON [dbo].[AspNetRoles]([ApplicationUserId] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex]
    ON [dbo].[AspNetRoles]([NormalizedName] ASC) WHERE ([NormalizedName] IS NOT NULL);

