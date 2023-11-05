CREATE TABLE [dbo].[ServicedPinCode] (
    [ServicedPinCodeId]                NVARCHAR (450) NOT NULL,
    [Name]                             NVARCHAR (MAX) NOT NULL,
    [Pincode]                          NVARCHAR (MAX) NOT NULL,
    [VendorInvestigationServiceTypeId] NVARCHAR (450) NOT NULL,
    [Created]                          DATETIME2 (7)  NOT NULL,
    [Updated]                          DATETIME2 (7)  NULL,
    [UpdatedBy]                        NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_ServicedPinCode] PRIMARY KEY CLUSTERED ([ServicedPinCodeId] ASC),
    CONSTRAINT [FK_ServicedPinCode_VendorInvestigationServiceType_VendorInvestigationServiceTypeId] FOREIGN KEY ([VendorInvestigationServiceTypeId]) REFERENCES [dbo].[VendorInvestigationServiceType] ([VendorInvestigationServiceTypeId]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_ServicedPinCode_VendorInvestigationServiceTypeId]
    ON [dbo].[ServicedPinCode]([VendorInvestigationServiceTypeId] ASC);

