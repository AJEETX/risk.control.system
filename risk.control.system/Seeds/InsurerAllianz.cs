using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public class InsurerAllianz
    {
        private const string vendorMapSize = "800x800";
        private const string companyMapSize = "800x800";
        public static async Task<ClientCompany> Seed(ApplicationDbContext context, List<Vendor> vendors, IWebHostEnvironment webHostEnvironment,
                    ICustomApiCLient customApiCLient, UserManager<ClientCompanyApplicationUser> clientUserManager, SeedInput input)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var globalSettings = context.GlobalSettings.FirstOrDefault();

            var companyPinCode = context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.Country.Code.ToLower() == input.COUNTRY);

            var companyAddressline = "34 Lasiandra Avenue ";
            var companyAddress = companyAddressline + ", " + companyPinCode.District.Name + ", " + companyPinCode.State.Name + ", " + companyPinCode.Country.Code;
            var companyAddressCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(companyAddress);
            var companyAddressCoordinatesLatLong = companyAddressCoordinates.Latitude + "," + companyAddressCoordinates.Longitude;
            var companyAddressUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={companyAddressCoordinatesLatLong}&zoom=14&size={companyMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{companyAddressCoordinatesLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

            //CREATE COMPANY1
            string insurerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(input.PHOTO));
            var insurerImage = File.ReadAllBytes(insurerImagePath);

            if (insurerImage == null)
            {
                insurerImage = File.ReadAllBytes(noCompanyImagePath);
            }
            vendors = vendors.Where(v => v.CountryId == companyPinCode.CountryId).ToList();

            var insurer = new ClientCompany
            {
                Name = input.NAME,
                Addressline = companyAddressline,
                Branch = "FOREST HILL CHASE",
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "NAB",
                BankAccountNumber = "1234567",
                IFSCCode = "IFSC100",
                PinCode = companyPinCode,
                Country = companyPinCode.Country,
                CountryId = companyPinCode.CountryId,
                StateId = companyPinCode.StateId,
                DistrictId = companyPinCode.DistrictId,
                PinCodeId = companyPinCode.PinCodeId,
                //Description = "CORPORATE OFFICE ",
                Email = input.DOMAIN,
                DocumentUrl = input.PHOTO,
                DocumentImage = insurerImage,
                PhoneNumber = "9988004739",
                ExpiryDate = DateTime.Now.AddDays(5),
                EmpanelledVendors = vendors,
                Status = CompanyStatus.ACTIVE,
                AutoAllocation = globalSettings.AutoAllocation,
                BulkUpload = globalSettings.BulkUpload,
                Updated = DateTime.Now,
                Deleted = false,
                MobileAppUrl = globalSettings.MobileAppUrl,
                VerifyPan = globalSettings.VerifyPan,
                VerifyPassport = globalSettings.VerifyPassport,
                EnableMedia = globalSettings.EnableMedia,
                PanIdfyUrl = globalSettings.PanIdfyUrl,
                AiEnabled = globalSettings.AiEnabled,
                CanChangePassword = globalSettings.CanChangePassword,
                EnablePassport = globalSettings.EnablePassport,
                HasSampleData = globalSettings.HasSampleData,
                PassportApiHost = globalSettings.PassportApiHost,
                PassportApiKey = globalSettings.PassportApiKey,
                PassportApiUrl = globalSettings.PassportApiUrl,
                PanAPIHost = globalSettings.PanAPIHost,
                PanAPIKey = globalSettings.PanAPIKey,
                UpdateAgentAnswer = globalSettings.UpdateAgentAnswer,
                AddressMapLocation = companyAddressUrl,
                AddressLatitude = companyAddressCoordinates.Latitude,
                AddressLongitude = companyAddressCoordinates.Longitude
            };

            var insurerCompany = await context.ClientCompany.AddAsync(insurer);

            await context.SaveChangesAsync(null, false);

            var creator = await ClientApplicationUserSeed.Seed(context, webHostEnvironment, clientUserManager, insurerCompany.Entity);

            var claimTemplate = ReportTemplateSeed.QuestionsCLAIM(context, insurer);
            var underwriting = ReportTemplateSeed.QuestionsUNDERWRITING(context, insurer);

            await Export2JsonReportTemplatesAsync(claimTemplate, "claimTemplate.json");
            await Export2JsonReportTemplatesAsync(underwriting, "underwritingTemplate.json");

            await context.SaveChangesAsync(null, false);

            return insurerCompany.Entity;
        }
        private static async Task Export2JsonReportTemplatesAsync(ReportTemplate template, string file)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };

            string jsonString = JsonSerializer.Serialize(template, options);

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "report", file);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)); // Ensure folder exists

            await File.WriteAllTextAsync(filePath, jsonString);
        }
    }
}