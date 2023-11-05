CREATE TABLE [dbo].[BeneficiaryRelation] (
    [BeneficiaryRelationId] BIGINT         IDENTITY (1, 1) NOT NULL,
    [Name]                  NVARCHAR (MAX) NOT NULL,
    [Code]                  NVARCHAR (MAX) NOT NULL,
    [Created]               DATETIME2 (7)  NOT NULL,
    [Updated]               DATETIME2 (7)  NULL,
    [UpdatedBy]             NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_BeneficiaryRelation] PRIMARY KEY CLUSTERED ([BeneficiaryRelationId] ASC)
);

