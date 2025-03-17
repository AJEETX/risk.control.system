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
    public class AgencyHonestSeed
    {
        private const string vendorMapSize = "800x800";
        public static async Task<Vendor> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
                    InvestigationServiceType investigationServiceType, InvestigationServiceType discreetServiceType, InvestigationServiceType docServiceType, 
                    LineOfBusiness lineOfBusiness, IHttpContextAccessor httpAccessor, ICustomApiCLient customApiCLient, UserManager<VendorApplicationUser> vendorUserManager)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var globalSettings = context.GlobalSettings.FirstOrDefault();

            //CREATE VENDOR COMPANY

            var honestPinCode = context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.Country.Code.ToLower() == "in");
            var honestAddressline = "1, MG Road";

            var honestAddress = honestAddressline + ", " + honestPinCode.District.Name + ", " + honestPinCode.State.Name + ", " + honestPinCode.Country.Code;
            var honestCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(honestAddress);
            var honestLatLong = honestCoordinates.Latitude + "," + honestCoordinates.Longitude;
            var honestUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={honestLatLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{honestLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

            string honestImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(Applicationsettings.AGENCY5PHOTO));
            var honestImage = File.ReadAllBytes(honestImagePath);

            if (honestImage == null)
            {
                honestImage = File.ReadAllBytes(noCompanyImagePath);
            }

            var honest = new Vendor
            {
                Name = Applicationsettings.AGENCY5NAME,
                Addressline = honestAddressline,
                Branch = "IndiraPuram",
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "HDFC",
                BankAccountNumber = "1234567",
                IFSCCode = "IFSC100",
                PinCode = honestPinCode,
                Country = honestPinCode.Country,
                CountryId = honestPinCode.CountryId,
                DistrictId = honestPinCode.DistrictId,
                StateId = honestPinCode.StateId,
                PinCodeId = honestPinCode.PinCodeId,
                Description = "HEAD OFFICE ",
                Email = Applicationsettings.AGENCY5DOMAIN,
                PhoneNumber = "8888004739",
                DocumentUrl = Applicationsettings.AGENCY5PHOTO,
                DocumentImage = honestImage,
                Updated = DateTime.Now,
                Status = VendorStatus.ACTIVE,
                EnableMailbox = globalSettings.EnableMailbox,
                MobileAppUrl = globalSettings.MobileAppUrl,
                CanChangePassword = globalSettings.CanChangePassword,
                AddressMapLocation = honestUrl,
                AddressLatitude = honestCoordinates.Latitude,
                AddressLongitude = honestCoordinates.Longitude
            };

            var honestAgency = await context.Vendor.AddAsync(honest);
            await context.SaveChangesAsync(null, false);

            var honestServices = new List<VendorInvestigationServiceType>
            {
                new VendorInvestigationServiceType{
                    VendorId = honestAgency.Entity.VendorId,
                    InvestigationServiceTypeId = investigationServiceType.InvestigationServiceTypeId,
                    Price = 199,
                    LineOfBusiness = lineOfBusiness,
                    DistrictId = honestPinCode.DistrictId,
                    StateId = honestPinCode.StateId,
                    CountryId = honestPinCode.CountryId,
                    Updated = DateTime.Now,
                },
                new VendorInvestigationServiceType{
                    VendorId = honestAgency.Entity.VendorId,
                    InvestigationServiceTypeId = docServiceType.InvestigationServiceTypeId,
                    Price = 99,
                    DistrictId = honestPinCode.DistrictId,
                    StateId = honestPinCode.StateId,
                    CountryId = honestPinCode.CountryId,
                    LineOfBusiness = lineOfBusiness,
                    Updated = DateTime.Now,
                }
            };
            honest.VendorInvestigationServiceTypes = honestServices;

            await context.SaveChangesAsync(null, false);
            await VendorApplicationUserSeed.Seed(context, webHostEnvironment, vendorUserManager, honestAgency.Entity, customApiCLient, httpAccessor);

            return honestAgency.Entity;
        }
    }
}