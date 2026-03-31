using System.Globalization;
using System.Net;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agency
{
    public interface IAgencyService
    {
        Task<bool> CreateAgency(Vendor vendor, string userEmail, string domainAddress, string portal_base_url);

        Task<bool> EditAgency(Vendor vendor, string userEmail, string portal_base_url);
    }

    internal class AgencyService : IAgencyService
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IFileStorageService _fileStorageService;

        public AgencyService(ApplicationDbContext context,
            RoleManager<ApplicationRole> roleManager,
            IFileStorageService fileStorageService)
        {
            _context = context;
            _roleManager = roleManager;
            _fileStorageService = fileStorageService;
        }

        public async Task<bool> CreateAgency(Vendor vendor, string userEmail, string domainAddress, string portal_base_url)
        {
            Domain domainData = (Domain)Enum.Parse(typeof(Domain), domainAddress, true);
            vendor.Email = vendor.Email.ToLower() + domainData.GetEnumDisplayName();
            var companyExist = await _context.ClientCompany.AsNoTracking().AnyAsync(u => u.Email.Trim().ToLower() == vendor.Email && !u.Deleted);
            if (companyExist)
            {
                return false;
            }
            var agencyExist = await _context.Vendor.AsNoTracking().AnyAsync(u => u.Email.Trim().ToLower() == vendor.Email);
            if (agencyExist)
            {
                return false;
            }
            if (vendor.Document is not null)
            {
                var (fileName, relativePath) = await _fileStorageService.SaveAsync(vendor.Document, vendor.Email);
                vendor.DocumentImageExtension = Path.GetExtension(fileName);
                vendor.DocumentUrl = relativePath;
            }
            UpdateAgency(vendor, domainData, userEmail);
            _context.Vendor.Add(vendor);
            var managerRole = await _roleManager.FindByNameAsync(MANAGER.DISPLAY_NAME);
            var companyUser = await _context.ApplicationUser.Include(c => c.ClientCompany).FirstOrDefaultAsync(c => c.Email == userEmail);
            var notification = new StatusNotification
            {
                RoleId = managerRole!.Id,
                ClientCompanyId = companyUser!.ClientCompanyId,
                Symbol = "far fa-hand-point-right i-green",
                Message = $"Agency {vendor.Email}",
                Status = "Created",
                NotifierUserEmail = userEmail
            };
            _context.Notifications.Add(notification);
            var rowsAffected = await _context.SaveChangesAsync();
            return rowsAffected > 0;
        }
        private void UpdateAgency(Vendor vendor, Domain domainData, string userEmail)
        {
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            vendor.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(vendor.Name.ToLower()));
            vendor.Status = VendorStatus.ACTIVE;
            vendor.AgreementDate = DateTime.UtcNow;
            vendor.ActivatedDate = DateTime.UtcNow;
            vendor.DomainName = domainData;
            vendor.BankName = WebUtility.HtmlEncode(vendor.BankName!.ToUpper());
            vendor.IFSCCode = WebUtility.HtmlEncode(vendor.IFSCCode!.ToUpper());
            vendor.Updated = DateTime.UtcNow;
            vendor.UpdatedBy = userEmail;
            vendor.CreatedUser = userEmail;
            vendor.PinCodeId = vendor.SelectedPincodeId;
            vendor.DistrictId = vendor.SelectedDistrictId;
            vendor.StateId = vendor.SelectedStateId;
            vendor.CountryId = vendor.SelectedCountryId;
            vendor.IsUpdated = true;
        }
        public async Task<bool> EditAgency(Vendor vendor, string userEmail, string portal_base_url)
        {
            if (vendor.Document is not null)
            {
                var (fileName, relativePath) = await _fileStorageService.SaveAsync(vendor.Document, vendor.Email);
                vendor.DocumentImageExtension = Path.GetExtension(fileName);
                vendor.DocumentUrl = relativePath;
            }
            else
            {
                var existingVendor = await _context.Vendor.AsNoTracking().FirstOrDefaultAsync(c => c.VendorId == vendor.VendorId);
                if (existingVendor != null && existingVendor!.DocumentUrl != null)
                {
                    vendor.DocumentImageExtension = existingVendor.DocumentImageExtension;
                    vendor.DocumentUrl = existingVendor.DocumentUrl;
                }
            }
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            vendor.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(vendor.Name.ToLower()));
            vendor.PinCodeId = vendor.SelectedPincodeId;
            vendor.DistrictId = vendor.SelectedDistrictId;
            vendor.StateId = vendor.SelectedStateId;
            vendor.CountryId = vendor.SelectedCountryId;
            vendor.BankName = WebUtility.HtmlEncode(vendor.BankName!.ToUpper());
            vendor.IFSCCode = WebUtility.HtmlEncode(vendor.IFSCCode!.ToUpper());
            vendor.IsUpdated = true;
            vendor.Updated = DateTime.UtcNow;
            vendor.UpdatedBy = userEmail;
            _context.Vendor.Update(vendor);
            var rowsAffected = await _context.SaveChangesAsync();
            return rowsAffected > 0;
        }
    }
}