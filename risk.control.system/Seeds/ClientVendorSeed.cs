using System.Diagnostics.Metrics;

using Google.Api;

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public class ClientVendorSeed
    {
        private const string vendorMapSize = "800x800";
        private const string companyMapSize = "800x800";
        public static async Task<(List<Vendor> vendors, List<ClientCompany> companyIds)> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
                    InvestigationServiceType investigationServiceType, InvestigationServiceType discreetServiceType, 
                    InvestigationServiceType docServiceType, LineOfBusiness lineOfBusiness, IHttpContextAccessor httpAccessor, ICustomApiCLient customApiCLient)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var request = httpAccessor.HttpContext?.Request;
            string host = request?.Host.Value;
            var mobileAppUrl = Applicationsettings.APP_DEMO_URL;
            if(host != null && host.Contains(Applicationsettings.AZURE_APP_URL))
            {
                mobileAppUrl = Applicationsettings.APP_URL;
            }
            var globalSetting = new GlobalSettings
            {
                EnableMailbox = true
            };
            var newGlobalSetting = await context.GlobalSettings.AddAsync(globalSetting);
            await context.SaveChangesAsync(null, false);
            var globalSettings = context.GlobalSettings.FirstOrDefault();

            var enableMailbox = globalSettings?.EnableMailbox ?? false;

            var vendors = await VendorSeed.Seed(context,webHostEnvironment,investigationServiceType,discreetServiceType,docServiceType,lineOfBusiness,httpAccessor,customApiCLient);

            var companyPinCode = context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE);

            var companyAddressline = "34 Lasiandra Avenue ";

            var companyAddress = companyAddressline + ", " + companyPinCode.District.Name + ", " + companyPinCode.State.Name + ", " + companyPinCode.Country.Code;
            var companyAddressCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(companyAddress);
            var companyAddressCoordinatesLatLong = companyAddressCoordinates.Latitude + "," + companyAddressCoordinates.Longitude;
            var companyAddressUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={companyAddressCoordinatesLatLong}&zoom=14&size={companyMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{companyAddressCoordinatesLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";


            //CREATE COMPANY1
            string insurerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(Applicationsettings.INSURERLOGO));
            var insurerImage = File.ReadAllBytes(insurerImagePath);

            if (insurerImage == null)
            {
                insurerImage = File.ReadAllBytes(noCompanyImagePath);
            }
            var insurer = new ClientCompany
            {
                Name = Applicationsettings.INSURER,
                Addressline = companyAddressline,
                Branch = "FOREST HILL CHASE",
                Code = Applicationsettings.INSURERCODE,
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "NAB",
                BankAccountNumber = "1234567",
                IFSCCode = "IFSC100",
                CountryId = companyPinCode.CountryId,
                StateId = companyPinCode.StateId,
                DistrictId = companyPinCode.DistrictId,
                PinCodeId = companyPinCode.PinCodeId,
                Description = "CORPORATE OFFICE ",
                Email = Applicationsettings.INSURERDOMAIN,
                DocumentUrl = Applicationsettings.INSURERLOGO,
                DocumentImage = insurerImage,
                PhoneNumber = "9988004739",
                ExpiryDate = DateTime.Now.AddDays(5),
                EmpanelledVendors = vendors,
                Status = CompanyStatus.ACTIVE,
                 AutoAllocation = true,
                 BulkUpload = true,
                Updated = DateTime.Now,
                Deleted = false,
                EnableMailbox = enableMailbox,
                MobileAppUrl = mobileAppUrl,
                AddressMapLocation = companyAddressUrl,
                AddressLatitude = companyAddressCoordinates.Latitude,
                AddressLongitude = companyAddressCoordinates.Longitude
            };

            var insurerCompany = await context.ClientCompany.AddAsync(insurer);

            var companyIds = new List<ClientCompany> { insurerCompany.Entity
                //, hdfcCompany.Entity, bajajCompany.Entity, tataCompany.Entity
            };

            foreach(var vendor in vendors)
            {
                vendor.Clients.Add(insurerCompany.Entity);
            }

            await context.SaveChangesAsync(null, false);
            return (vendors, companyIds);
        }
    }
}