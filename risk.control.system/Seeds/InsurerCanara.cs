using System.ComponentModel.Design;
using System.Diagnostics.Metrics;

using Google.Api;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public class InsurerCanara
    {
        private const string vendorMapSize = "800x800";
        private const string companyMapSize = "800x800";
        public static async Task<ClientCompany> Seed(ApplicationDbContext context, List<Vendor> vendors, IWebHostEnvironment webHostEnvironment,
                    InvestigationServiceType investigationServiceType, InvestigationServiceType discreetServiceType,
                    InvestigationServiceType docServiceType, LineOfBusiness lineOfBusiness, IHttpContextAccessor httpAccessor,
                    ICustomApiCLient customApiCLient, UserManager<ClientCompanyApplicationUser> clientUserManager)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var globalSettings = context.GlobalSettings.FirstOrDefault();

            var companyPinCode = context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.District.Name.ToLower().Contains("delhi"));

            var companyAddressline = "34 Lasiandra Avenue ";
            var companyAddress = companyAddressline + ", " + companyPinCode.District.Name + ", " + companyPinCode.State.Name + ", " + companyPinCode.Country.Code;
            var companyAddressCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(companyAddress);
            var companyAddressCoordinatesLatLong = companyAddressCoordinates.Latitude + "," + companyAddressCoordinates.Longitude;
            var companyAddressUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={companyAddressCoordinatesLatLong}&zoom=14&size={companyMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{companyAddressCoordinatesLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

            //CREATE COMPANY1
            string insurerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(Applicationsettings.CANARALOGO));
            var insurerImage = File.ReadAllBytes(insurerImagePath);

            if (insurerImage == null)
            {
                insurerImage = File.ReadAllBytes(noCompanyImagePath);
            }
            vendors = vendors.Where(v => v.CountryId == companyPinCode.CountryId).ToList();

            var insurer = new ClientCompany
            {
                Name = Applicationsettings.CANARA,
                Addressline = companyAddressline,
                Branch = "FOREST HILL CHASE",
                //Code = Applicationsettings.CANARACODE,
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
                Email = Applicationsettings.CANARADOMAIN,
                DocumentUrl = Applicationsettings.CANARALOGO,
                DocumentImage = insurerImage,
                PhoneNumber = "9988004739",
                ExpiryDate = DateTime.Now.AddDays(5),
                EmpanelledVendors = vendors,
                Status = CompanyStatus.ACTIVE,
                AutoAllocation = globalSettings.AutoAllocation,
                BulkUpload = globalSettings.BulkUpload,
                Updated = DateTime.Now,
                Deleted = false,
                EnableMailbox = globalSettings.EnableMailbox,
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
                UpdateAgentReport = globalSettings.UpdateAgentReport,

                AddressMapLocation = companyAddressUrl,
                AddressLatitude = companyAddressCoordinates.Latitude,
                AddressLongitude = companyAddressCoordinates.Longitude
            };

            var insurerCompany = await context.ClientCompany.AddAsync(insurer);
            await context.SaveChangesAsync(null, false);

            await ClientApplicationUserSeed.Seed(context, webHostEnvironment, clientUserManager, insurerCompany.Entity, httpAccessor);

            return insurerCompany.Entity;
        }
    }
}