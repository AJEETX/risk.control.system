CREATE TABLE [dbo].[ClaimMessage] (
    [ClaimMessageId]        NVARCHAR (450) NOT NULL,
    [SenderEmail]           NVARCHAR (MAX) NULL,
    [RecepicientEmail]      NVARCHAR (MAX) NULL,
    [Message]               NVARCHAR (MAX) NULL,
    [ClaimsInvestigationId] NVARCHAR (450) NULL,
    CONSTRAINT [PK_ClaimMessage] PRIMARY KEY CLUSTERED ([ClaimMessageId] ASC),
    CONSTRAINT [FK_ClaimMessage_ClaimsInvestigation_ClaimsInvestigationId] FOREIGN KEY ([ClaimsInvestigationId]) REFERENCES [dbo].[ClaimsInvestigation] ([ClaimsInvestigationId])
);


GO
CREATE NONCLUSTERED INDEX [IX_ClaimMessage_ClaimsInvestigationId]
    ON [dbo].[ClaimMessage]([ClaimsInvestigationId] ASC);

