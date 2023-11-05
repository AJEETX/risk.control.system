CREATE TABLE [dbo].[DraftMessage] (
    [DraftMessageId]  BIGINT          IDENTITY (1, 1) NOT NULL,
    [SenderEmail]     NVARCHAR (MAX)  NOT NULL,
    [ReceipientEmail] NVARCHAR (MAX)  NOT NULL,
    [Subject]         NVARCHAR (MAX)  NOT NULL,
    [Message]         NVARCHAR (MAX)  NOT NULL,
    [Read]            BIT             NOT NULL,
    [Priority]        INT             NOT NULL,
    [SendDate]        DATETIME2 (7)   NULL,
    [ReceiveDate]     DATETIME2 (7)   NULL,
    [AttachmentName]  NVARCHAR (MAX)  NULL,
    [FileType]        NVARCHAR (MAX)  NULL,
    [Extension]       NVARCHAR (MAX)  NULL,
    [Attachment]      VARBINARY (MAX) NULL,
    [IsDraft]         BIT             NULL,
    [Trashed]         BIT             NULL,
    [DeleteTrashed]   BIT             NULL,
    [MessageStatus]   INT             NOT NULL,
    [MailboxId]       BIGINT          NOT NULL,
    [Created]         DATETIME2 (7)   NOT NULL,
    [Updated]         DATETIME2 (7)   NULL,
    [UpdatedBy]       NVARCHAR (MAX)  NULL,
    CONSTRAINT [PK_DraftMessage] PRIMARY KEY CLUSTERED ([DraftMessageId] ASC),
    CONSTRAINT [FK_DraftMessage_Mailbox_MailboxId] FOREIGN KEY ([MailboxId]) REFERENCES [dbo].[Mailbox] ([MailboxId]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_DraftMessage_MailboxId]
    ON [dbo].[DraftMessage]([MailboxId] ASC);

