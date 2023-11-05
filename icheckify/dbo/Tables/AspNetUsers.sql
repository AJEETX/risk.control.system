CREATE TABLE [dbo].[AspNetUsers] (
    [Id]                             BIGINT             IDENTITY (1, 1) NOT NULL,
    [ProfilePictureUrl]              NVARCHAR (MAX)     NULL,
    [IsSuperAdmin]                   BIT                NOT NULL,
    [IsClientAdmin]                  BIT                NOT NULL,
    [IsVendorAdmin]                  BIT                NOT NULL,
    [ProfilePicture]                 VARBINARY (MAX)    NULL,
    [FirstName]                      NVARCHAR (MAX)     NOT NULL,
    [LastName]                       NVARCHAR (MAX)     NOT NULL,
    [PinCodeId]                      NVARCHAR (450)     NULL,
    [StateId]                        NVARCHAR (450)     NULL,
    [CountryId]                      NVARCHAR (450)     NULL,
    [DistrictId]                     NVARCHAR (450)     NULL,
    [Addressline]                    NVARCHAR (MAX)     NULL,
    [Created]                        DATETIME2 (7)      NOT NULL,
    [Updated]                        DATETIME2 (7)      NULL,
    [UpdatedBy]                      NVARCHAR (MAX)     NULL,
    [Password]                       NVARCHAR (MAX)     NOT NULL,
    [Active]                         BIT                NOT NULL,
    [Deleted]                        BIT                NOT NULL,
    [Discriminator]                  NVARCHAR (MAX)     NOT NULL,
    [ClientCompanyId]                NVARCHAR (450)     NULL,
    [Comments]                       NVARCHAR (MAX)     NULL,
    [VendorId]                       NVARCHAR (450)     NULL,
    [VendorApplicationUser_Comments] NVARCHAR (MAX)     NULL,
    [UserName]                       NVARCHAR (256)     NULL,
    [NormalizedUserName]             NVARCHAR (256)     NULL,
    [Email]                          NVARCHAR (256)     NULL,
    [NormalizedEmail]                NVARCHAR (256)     NULL,
    [EmailConfirmed]                 BIT                NOT NULL,
    [PasswordHash]                   NVARCHAR (MAX)     NULL,
    [SecurityStamp]                  NVARCHAR (MAX)     NULL,
    [ConcurrencyStamp]               NVARCHAR (MAX)     NULL,
    [PhoneNumber]                    NVARCHAR (MAX)     NULL,
    [PhoneNumberConfirmed]           BIT                NOT NULL,
    [TwoFactorEnabled]               BIT                NOT NULL,
    [LockoutEnd]                     DATETIMEOFFSET (7) NULL,
    [LockoutEnabled]                 BIT                NOT NULL,
    [AccessFailedCount]              INT                NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AspNetUsers_ClientCompany_ClientCompanyId] FOREIGN KEY ([ClientCompanyId]) REFERENCES [dbo].[ClientCompany] ([ClientCompanyId]),
    CONSTRAINT [FK_AspNetUsers_Country_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [dbo].[Country] ([CountryId]),
    CONSTRAINT [FK_AspNetUsers_District_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [dbo].[District] ([DistrictId]),
    CONSTRAINT [FK_AspNetUsers_PinCode_PinCodeId] FOREIGN KEY ([PinCodeId]) REFERENCES [dbo].[PinCode] ([PinCodeId]),
    CONSTRAINT [FK_AspNetUsers_State_StateId] FOREIGN KEY ([StateId]) REFERENCES [dbo].[State] ([StateId]),
    CONSTRAINT [FK_AspNetUsers_Vendor_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Vendor] ([VendorId])
);


GO
CREATE NONCLUSTERED INDEX [EmailIndex]
    ON [dbo].[AspNetUsers]([NormalizedEmail] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_AspNetUsers_ClientCompanyId]
    ON [dbo].[AspNetUsers]([ClientCompanyId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_AspNetUsers_CountryId]
    ON [dbo].[AspNetUsers]([CountryId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_AspNetUsers_DistrictId]
    ON [dbo].[AspNetUsers]([DistrictId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_AspNetUsers_PinCodeId]
    ON [dbo].[AspNetUsers]([PinCodeId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_AspNetUsers_StateId]
    ON [dbo].[AspNetUsers]([StateId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_AspNetUsers_VendorId]
    ON [dbo].[AspNetUsers]([VendorId] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex]
    ON [dbo].[AspNetUsers]([NormalizedUserName] ASC) WHERE ([NormalizedUserName] IS NOT NULL);

