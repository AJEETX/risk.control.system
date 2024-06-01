using AspNetCoreHero.ToastNotification.Notyf;

using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ICreatorService
    {
        ClaimTransactionModel Create(string currentUserEmail);
    }
    public class CreatorService : ICreatorService
    {
        private readonly ApplicationDbContext context;

        public CreatorService(ApplicationDbContext context)
        {
            this.context = context;
        }
        public ClaimTransactionModel Create(string currentUserEmail)
        {
            var claim = new ClaimsInvestigation
            {
                PolicyDetail = new PolicyDetail
                {
                    LineOfBusinessId = context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == "claims").LineOfBusinessId
                }
            };
            var companyUser = context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(c => c.Email == currentUserEmail);
            bool userCanCreate = true;
            int availableCount = 0;
            var trial = companyUser.ClientCompany.LicenseType == Standard.Licensing.LicenseType.Trial;
            if (trial)
            {
                var totalClaimsCreated = context.ClaimsInvestigation.Include(c => c.PolicyDetail).Where(c => !c.Deleted && 
                    c.ClientCompanyId == companyUser.ClientCompanyId)?.ToList();
                availableCount = companyUser.ClientCompany.TotalCreatedClaimAllowed - totalClaimsCreated.Count;

                if (totalClaimsCreated?.Count >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                {
                    userCanCreate = false;
                }
            }
            var model = new ClaimTransactionModel
            {
                ClaimsInvestigation = claim,
                Log = null,
                AllowedToCreate = userCanCreate,
                AutoAllocation = companyUser.ClientCompany.AutoAllocation,
                Location = new BeneficiaryDetail { },
                AvailableCount = availableCount,
                TotalCount = companyUser.ClientCompany.TotalCreatedClaimAllowed,
                Trial = trial
            };
            return model;
        }
    }
}
