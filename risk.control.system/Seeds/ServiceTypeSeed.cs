using Google.Api;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public static class ServiceTypeSeed
    {
        public static async Task<List<InvestigationServiceType>> Seed(ApplicationDbContext context)
        {
            var lobExist = context.LineOfBusiness.Any();

            if (lobExist)
            {
                return context.InvestigationServiceType.ToList();
            }
            #region LINE OF BUSINESS

            var claims = new LineOfBusiness
            {
                Name = "CLAIMS",
                Code = "CLAIMS",
                MasterData = true,
                Updated = DateTime.Now,
            };

            var claimCaseType = await context.LineOfBusiness.AddAsync(claims);

            var underwriting = new LineOfBusiness
            {
                Name = "UNDERWRITING",
                Code = "UNDERWRITING",
                MasterData = true,
                Updated = DateTime.Now,
            };

            var underwritingCaseType = await context.LineOfBusiness.AddAsync(underwriting);

            #endregion LINE OF BUSINESS

            #region INVESTIGATION SERVICE TYPES

            var claimComprehensive = new InvestigationServiceType
            {
                Name = "COMPREHENSIVE",
                Code = "COMP",
                MasterData = true,
                Updated = DateTime.Now,
                InsuranceType = InsuranceType.CLAIM,
                LineOfBusiness = claimCaseType.Entity,
            };

            var claimComprehensiveService = await context.InvestigationServiceType.AddAsync(claimComprehensive);

            var claimNonComprehensive = new InvestigationServiceType
            {
                Name = "STANDARD",
                Code = "NON-COMP",
                InsuranceType = InsuranceType.CLAIM,
                MasterData = true,
                Updated = DateTime.Now,
                LineOfBusiness = claimCaseType.Entity
            };

            var claimNonComprehensiveService = await context.InvestigationServiceType.AddAsync(claimNonComprehensive);

            var claimDocumentCollection = new InvestigationServiceType
            {
                Name = "COLLECTION",
                Code = "DOC",
                InsuranceType = InsuranceType.CLAIM,
                MasterData = true,
                Updated = DateTime.Now,
                LineOfBusiness = claimCaseType.Entity
            };

            var claimDocumentCollectionService = await context.InvestigationServiceType.AddAsync(claimDocumentCollection);

            var claimDiscreet = new InvestigationServiceType
            {
                Name = "DISCREET",
                Code = "DISCREET",
                MasterData = true,
                InsuranceType = InsuranceType.CLAIM,
                Updated = DateTime.Now,
                LineOfBusiness = claimCaseType.Entity
            };

            var claimDiscreetService = await context.InvestigationServiceType.AddAsync(claimDiscreet);

            var underWritingPreVerification = new InvestigationServiceType
            {
                Name = "PRE-BOARD",
                Code = "PRE-OV",
                InsuranceType = InsuranceType.UNDERWRITING,
                MasterData = true,
                Updated = DateTime.Now,
                LineOfBusiness = underwritingCaseType.Entity
            };

            var underWritingPreVerificationService = await context.InvestigationServiceType.AddAsync(underWritingPreVerification);

            var underWritingPostVerification = new InvestigationServiceType
            {
                Name = "POST-BOARD",
                Code = "POST-OV",
                InsuranceType = InsuranceType.UNDERWRITING,
                MasterData = true,
                LineOfBusiness = underwritingCaseType.Entity
            };

            var underWritingPostVerificationService = await context.InvestigationServiceType.AddAsync(underWritingPostVerification);

            #endregion INVESTIGATION SERVICE TYPES

            await context.SaveChangesAsync(null, false);

            var investigationServiceTypes = new List<InvestigationServiceType>
            {
                claimComprehensiveService.Entity,
                claimNonComprehensiveService.Entity,
                claimDocumentCollectionService.Entity,
                claimDiscreetService.Entity,
                underWritingPreVerificationService.Entity,
                underWritingPostVerificationService.Entity
            };
            return investigationServiceTypes;
        }
    }
}
