using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Api;

namespace risk.control.system.Services.Creator
{
    public interface ILicenseService
    {
        Task<LicenseStatus> GetUploadPermissionsAsync(ApplicationUser user, bool isManager = false);
    }

    public class LicenseService(ApplicationDbContext context, IInvestigationService investigationService) : ILicenseService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IInvestigationService _investigationService = investigationService;

        public async Task<LicenseStatus> GetUploadPermissionsAsync(ApplicationUser user, bool isManager = false)
        {
            var company = user.ClientCompany;
            if (company!.LicenseType != LicenseType.Trial)
                return LicenseStatus.Unlimited();

            var totalReadyToAssign = await _investigationService.GetAutoCount(user.Email!);
            bool hasUploadFiles = false;
            if (!isManager)
            {
                hasUploadFiles = await _context.FilesOnFileSystem.AsNoTracking().AnyAsync(f => f.UploadedBy == user.Email && !f.Deleted);
            }
            else
            {
                hasUploadFiles = await _context.FilesOnFileSystem.AsNoTracking().AnyAsync(f => f.CompanyId == user.ClientCompanyId && !f.Deleted);
            }

            var totalClaimsCreated = await _context.Investigations
                .CountAsync(c => !c.Deleted && c.ClientCompanyId == company.ClientCompanyId);

            int available = company.TotalCreatedClaimAllowed - totalClaimsCreated;

            return new LicenseStatus
            {
                CanCreate = (available > 0) && (company.TotalToAssignMaxAllowed > totalReadyToAssign),
                HasClaimsPending = hasUploadFiles,
                AvailableCount = available,
                MaxAllowed = company.TotalCreatedClaimAllowed
            };
        }
    }
}