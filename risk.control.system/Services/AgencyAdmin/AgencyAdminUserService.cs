using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.AgencyAdmin
{
    public interface IAgencyAdminUserService
    {
        Task<ApplicationUser> PrepareCreateModelAsync(string currentUserEmail);

        Task<ApplicationUser> PrepareEditModelAsync(long id);

        Task LoadMetadataAsync(ApplicationUser model);

        Task<ApplicationUser> GetUserForDeleteAsync(long id);

        Task<bool> SoftDeleteUserAsync(string email, string deletedBy);
    }

    internal class AgencyAdminUserService : IAgencyAdminUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeatureManager _featureManager;

        public AgencyAdminUserService(ApplicationDbContext context, IFeatureManager featureManager)
        {
            _context = context;
            _featureManager = featureManager;
        }

        public async Task<ApplicationUser> PrepareCreateModelAsync(string currentUserEmail)
        {
            var vendorUser = await _context.ApplicationUser.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);

            if (vendorUser == null) return null;

            var vendor = await _context.Vendor.AsNoTracking()
                .Include(v => v.Country)
                .FirstOrDefaultAsync(v => v.VendorId == vendorUser.VendorId);

            if (vendor == null) return null;

            var model = new ApplicationUser
            {
                Country = vendor.Country,
                CountryId = vendor.CountryId,
                Vendor = vendor,
                VendorId = vendor.VendorId
            };

            await LoadMetadataAsync(model);
            return model;
        }

        public async Task<ApplicationUser> PrepareEditModelAsync(long id)
        {
            var user = await _context.ApplicationUser.AsNoTracking()
                .Include(u => u.Country)
                .Include(u => u.Vendor)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (user == null) return null;

            // Apply Feature Flag Logic
            bool forceChange = await _featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION);
            user.IsPasswordChangeRequired = forceChange ? !user.IsPasswordChangeRequired : true;

            await LoadMetadataAsync(user);
            return user;
        }

        public async Task LoadMetadataAsync(ApplicationUser model)
        {
            // Centralized Role Logic
            model.AvailableRoles = RoleGroups.AgencyAppRoles
                .Where(r => r != AppRoles.AGENCY_ADMIN)
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.GetType().GetMember(r.ToString()).First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? r.ToString()
                }).ToList();

            // Ensure Vendor/Country Data is present if missing
            if (model.Vendor == null && model.VendorId != null)
            {
                model.Vendor = await _context.Vendor.AsNoTracking()
                    .Include(v => v.Country)
                    .FirstOrDefaultAsync(v => v.VendorId == model.VendorId);

                if (model.Vendor != null)
                {
                    model.Country = model.Vendor.Country;
                    model.CountryId = model.Vendor.CountryId;
                }
            }

            model.StateId = model.SelectedStateId > 0 ? model.SelectedStateId : model.StateId;
            model.DistrictId = model.SelectedDistrictId > 0 ? model.SelectedDistrictId : model.DistrictId;
            model.PinCodeId = model.SelectedPincodeId > 0 ? model.SelectedPincodeId : model.PinCodeId;
        }

        public async Task<ApplicationUser> GetUserForDeleteAsync(long id)
        {
            var model = await _context.ApplicationUser.AsNoTracking()
                .Include(v => v.Country).Include(v => v.State)
                .Include(v => v.District).Include(v => v.PinCode)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (model == null) return null;

            var agencySubStatuses = new[] {
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR
        };

            model.HasClaims = await _context.Investigations.AsNoTracking()
                .AnyAsync(c => agencySubStatuses.Contains(c.SubStatus) && c.VendorId == model.VendorId);

            return model;
        }

        public async Task<bool> SoftDeleteUserAsync(string email, string deletedBy)
        {
            var user = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == email);
            if (user == null) return false;

            user.Updated = DateTime.UtcNow;
            user.UpdatedBy = deletedBy;
            user.Deleted = true;

            _context.ApplicationUser.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}