CREATE TABLE [dbo].[ClaimNote] (
    [ClaimNoteId]                NVARCHAR (450) NOT NULL,
    [Sender]                     NVARCHAR (MAX) NOT NULL,
    [Comment]                    NVARCHAR (MAX) NOT NULL,
    [ParentClaimNoteClaimNoteId] NVARCHAR (450) NULL,
    [ClaimsInvestigationId]      NVARCHAR (450) NULL,
    [Created]                    DATETIME2 (7)  NOT NULL,
    [Updated]                    DATETIME2 (7)  NULL,
    [UpdatedBy]                  NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_ClaimNote] PRIMARY KEY CLUSTERED ([ClaimNoteId] ASC),
    CONSTRAINT [FK_ClaimNote_ClaimNote_ParentClaimNoteClaimNoteId] FOREIGN KEY ([ParentClaimNoteClaimNoteId]) REFERENCES [dbo].[ClaimNote] ([ClaimNoteId]),
    CONSTRAINT [FK_ClaimNote_ClaimsInvestigation_ClaimsInvestigationId] FOREIGN KEY ([ClaimsInvestigationId]) REFERENCES [dbo].[ClaimsInvestigation] ([ClaimsInvestigationId])
);


GO
CREATE NONCLUSTERED INDEX [IX_ClaimNote_ClaimsInvestigationId]
    ON [dbo].[ClaimNote]([ClaimsInvestigationId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ClaimNote_ParentClaimNoteClaimNoteId]
    ON [dbo].[ClaimNote]([ParentClaimNoteClaimNoteId] ASC);

