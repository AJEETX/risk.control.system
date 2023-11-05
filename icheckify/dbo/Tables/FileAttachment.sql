CREATE TABLE [dbo].[FileAttachment] (
    [FileAttachmentId]      NVARCHAR (450)  NOT NULL,
    [Name]                  NVARCHAR (MAX)  NOT NULL,
    [AttachedDocument]      VARBINARY (MAX) NULL,
    [ContactMessageId]      NVARCHAR (MAX)  NULL,
    [ClaimsInvestigationId] NVARCHAR (450)  NULL,
    CONSTRAINT [PK_FileAttachment] PRIMARY KEY CLUSTERED ([FileAttachmentId] ASC),
    CONSTRAINT [FK_FileAttachment_ClaimsInvestigation_ClaimsInvestigationId] FOREIGN KEY ([ClaimsInvestigationId]) REFERENCES [dbo].[ClaimsInvestigation] ([ClaimsInvestigationId])
);


GO
CREATE NONCLUSTERED INDEX [IX_FileAttachment_ClaimsInvestigationId]
    ON [dbo].[FileAttachment]([ClaimsInvestigationId] ASC);

