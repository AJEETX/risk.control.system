using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Seeds
{
    public static class Insurer
    {
        private const string companyMapSize = "800x800";

        public static async Task<ClientCompany> Seed(ApplicationDbContext context, List<Vendor> vendors, IWebHostEnvironment webHostEnvironment,
                    ICustomApiClient customApiCLient, UserManager<ApplicationUser> userManager, SeedInput input, IFileStorageService fileStorageService)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var globalSettings = await context.GlobalSettings.FirstOrDefaultAsync();

            var companyPinCode = await context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefaultAsync(s => s.Country.Code == input.COUNTRY && s.Code == input.PINCODE);

            var companyAddress = input.ADDRESSLINE + ", " + companyPinCode.District.Name + ", " + companyPinCode.State.Name + ", " + companyPinCode.Country.Code;
            var companyAddressCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(companyAddress);
            var companyAddressCoordinatesLatLong = companyAddressCoordinates.Latitude + "," + companyAddressCoordinates.Longitude;
            var companyAddressUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={companyAddressCoordinatesLatLong}&zoom=14&size={companyMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{companyAddressCoordinatesLatLong}&key={EnvHelper.Get("GOOGLE_MAP_KEY")}";

            //CREATE COMPANY1
            string insurerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(input.PHOTO));
            var insurerImage = File.ReadAllBytes(insurerImagePath);

            if (insurerImage == null)
            {
                insurerImage = File.ReadAllBytes(noCompanyImagePath);
            }
            var extension = Path.GetExtension(insurerImagePath);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(insurerImage, extension, input.DOMAIN);
            vendors = vendors.Where(v => v.CountryId == companyPinCode.CountryId).ToList();

            var insurer = new ClientCompany
            {
                Name = input.NAME,
                Addressline = input.ADDRESSLINE,
                Branch = input.BRANCH,
                ActivatedDate = DateTime.UtcNow,
                AgreementDate = DateTime.UtcNow,
                BankName = input.BANK,
                BankAccountNumber = "1234567890",
                IFSCCode = input.IFSC,
                PinCode = companyPinCode,
                Country = companyPinCode.Country,
                CountryId = companyPinCode.CountryId,
                StateId = companyPinCode.StateId,
                DistrictId = companyPinCode.DistrictId,
                PinCodeId = companyPinCode.PinCodeId,
                //Description = "CORPORATE OFFICE ",
                Email = input.DOMAIN,
                DocumentUrl = relativePath,
                PhoneNumber = input.PHONE,
                ExpiryDate = DateTime.UtcNow.AddDays(10),
                EmpanelledVendors = vendors,
                Status = CompanyStatus.ACTIVE,
                AutoAllocation = globalSettings.AutoAllocation,
                BulkUpload = globalSettings.BulkUpload,
                Updated = DateTime.UtcNow,
                Deleted = false,
                VerifyPan = globalSettings.VerifyPan,
                VerifyPassport = globalSettings.VerifyPassport,
                EnableMedia = globalSettings.EnableMedia,
                PanIdfyUrl = globalSettings.PanIdfyUrl,
                AiEnabled = globalSettings.AiEnabled,
                CanChangePassword = globalSettings.CanChangePassword,
                EnablePassport = globalSettings.EnablePassport,
                HasSampleData = globalSettings.HasSampleData,
                PassportApiHost = globalSettings.PassportApiHost,
                PassportApiData = globalSettings.PassportApiData,
                PassportApiUrl = globalSettings.PassportApiUrl,
                PanAPIHost = globalSettings.PanAPIHost,
                PanAPIData = globalSettings.PanAPIData,
                UpdateAgentAnswer = globalSettings.UpdateAgentAnswer,
                AddressMapLocation = companyAddressUrl,
                AddressLatitude = companyAddressCoordinates.Latitude,
                AddressLongitude = companyAddressCoordinates.Longitude
            };

            var insurerCompany = await context.ClientCompany.AddAsync(insurer);

            await context.SaveChangesAsync(null, false);

            var creator = await ClientApplicationUserSeed.Seed(context, webHostEnvironment, userManager, insurerCompany.Entity, fileStorageService);

            var claimTemplate = ReportTemplateSeed.CLAIM(context, insurer);
            var underwriting = ReportTemplateSeed.UNDERWRITING(context, insurer);

            await context.SaveChangesAsync(null, false);

            return insurerCompany.Entity;
        }
    }
}