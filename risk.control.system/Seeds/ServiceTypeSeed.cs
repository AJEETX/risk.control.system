using risk.control.system.Data;
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
                Code = "STD",
                InsuranceType = InsuranceType.CLAIM,
                MasterData = true,
                Updated = DateTime.Now
            };

            var claimNonComprehensiveService = await context.InvestigationServiceType.AddAsync(claimNonComprehensive);

            var underWritingPreVerification = new InvestigationServiceType
            {
                Name = "PRE-BOARD",
                Code = "PRE",
                InsuranceType = InsuranceType.UNDERWRITING,
                MasterData = true,
                Updated = DateTime.Now
            };

            var underWritingPreVerificationService = await context.InvestigationServiceType.AddAsync(underWritingPreVerification);

            var underWritingPostVerification = new InvestigationServiceType
            {
                Name = "POST-BOARD",
                Code = "POS",
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
                underWritingPreVerificationService.Entity,
                underWritingPostVerificationService.Entity
            };
            return investigationServiceTypes;
        }
    }
}
