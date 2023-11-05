CREATE TABLE [dbo].[ClaimReport] (
    [ClaimReportId]            NVARCHAR (450)  NOT NULL,
    [VendorId]                 NVARCHAR (450)  NULL,
    [AgentEmail]               NVARCHAR (MAX)  NULL,
    [AgentRemarksUpdated]      DATETIME2 (7)   NULL,
    [AgentRemarks]             NVARCHAR (MAX)  NULL,
    [Question1]                NVARCHAR (MAX)  NULL,
    [Question2]                NVARCHAR (MAX)  NULL,
    [Question3]                NVARCHAR (MAX)  NULL,
    [Question4]                NVARCHAR (MAX)  NULL,
    [Question5]                NVARCHAR (MAX)  NULL,
    [AgentLocationPictureUrl]  NVARCHAR (MAX)  NULL,
    [AgentLocationPicture]     VARBINARY (MAX) NULL,
    [LocationData]             NVARCHAR (MAX)  NULL,
    [AgentOcrUrl]              NVARCHAR (MAX)  NULL,
    [AgentOcrPicture]          VARBINARY (MAX) NULL,
    [AgentOcrData]             NVARCHAR (MAX)  NULL,
    [AgentQrUrl]               NVARCHAR (MAX)  NULL,
    [AgentQrPicture]           VARBINARY (MAX) NULL,
    [QrData]                   NVARCHAR (MAX)  NULL,
    [LocationLongLat]          NVARCHAR (MAX)  NULL,
    [LocationLongLatTime]      DATETIME2 (7)   NULL,
    [OcrLongLat]               NVARCHAR (MAX)  NULL,
    [OcrLongLatTime]           DATETIME2 (7)   NULL,
    [AgentReportId]            NVARCHAR (450)  NULL,
    [SupervisorPicture]        VARBINARY (MAX) NULL,
    [SupervisorRemarksUpdated] DATETIME2 (7)   NULL,
    [SupervisorEmail]          NVARCHAR (MAX)  NULL,
    [SupervisorRemarks]        NVARCHAR (MAX)  NULL,
    [SupervisorRemarkType]     INT             NULL,
    [AssessorRemarksUpdated]   DATETIME2 (7)   NULL,
    [AssessorEmail]            NVARCHAR (MAX)  NULL,
    [AssessorRemarks]          NVARCHAR (MAX)  NULL,
    [AssessorRemarkType]       INT             NULL,
    [CaseLocationId]           BIGINT          NOT NULL,
    CONSTRAINT [PK_ClaimReport] PRIMARY KEY CLUSTERED ([ClaimReportId] ASC),
    CONSTRAINT [FK_ClaimReport_AgentReport_AgentReportId] FOREIGN KEY ([AgentReportId]) REFERENCES [dbo].[AgentReport] ([AgentReportId]),
    CONSTRAINT [FK_ClaimReport_CaseLocation_CaseLocationId] FOREIGN KEY ([CaseLocationId]) REFERENCES [dbo].[CaseLocation] ([CaseLocationId]) ON DELETE CASCADE,
    CONSTRAINT [FK_ClaimReport_Vendor_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Vendor] ([VendorId])
);


GO
CREATE NONCLUSTERED INDEX [IX_ClaimReport_AgentReportId]
    ON [dbo].[ClaimReport]([AgentReportId] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ClaimReport_CaseLocationId]
    ON [dbo].[ClaimReport]([CaseLocationId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ClaimReport_VendorId]
    ON [dbo].[ClaimReport]([VendorId] ASC);

