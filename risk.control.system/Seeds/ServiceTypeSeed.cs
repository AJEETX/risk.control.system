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
           
            #region INVESTIGATION SERVICE TYPES

            var claimComprehensive = new InvestigationServiceType
            {
                Name = "COMPREHENSIVE",
                Code = "COMP",
                MasterData = true,
                Updated = DateTime.Now,
                InsuranceType = InsuranceType.CLAIM
            };

            var claimComprehensiveService = await context.InvestigationServiceType.AddAsync(claimComprehensive);

            var claimNonComprehensive = new InvestigationServiceType
            {
                Name = "STANDARD",
                Code = "NON-COMP",
                InsuranceType = InsuranceType.CLAIM,
                MasterData = true,
                Updated = DateTime.Now
            };

            var claimNonComprehensiveService = await context.InvestigationServiceType.AddAsync(claimNonComprehensive);

            var claimDocumentCollection = new InvestigationServiceType
            {
                Name = "COLLECTION",
                Code = "DOC",
                InsuranceType = InsuranceType.CLAIM,
                MasterData = true,
                Updated = DateTime.Now
            };

            var claimDocumentCollectionService = await context.InvestigationServiceType.AddAsync(claimDocumentCollection);

            var claimDiscreet = new InvestigationServiceType
            {
                Name = "DISCREET",
                Code = "DISCREET",
                MasterData = true,
                InsuranceType = InsuranceType.CLAIM,
                Updated = DateTime.Now
            };

            var claimDiscreetService = await context.InvestigationServiceType.AddAsync(claimDiscreet);

            var underWritingPreVerification = new InvestigationServiceType
            {
                Name = "PRE-BOARD",
                Code = "PRE-OV",
                InsuranceType = InsuranceType.UNDERWRITING,
                MasterData = true,
                Updated = DateTime.Now
            };

            var underWritingPreVerificationService = await context.InvestigationServiceType.AddAsync(underWritingPreVerification);

            var underWritingPostVerification = new InvestigationServiceType
            {
                Name = "POST-BOARD",
                Code = "POST-OV",
                InsuranceType = InsuranceType.UNDERWRITING,
                MasterData = true
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
