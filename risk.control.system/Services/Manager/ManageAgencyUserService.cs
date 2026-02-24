using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.AgencyAdmin;

namespace risk.control.system.Services.Manager
{
    public interface IManageAgencyUserService
    {
        Task<ApplicationUser> GetUserCreationModelAsync(long vendorId);

        Task<ApplicationUser> GetNewUserCreationModelAsync(long vendorId);

        Task<(bool Success, string Message, Dictionary<string, string> Errors)> CreateAgencyUserAsync(ModelStateDictionary ModelState, ApplicationUser model, string emailSuffix, string currentUserEmail, string baseUrl);

        Task<ApplicationUser> GetUserForEditAsync(long id);

        Task<(bool Success, string Message, Dictionary<string, string> Errors)> EditAgencyUserAsync(ModelStateDictionary ModelState, string id, ApplicationUser model, string currentUserEmail, string baseUrl);

        Task<ApplicationUser> GetUserForDeleteAsync(long id);

        Task<(bool, string)> SoftDeleteUserAsync(string email, string currentUserEmail);

        Task LoadModelAsync(ApplicationUser model);

        Task LoadEditModelAsync(ApplicationUser model);
    }

    public class ManageAgencyUserService : IManageAgencyUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeatureManager _featureManager;
        private readonly IAgencyUserCreateEditService _agencyUserCreateEditService;
        private readonly ILogger<ManageAgencyUserService> _logger;

        public ManageAgencyUserService(
            ApplicationDbContext context,
            IFeatureManager featureManager,
            IAgencyUserCreateEditService agencyUserCreateEditService,
            ILogger<ManageAgencyUserService> logger)
        {
            _context = context;
            _featureManager = featureManager;
            _agencyUserCreateEditService = agencyUserCreateEditService;
            _logger = logger;
        }

        public async Task<ApplicationUser> GetUserCreationModelAsync(long vendorId)
        {
            var vendor = await _context.Vendor.AsNoTracking()
                .Include(v => v.Country)
                .FirstOrDefaultAsync(v => v.VendorId == vendorId);

            if (vendor == null) return null;

            return new ApplicationUser
            {
                Country = vendor.Country,
                CountryId = vendor.CountryId,
                Vendor = vendor,
                VendorId = vendorId,
                AvailableRoles = GetAgencyRoles()
            };
        }

        public async Task<ApplicationUser> GetNewUserCreationModelAsync(long vendorId)
        {
            var vendor = await _context.Vendor.AsNoTracking()
                .Include(v => v.Country)
                .FirstOrDefaultAsync(v => v.VendorId == vendorId);

            if (vendor == null) return null;
            var (roles, role, status) = await GetNewAgencyUserRoles(vendorId);
            return new ApplicationUser
            {
                Country = vendor.Country,
                CountryId = vendor.CountryId,
                Vendor = vendor,
                VendorId = vendorId,
                AvailableRoles = roles,
                Active = status,
                Role = role
            };
        }

        public async Task<(bool Success, string Message, Dictionary<string, string> Errors)> CreateAgencyUserAsync(ModelStateDictionary ModelState, ApplicationUser model, string emailSuffix, string currentUserEmail, string baseUrl)
        {
            model.Id = 0; // Ensure new user
            var vendorUserModel = new CreateVendorUserRequest
            {
                User = model,
                EmailSuffix = emailSuffix,
                CreatedBy = currentUserEmail
            };

            // Note: Pass an empty/new ModelStateDictionary if the service requires it for internal validation
            return await _agencyUserCreateEditService.CreateVendorUserAsync(vendorUserModel, ModelState, baseUrl);
        }

        public async Task LoadModelAsync(ApplicationUser model)
        {
            var vendor = await _context.Vendor.AsNoTracking()
                .Include(v => v.Country)
                .FirstOrDefaultAsync(v => v.VendorId == model.VendorId);

            if (vendor != null)
            {
                model.Country = vendor.Country;
                model.CountryId = vendor.CountryId;
                model.Vendor = vendor;
            }
            model.StateId = model.SelectedStateId > 0 ? model.SelectedStateId : model.StateId;
            model.DistrictId = model.SelectedDistrictId > 0 ? model.SelectedDistrictId : model.StateId;
            model.PinCodeId = model.SelectedPincodeId > 0 ? model.SelectedPincodeId : model.PinCodeId;
            model.AvailableRoles = GetAgencyRoles();
        }

