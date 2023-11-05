CREATE TABLE [dbo].[VerifyPinCode] (
    [VerifyPinCodeId] NVARCHAR (450) NOT NULL,
    [Name]            NVARCHAR (MAX) NOT NULL,
    [Pincode]         NVARCHAR (MAX) NOT NULL,
    [CaseLocationId]  BIGINT         NOT NULL,
    [Created]         DATETIME2 (7)  NOT NULL,
    [Updated]         DATETIME2 (7)  NULL,
    [UpdatedBy]       NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_VerifyPinCode] PRIMARY KEY CLUSTERED ([VerifyPinCodeId] ASC),
    CONSTRAINT [FK_VerifyPinCode_CaseLocation_CaseLocationId] FOREIGN KEY ([CaseLocationId]) REFERENCES [dbo].[CaseLocation] ([CaseLocationId]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_VerifyPinCode_CaseLocationId]
    ON [dbo].[VerifyPinCode]([CaseLocationId] ASC);

