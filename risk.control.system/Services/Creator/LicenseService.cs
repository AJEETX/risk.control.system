using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Api;

namespace risk.control.system.Services.Creator
{
    public interface ILicenseService
    {
        Task<LicenseStatus> GetUploadPermissionsAsync(ApplicationUser user);
    }

    public class LicenseService : ILicenseService
    {
        private readonly ApplicationDbContext context;
        private readonly IInvestigationService investigationService;

        public LicenseService(ApplicationDbContext context, IInvestigationService investigationService)
        {
            this.context = context;
            this.investigationService = investigationService;
        }

        public async Task<LicenseStatus> GetUploadPermissionsAsync(ApplicationUser user)
        {
            var company = user.ClientCompany;
            if (company.LicenseType != LicenseType.Trial)
                return LicenseStatus.Unlimited();

            var totalReadyToAssign = await investigationService.GetAutoCount(user.Email);
            var totalClaimsCreated = await context.Investigations
                .CountAsync(c => !c.Deleted && c.ClientCompanyId == company.ClientCompanyId);

            int available = company.TotalCreatedClaimAllowed - totalClaimsCreated;

            return new LicenseStatus
            {
                CanCreate = (available > 0) && (company.TotalToAssignMaxAllowed > totalReadyToAssign),
                HasClaimsPending = totalReadyToAssign > 0,
                AvailableCount = available,
                MaxAllowed = company.TotalCreatedClaimAllowed
            };
        }
    }
}