        public async Task LoadEditModelAsync(ApplicationUser model)
        {
            List<SelectListItem> allRoles = null;
            AppRoles? role = null;
            var vendor = await _context.Vendor.Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == model.VendorId);

            var currentVendorUserCount = await _context.ApplicationUser.CountAsync(v => v.VendorId == model.VendorId);
            bool status = false;
            if (currentVendorUserCount == 0)
            {
                role = AppRoles.AGENCY_ADMIN;
                status = true;
                allRoles = RoleGroups.AgencyAppRoles
                .Where(r => r == AppRoles.AGENCY_ADMIN) // Include ADMIN if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
            }
            else
            {
                allRoles = RoleGroups.AgencyAppRoles
                .Where(r => r != AppRoles.AGENCY_ADMIN) // Include ADMIN if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
            }
            model.Active = status;
            model.Vendor = vendor;
            model.Country = vendor.Country;
            model.CountryId = vendor.CountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            model.AvailableRoles = allRoles;
            model.Role = role;
        }

        public async Task<ApplicationUser> GetUserForEditAsync(long id)
        {
            var agencyUser = await _context.ApplicationUser
                .Include(u => u.Vendor)
                .Include(v => v.Country)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (agencyUser == null) return null;

            // Apply business logic for password change requirements
            bool isFirstLoginEnabled = await _featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION);
            agencyUser.IsPasswordChangeRequired = isFirstLoginEnabled ? !agencyUser.IsPasswordChangeRequired : true;

            // Populate SelectLists
            agencyUser.AvailableRoles = GetAgencyRoles();

            return agencyUser;
        }

        public async Task<(bool Success, string Message, Dictionary<string, string> Errors)> EditAgencyUserAsync(ModelStateDictionary ModelState, string id, ApplicationUser model, string currentUserEmail, string baseUrl)
        {
            var editRequest = new EditVendorUserRequest
            {
                UserId = id,
                Model = model,
                UpdatedBy = currentUserEmail
            };

            // Pass an empty ModelState if the underlying service uses it for internal tracking
            return await _agencyUserCreateEditService.EditVendorUserAsync(editRequest, ModelState, baseUrl);
        }

        public async Task<ApplicationUser> GetUserForDeleteAsync(long id)
        {
            var model = await _context.ApplicationUser
                .Include(v => v.Country)
                .Include(v => v.State)
                .Include(v => v.District)
                .Include(v => v.PinCode)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (model == null) return null;

            // Business Rule: Check for active sub-statuses before allowing deletion
            var agencySubStatuses = new[] {
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR
        };

            model.HasClaims = await _context.Investigations.AnyAsync(c =>
                agencySubStatuses.Contains(c.SubStatus) &&
                c.VendorId == model.VendorId);

            return model;
        }

        public async Task<(bool, string)> SoftDeleteUserAsync(string email, string currentUserEmail)
        {
            var user = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return new(false, "User not found.");

            try
            {
                user.Updated = DateTime.UtcNow;
                user.UpdatedBy = currentUserEmail;
                user.Deleted = true; // Soft delete

                _context.ApplicationUser.Update(user);
                await _context.SaveChangesAsync();

                return (true, $"User <b>{user.Email}</b> deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to soft delete user {Email}", email);
                return (false, "An error occurred while deleting the user.");
            }
        }

        private async Task<(List<SelectListItem>, AppRoles?, bool)> GetNewAgencyUserRoles(long id)
        {
            var currentVendorUserCount = await _context.ApplicationUser.CountAsync(v => v.VendorId == id);
            bool status = false;
            List<SelectListItem> allRoles = null;
            AppRoles? role = null;
            if (currentVendorUserCount == 0)
            {
                role = AppRoles.AGENCY_ADMIN;
                status = true;
                allRoles = RoleGroups.AgencyAppRoles
                .Where(r => r == AppRoles.AGENCY_ADMIN) // Include ADMIN if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
            }
            else
            {
                allRoles = RoleGroups.AgencyAppRoles
                .Where(r => r != AppRoles.AGENCY_ADMIN) // Exclude ADMIN if already taken
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
            }
            return (allRoles, role, status);
        }

        private List<SelectListItem> GetAgencyRoles()
        {
            return RoleGroups.AgencyAppRoles
                .Where(r => r != AppRoles.AGENCY_ADMIN)
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType()
                            .GetMember(r.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                })
                .ToList();
        }
    }
}