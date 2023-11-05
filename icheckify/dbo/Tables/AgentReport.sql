CREATE TABLE [dbo].[AgentReport] (
    [AgentReportId]           NVARCHAR (450)  NOT NULL,
    [AgentEmail]              NVARCHAR (MAX)  NULL,
    [AgentRemarksUpdated]     DATETIME2 (7)   NULL,
    [AgentRemarks]            NVARCHAR (MAX)  NULL,
    [AgentLocationPictureUrl] NVARCHAR (MAX)  NULL,
    [AgentLocationPicture]    VARBINARY (MAX) NULL,
    [AgentOcrUrl]             NVARCHAR (MAX)  NULL,
    [AgentOcrPicture]         VARBINARY (MAX) NULL,
    [AgentOcrData]            NVARCHAR (MAX)  NULL,
    [AgentQrUrl]              NVARCHAR (MAX)  NULL,
    [AgentQrPicture]          VARBINARY (MAX) NULL,
    [QrData]                  NVARCHAR (MAX)  NULL,
    [LongLat]                 NVARCHAR (MAX)  NULL,
    CONSTRAINT [PK_AgentReport] PRIMARY KEY CLUSTERED ([AgentReportId] ASC)
);

