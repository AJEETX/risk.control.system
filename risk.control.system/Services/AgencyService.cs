using System.Net;

using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IAgencyService
    {
        Task<bool> CreateAgency(Vendor vendor, string currentUserEmail, string domainAddress, string portal_base_url);
        Task<bool> EditAgency(Vendor vendor, string currentUserEmail, string portal_base_url);
    }
    internal class AgencyService : IAgencyService
    {
        private readonly ApplicationDbContext context;
        private readonly ISmsService smsService;
        private readonly IFileStorageService fileStorageService;
        private readonly IFeatureManager featureManager;

        public AgencyService(ApplicationDbContext context, ISmsService SmsService, IFileStorageService fileStorageService, IFeatureManager featureManager)
        {
            this.context = context;
            smsService = SmsService;
            this.fileStorageService = fileStorageService;
            this.featureManager = featureManager;
        }

        public async Task<bool> CreateAgency(Vendor vendor, string currentUserEmail, string domainAddress, string portal_base_url)
        {
            Domain domainData = (Domain)Enum.Parse(typeof(Domain), domainAddress, true);

            vendor.Email = vendor.Email.ToLower() + domainData.GetEnumDisplayName();

            if (vendor.Document is not null)
            {
                var (fileName, relativePath) = await fileStorageService.SaveAsync(vendor.Document, vendor.Email);

                using var dataStream = new MemoryStream();
                vendor.Document.CopyTo(dataStream);
                vendor.DocumentImage = dataStream.ToArray();

                vendor.DocumentImageExtension = Path.GetExtension(fileName);
                vendor.DocumentUrl = relativePath;
            }
            vendor.Status = VendorStatus.ACTIVE;
            vendor.AgreementDate = DateTime.Now;
            vendor.ActivatedDate = DateTime.Now;
            vendor.DomainName = domainData;
            vendor.BankName = WebUtility.HtmlEncode(vendor.BankName.ToUpper());
            vendor.IFSCCode = WebUtility.HtmlEncode(vendor.IFSCCode.ToUpper());
            vendor.PhoneNumber = WebUtility.HtmlEncode(vendor.PhoneNumber.TrimStart('0'));
            vendor.Updated = DateTime.Now;
            vendor.UpdatedBy = currentUserEmail;
            vendor.CreatedUser = currentUserEmail;
            vendor.PinCodeId = vendor.SelectedPincodeId;
            vendor.DistrictId = vendor.SelectedDistrictId;
            vendor.StateId = vendor.SelectedStateId;
            vendor.CountryId = vendor.SelectedCountryId;

            var pinCode = await context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefaultAsync(s => s.PinCodeId == vendor.SelectedPincodeId);

            context.Add(vendor);

            var managerRole = await context.ApplicationRole.FirstOrDefaultAsync(r => r.Name.Contains(AppRoles.MANAGER.ToString()));
            var companyUser = await context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefaultAsync(c => c.Email == currentUserEmail);

            var notification = new StatusNotification
            {
                Role = managerRole,
                Company = companyUser.ClientCompany,
                Symbol = "far fa-hand-point-right i-orangered",
                Message = $"Agency {vendor.Email} created",
                Status = "",
                NotifierUserEmail = currentUserEmail
            };
            context.Notifications.Add(notification);
            var rowsAffected = await context.SaveChangesAsync();
            if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
            {
                await smsService.DoSendSmsAsync(pinCode.Country.Code, pinCode.Country.ISDCode + vendor.PhoneNumber, "Agency created. \nDomain : " + vendor.Email + "\n" + portal_base_url);
            }

            return rowsAffected > 0;
        }

        public async Task<bool> EditAgency(Vendor vendor, string currentUserEmail, string portal_base_url)
        {
            if (vendor.Document is not null)
            {
                var (fileName, relativePath) = await fileStorageService.SaveAsync(vendor.Document, vendor.Email);

                using var dataStream = new MemoryStream();
                vendor.Document.CopyTo(dataStream);
                vendor.DocumentImage = dataStream.ToArray();

                vendor.DocumentImageExtension = Path.GetExtension(fileName);
                vendor.DocumentUrl = relativePath;
            }
            else
            {
                var vendorUser = await context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == currentUserEmail);
                var existingVendor = await context.Vendor.AsNoTracking().FirstOrDefaultAsync(c => c.VendorId == vendorUser.VendorId);
                if (existingVendor.DocumentImage != null || existingVendor.DocumentUrl != null)
                {
                    vendor.DocumentImage = existingVendor.DocumentImage;
                    vendor.DocumentUrl = existingVendor.DocumentUrl;
                }
            }
            vendor.PinCodeId = vendor.SelectedPincodeId;
            vendor.DistrictId = vendor.SelectedDistrictId;
            vendor.StateId = vendor.SelectedStateId;
            vendor.CountryId = vendor.SelectedCountryId;
            vendor.BankName = WebUtility.HtmlEncode(vendor.BankName.ToUpper());
            vendor.IFSCCode = WebUtility.HtmlEncode(vendor.IFSCCode.ToUpper());
            vendor.PhoneNumber = WebUtility.HtmlEncode(vendor.PhoneNumber.TrimStart('0'));
            var pinCode = await context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefaultAsync(s => s.PinCodeId == vendor.SelectedPincodeId);
            vendor.IsUpdated = true;
            vendor.Updated = DateTime.Now;
            vendor.UpdatedBy = currentUserEmail;
            context.Vendor.Update(vendor);
            var rowsAffected = await context.SaveChangesAsync();
            if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
            {
                await smsService.DoSendSmsAsync(pinCode.Country.Code, pinCode.Country.ISDCode + vendor.PhoneNumber, "Agency edited. \n\nDomain : " + vendor.Email + "\n" + portal_base_url);
            }
            return rowsAffected > 0;
        }
    }
}
