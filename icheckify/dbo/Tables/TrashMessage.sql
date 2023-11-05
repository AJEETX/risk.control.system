CREATE TABLE [dbo].[TrashMessage] (
    [TrashMessageId]  BIGINT          IDENTITY (1, 1) NOT NULL,
    [SenderEmail]     NVARCHAR (MAX)  NOT NULL,
    [ReceipientEmail] NVARCHAR (MAX)  NOT NULL,
    [Subject]         NVARCHAR (MAX)  NOT NULL,
    [RawMessage]      NVARCHAR (MAX)  NULL,
    [Message]         NVARCHAR (MAX)  NOT NULL,
    [Read]            BIT             NOT NULL,
    [Priority]        INT             NOT NULL,
    [SendDate]        DATETIME2 (7)   NULL,
    [ReceiveDate]     DATETIME2 (7)   NULL,
    [Attachment]      VARBINARY (MAX) NULL,
    [AttachmentName]  NVARCHAR (MAX)  NULL,
    [FileType]        NVARCHAR (MAX)  NULL,
    [Extension]       NVARCHAR (MAX)  NULL,
    [IsDraft]         BIT             NULL,
    [Trashed]         BIT             NULL,
    [DeleteTrashed]   BIT             NULL,
    [MessageStatus]   INT             NOT NULL,
    [MailboxId]       BIGINT          NOT NULL,
    [Created]         DATETIME2 (7)   NOT NULL,
    [Updated]         DATETIME2 (7)   NULL,
    [UpdatedBy]       NVARCHAR (MAX)  NULL,
    CONSTRAINT [PK_TrashMessage] PRIMARY KEY CLUSTERED ([TrashMessageId] ASC),
    CONSTRAINT [FK_TrashMessage_Mailbox_MailboxId] FOREIGN KEY ([MailboxId]) REFERENCES [dbo].[Mailbox] ([MailboxId]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_TrashMessage_MailboxId]
    ON [dbo].[TrashMessage]([MailboxId] ASC);

