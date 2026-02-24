using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Company
{
    public interface IManageCompanyUserService
    {
        Task<ApplicationUser> GetUserCreationModelAsync(string currentUserEmail);

        Task<ApplicationUser> GetUserForEditAsync(long userId);

        Task<ServiceResult> CreateUserAsync(ApplicationUser model, string emailSuffix, string currentUserEmail, string baseUrl);

        Task<ServiceResult> UpdateUserAsync(string userId, ApplicationUser model, string currentUserEmail, string baseUrl);

        Task LoadModelAsync(ApplicationUser model, string currentUserEmail);
    }

    public class ManageCompanyUserService : IManageCompanyUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICompanyUserService _companyUserService; // Internal logic service
        private readonly IFeatureManager _featureManager;

        public ManageCompanyUserService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICompanyUserService companyUserService,
            IFeatureManager featureManager)
        {
            _context = context;
            _userManager = userManager;
            _companyUserService = companyUserService;
            _featureManager = featureManager;
        }

        public async Task<ApplicationUser> GetUserCreationModelAsync(string currentUserEmail)
        {
            var companyUser = await GetCurrentUserWithCompany(currentUserEmail);
            if (companyUser?.ClientCompany == null) return null;

            var availableRoles = await GetAvailableRoles(companyUser.ClientCompanyId.Value, null);

            return new ApplicationUser
            {
                Country = companyUser.ClientCompany.Country,
                ClientCompany = companyUser.ClientCompany,
                CountryId = companyUser.ClientCompany.CountryId,
                AvailableRoles = availableRoles
            };
        }

        public async Task<ApplicationUser> GetUserForEditAsync(long id)
        {
            var companyUser = await _context.ApplicationUser.AsNoTracking()
                .Include(u => u.Country)
                .Include(u => u.ClientCompany)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (companyUser == null) return null;

            // Determine if password change logic is required based on feature flags
            bool isFirstLoginEnabled = await _featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION);
            companyUser.IsPasswordChangeRequired = isFirstLoginEnabled ? !companyUser.IsPasswordChangeRequired : true;

            // Load roles, ensuring we don't count the current user as the "taken" manager
            companyUser.AvailableRoles = await GetAvailableRoles(companyUser.ClientCompanyId.Value, companyUser.Id);

            return companyUser;
        }

        public async Task<ServiceResult> UpdateUserAsync(string id, ApplicationUser model, string currentUserEmail, string baseUrl)
        {
            // This calls your existing lower-level logic service
            var result = await _companyUserService.UpdateAsync(id, model, currentUserEmail, baseUrl);

            return result;
        }

        public async Task LoadModelAsync(ApplicationUser model, string currentUserEmail)
        {
            var companyUser = await GetCurrentUserWithCompany(currentUserEmail);
            var company = companyUser.ClientCompany;

            model.ClientCompany = company;
            model.Country = company.Country;
            model.CountryId = company.CountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            model.AvailableRoles = await GetAvailableRoles(company.ClientCompanyId, model.Id);
        }

        private async Task<List<SelectListItem>> GetAvailableRoles(long companyId, long? editingUserId)
        {
            var usersInCompany = _context.ApplicationUser.AsNoTracking()
                .Where(c => !c.Deleted && c.ClientCompanyId == companyId && c.Id != (editingUserId ?? 0));

            bool isManagerTaken = false;
            foreach (var user in usersInCompany)
            {
                if (await _userManager.IsInRoleAsync(user, "Manager")) // Simplified check
                {
                    isManagerTaken = true;
                    break;
                }
            }

            return RoleGroups.CompanyAppRoles
                .Where(r => r != AppRoles.COMPANY_ADMIN && (r != AppRoles.MANAGER || !isManagerTaken))
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType().GetMember(r.ToString()).First().GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                }).ToList();
        }

        private async Task<ApplicationUser> GetCurrentUserWithCompany(string email)
        {
            return await _context.ApplicationUser.AsNoTracking()
                .Include(u => u.ClientCompany)
                .ThenInclude(c => c.Country)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<ServiceResult> CreateUserAsync(ApplicationUser model, string emailSuffix, string currentUserEmail, string baseUrl)
        {
            var result = await _companyUserService.CreateAsync(model, emailSuffix, currentUserEmail, baseUrl);

            return result;
        }

        // Implement CreateUserAsync, UpdateUserAsync, and DeleteUserAsync similarly...
    }
}