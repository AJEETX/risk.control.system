CREATE TABLE [dbo].[CustomerDetail] (
    [CustomerDetailId]    NVARCHAR (450)  NOT NULL,
    [CustomerName]        NVARCHAR (MAX)  NOT NULL,
    [CustomerDateOfBirth] DATETIME2 (7)   NOT NULL,
    [Gender]              INT             NULL,
    [ContactNumber]       BIGINT          NOT NULL,
    [Addressline]         NVARCHAR (MAX)  NOT NULL,
    [PinCodeId]           NVARCHAR (450)  NULL,
    [StateId]             NVARCHAR (450)  NULL,
    [CountryId]           NVARCHAR (450)  NULL,
    [DistrictId]          NVARCHAR (450)  NULL,
    [CustomerType]        INT             NOT NULL,
    [CustomerIncome]      INT             NOT NULL,
    [CustomerOccupation]  INT             NOT NULL,
    [CustomerEducation]   INT             NOT NULL,
    [ProfilePictureUrl]   NVARCHAR (MAX)  NULL,
    [ProfilePicture]      VARBINARY (MAX) NULL,
    [Description]         NVARCHAR (MAX)  NULL,
    CONSTRAINT [PK_CustomerDetail] PRIMARY KEY CLUSTERED ([CustomerDetailId] ASC),
    CONSTRAINT [FK_CustomerDetail_Country_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [dbo].[Country] ([CountryId]),
    CONSTRAINT [FK_CustomerDetail_District_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [dbo].[District] ([DistrictId]),
    CONSTRAINT [FK_CustomerDetail_PinCode_PinCodeId] FOREIGN KEY ([PinCodeId]) REFERENCES [dbo].[PinCode] ([PinCodeId]),
    CONSTRAINT [FK_CustomerDetail_State_StateId] FOREIGN KEY ([StateId]) REFERENCES [dbo].[State] ([StateId])
);


GO
CREATE NONCLUSTERED INDEX [IX_CustomerDetail_CountryId]
    ON [dbo].[CustomerDetail]([CountryId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_CustomerDetail_DistrictId]
    ON [dbo].[CustomerDetail]([DistrictId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_CustomerDetail_PinCodeId]
    ON [dbo].[CustomerDetail]([PinCodeId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_CustomerDetail_StateId]
    ON [dbo].[CustomerDetail]([StateId] ASC);

