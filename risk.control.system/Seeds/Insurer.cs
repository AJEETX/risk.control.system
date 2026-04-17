using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            string insurerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "seed", Path.GetFileName(input.PHOTO)!);
            var insurerImage = await File.ReadAllBytesAsync(insurerImagePath);
            var extension = Path.GetExtension(insurerImagePath);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(insurerImage, extension, input.DOMAIN!);
            var insurer = await GetCompany(context, input, vendors, customApiCLient, relativePath);
            var insurerCompany = await context.ClientCompany.AddAsync(insurer);
            await context.SaveChangesAsync(null, false);
            var creator = await InsurerUserSeed.Seed(context, webHostEnvironment, userManager, insurerCompany.Entity, fileStorageService);
            var claimTemplate = ReportTemplateSeed.CLAIM(context, insurer);
            var underwriting = ReportTemplateSeed.UNDERWRITING(context, insurer);
            await context.SaveChangesAsync(null, false);
            return insurerCompany.Entity;
        }

        private static async Task<ClientCompany> GetCompany(ApplicationDbContext context, SeedInput input, List<Vendor> vendors, ICustomApiClient customApiCLient, string relativePath)
        {
            var globalSettings = await context.GlobalSettings.FirstOrDefaultAsync();
            var companyPinCode = await context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefaultAsync(s => s.Country!.Code == input.COUNTRY && s.Code == input.PINCODE);
            var companyAddress = input.ADDRESSLINE + ", " + companyPinCode!.District!.Name + ", " + companyPinCode.State!.Name + ", " + companyPinCode.Country!.Code;
            var companyAddressCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(companyAddress);
            var companyAddressCoordinatesLatLong = companyAddressCoordinates.Latitude + "," + companyAddressCoordinates.Longitude;
            var companyAddressUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={companyAddressCoordinatesLatLong}&zoom=14&size={companyMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{companyAddressCoordinatesLatLong}&key={EnvHelper.Get("GOOGLE_MAP_KEY")}";
            return new ClientCompany
            {
                Name = input.NAME!,
                Addressline = input.ADDRESSLINE!,
                Branch = input.BRANCH,
                BankName = input.BANK,
                BankAccountNumber = "1234567890",
                IFSCCode = input.IFSC,
                PinCode = companyPinCode,
                Country = companyPinCode.Country,
                CountryId = companyPinCode.CountryId,
                StateId = companyPinCode.StateId,
                DistrictId = companyPinCode.DistrictId,
                PinCodeId = companyPinCode.PinCodeId,
                Email = input.DOMAIN!,
                DocumentUrl = relativePath,
                PhoneNumber = input.PHONE!,
                EmpanelledVendors = vendors.Where(v => v.CountryId == companyPinCode.CountryId).ToList(),
                Updated = DateTime.UtcNow,
                VerifyPan = globalSettings!.VerifyPan,
                VerifyPassport = globalSettings.VerifyPassport,
                EnableMedia = globalSettings.EnableMedia,
                PanIdfyUrl = globalSettings.PanIdfyUrl,
                AiEnabled = globalSettings.AiEnabled,
                EnablePassport = globalSettings.EnablePassport,
                HasSampleData = globalSettings.HasSampleData,
                PassportApiHost = globalSettings.PassportApiHost,
                PassportApiData = globalSettings.PassportApiData,
                PassportApiUrl = globalSettings.PassportApiUrl,
                PanAPIHost = globalSettings.PanAPIHost,
                PanAPIData = globalSettings.PanAPIData,
                AddressMapLocation = companyAddressUrl,
                AddressLatitude = companyAddressCoordinates.Latitude,
                AddressLongitude = companyAddressCoordinates.Longitude
            };
        }
    }
}