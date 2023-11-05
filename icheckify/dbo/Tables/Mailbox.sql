CREATE TABLE [dbo].[Mailbox] (
    [MailboxId]         BIGINT         IDENTITY (1, 1) NOT NULL,
    [Name]              NVARCHAR (MAX) NOT NULL,
    [ApplicationUserId] BIGINT         NOT NULL,
    [Created]           DATETIME2 (7)  NOT NULL,
    [Updated]           DATETIME2 (7)  NULL,
    [UpdatedBy]         NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_Mailbox] PRIMARY KEY CLUSTERED ([MailboxId] ASC),
    CONSTRAINT [FK_Mailbox_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Mailbox_ApplicationUserId]
    ON [dbo].[Mailbox]([ApplicationUserId] ASC);

