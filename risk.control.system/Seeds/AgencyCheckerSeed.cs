using System.Diagnostics.Metrics;

using Google.Api;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public class AgencyCheckerSeed
    {
        private const string vendorMapSize = "800x800";
        public static async Task<Vendor> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
                    InvestigationServiceType investigationServiceType, InvestigationServiceType discreetServiceType, InvestigationServiceType docServiceType, 
                    LineOfBusiness lineOfBusiness, IHttpContextAccessor httpAccessor, ICustomApiCLient customApiCLient, UserManager<VendorApplicationUser> vendorUserManager, SeedInput input, InvestigationServiceType claimNonComprehensiveService)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var globalSettings = context.GlobalSettings.FirstOrDefault();

            //CREATE VENDOR COMPANY

            var checkerPinCode = context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).OrderBy(o=>o.State.Code).LastOrDefault(s => s.Country.Code.ToLower() == input.COUNTRY);
            var checkerAddressline = "1, Nice Road";

            var checkerAddress = checkerAddressline + ", " + checkerPinCode.District.Name + ", " + checkerPinCode.State.Name + ", " + checkerPinCode.Country.Code;
            var checkerCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(checkerAddress);
            var checkerLatLong = checkerCoordinates.Latitude + "," + checkerCoordinates.Longitude;
            var checkerUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={checkerLatLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{checkerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

            string checkerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(input.PHOTO));
            var checkerImage = File.ReadAllBytes(checkerImagePath);

            if (checkerImage == null)
            {
                checkerImage = File.ReadAllBytes(noCompanyImagePath);
            }

            var checker = new Vendor
            {
                Name = input.NAME,
                Addressline = checkerAddressline,
                Branch = "MAHATTAN",
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "WESTPAC",
                BankAccountNumber = "1234567",
                IFSCCode = "IFSC100",
                PinCode = checkerPinCode,
                Country = checkerPinCode.Country,
                CountryId = checkerPinCode.CountryId,
                DistrictId = checkerPinCode.DistrictId,
                StateId = checkerPinCode.StateId,
                PinCodeId = checkerPinCode.PinCodeId,
                Description = "HEAD OFFICE ",
                Email = input.DOMAIN,
                PhoneNumber = "8888004739",
                DocumentUrl = input.PHOTO,
                DocumentImage = checkerImage,
                Updated = DateTime.Now,
                Status = VendorStatus.ACTIVE,
                EnableMailbox = globalSettings.EnableMailbox,
                MobileAppUrl = globalSettings.MobileAppUrl,
                CanChangePassword = globalSettings.CanChangePassword,
                AddressMapLocation = checkerUrl,
                AddressLatitude = checkerCoordinates.Latitude,
                AddressLongitude = checkerCoordinates.Longitude
            };

            var checkerAgency = await context.Vendor.AddAsync(checker);
            await context.SaveChangesAsync(null, false);

            var checkerServices = new List<VendorInvestigationServiceType>
            {
                new VendorInvestigationServiceType{
                    VendorId = checkerAgency.Entity.VendorId,
                    InvestigationServiceTypeId = investigationServiceType.InvestigationServiceTypeId,
                    Price = 399,
                    LineOfBusiness = lineOfBusiness,
                    DistrictId = null,
                    StateId = checkerPinCode.StateId,
                    CountryId = checkerPinCode.CountryId,
                    Updated = DateTime.Now,
                },
                new VendorInvestigationServiceType{
                    VendorId = checkerAgency.Entity.VendorId,
                    InvestigationServiceTypeId = docServiceType.InvestigationServiceTypeId,
                    Price = 299,
                    DistrictId = null,
                    StateId = checkerPinCode.StateId,
                    CountryId = checkerPinCode.CountryId,
                    LineOfBusiness = lineOfBusiness,
                    Updated = DateTime.Now,
                },
                new VendorInvestigationServiceType{
                    VendorId = checkerAgency.Entity.VendorId,
                    InvestigationServiceTypeId = discreetServiceType.InvestigationServiceTypeId,
                    Price = 199,
                    DistrictId = null,
                    StateId = checkerPinCode.StateId,
                    CountryId = checkerPinCode.CountryId,
                    LineOfBusiness = lineOfBusiness,
                    Updated = DateTime.Now,
                },
                new VendorInvestigationServiceType{
                    VendorId = checkerAgency.Entity.VendorId,
                    InvestigationServiceTypeId = claimNonComprehensiveService.InvestigationServiceTypeId,
                    Price = 99,
                    DistrictId = null,
                    StateId = checkerPinCode.StateId,
                    CountryId = checkerPinCode.CountryId,
                    LineOfBusiness = lineOfBusiness,
                    Updated = DateTime.Now,
                }
            };
            checker.VendorInvestigationServiceTypes = checkerServices;

            await context.SaveChangesAsync(null, false);
            await VendorApplicationUserSeed.Seed(context, webHostEnvironment, vendorUserManager, checkerAgency.Entity, customApiCLient, httpAccessor);

            return checkerAgency.Entity;
        }
    }
}