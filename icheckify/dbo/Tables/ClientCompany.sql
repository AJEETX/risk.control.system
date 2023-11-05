CREATE TABLE [dbo].[ClientCompany] (
    [ClientCompanyId]   NVARCHAR (450)  NOT NULL,
    [Name]              NVARCHAR (MAX)  NOT NULL,
    [Code]              NVARCHAR (MAX)  NOT NULL,
    [Description]       NVARCHAR (MAX)  NOT NULL,
    [PhoneNumber]       NVARCHAR (MAX)  NOT NULL,
    [Email]             NVARCHAR (MAX)  NOT NULL,
    [Branch]            NVARCHAR (MAX)  NOT NULL,
    [Addressline]       NVARCHAR (MAX)  NOT NULL,
    [StateId]           NVARCHAR (450)  NULL,
    [CountryId]         NVARCHAR (450)  NULL,
    [PinCodeId]         NVARCHAR (450)  NULL,
    [DistrictId]        NVARCHAR (450)  NULL,
    [BankName]          NVARCHAR (MAX)  NOT NULL,
    [BankAccountNumber] NVARCHAR (MAX)  NOT NULL,
    [IFSCCode]          NVARCHAR (MAX)  NOT NULL,
    [AgreementDate]     DATETIME2 (7)   NULL,
    [ActivatedDate]     DATETIME2 (7)   NULL,
    [Status]            INT             NULL,
    [DocumentUrl]       NVARCHAR (MAX)  NULL,
    [DocumentImage]     VARBINARY (MAX) NULL,
    [Auto]              BIT             NOT NULL,
    [Created]           DATETIME2 (7)   NOT NULL,
    [Updated]           DATETIME2 (7)   NULL,
    [UpdatedBy]         NVARCHAR (MAX)  NULL,
    CONSTRAINT [PK_ClientCompany] PRIMARY KEY CLUSTERED ([ClientCompanyId] ASC),
    CONSTRAINT [FK_ClientCompany_Country_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [dbo].[Country] ([CountryId]),
    CONSTRAINT [FK_ClientCompany_District_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [dbo].[District] ([DistrictId]),
    CONSTRAINT [FK_ClientCompany_PinCode_PinCodeId] FOREIGN KEY ([PinCodeId]) REFERENCES [dbo].[PinCode] ([PinCodeId]),
    CONSTRAINT [FK_ClientCompany_State_StateId] FOREIGN KEY ([StateId]) REFERENCES [dbo].[State] ([StateId])
);


GO
CREATE NONCLUSTERED INDEX [IX_ClientCompany_CountryId]
    ON [dbo].[ClientCompany]([CountryId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ClientCompany_DistrictId]
    ON [dbo].[ClientCompany]([DistrictId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ClientCompany_PinCodeId]
    ON [dbo].[ClientCompany]([PinCodeId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ClientCompany_StateId]
    ON [dbo].[ClientCompany]([StateId] ASC);

