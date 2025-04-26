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
                    ICustomApiCLient customApiCLient, UserManager<VendorApplicationUser> vendorUserManager, SeedInput input, List<InvestigationServiceType> servicesTypes)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var globalSettings = context.GlobalSettings.FirstOrDefault();

            //CREATE VENDOR COMPANY

            var checkerPinCode = context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).OrderBy(o=>o.State.Code).LastOrDefault(s => s.Country.Code.ToLower() == input.COUNTRY);
            var checkerAddressline = "1, Nice Road";

            var states = context.State.Include(s => s.Country).Where(s => s.Country.Code.ToLower() == input.COUNTRY).ToList();

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
                CanChangePassword = globalSettings.CanChangePassword,
                AddressMapLocation = checkerUrl,
                AddressLatitude = checkerCoordinates.Latitude,
                AddressLongitude = checkerCoordinates.Longitude
            };

            var checkerAgency = await context.Vendor.AddAsync(checker);
            await context.SaveChangesAsync(null, false);
            var agencyServices = new List<VendorInvestigationServiceType>();
            foreach(var state in states)
            {
                foreach (var service in servicesTypes)
                {
                    var vendorService = new VendorInvestigationServiceType
                    {
                        VendorId = checkerAgency.Entity.VendorId,
                        InvestigationServiceTypeId = service.InvestigationServiceTypeId,
                        Price = 399,
                        InsuranceType = service.InsuranceType,
                        DistrictId = null,
                        StateId = state.StateId,
                        CountryId = state.CountryId,
                        Updated = DateTime.Now,
                    };
                    agencyServices.Add(vendorService);
                }
            }

            checker.VendorInvestigationServiceTypes = agencyServices;

            await context.SaveChangesAsync(null, false);
            await VendorApplicationUserSeed.Seed(context, webHostEnvironment, vendorUserManager, checkerAgency.Entity, customApiCLient);

            return checkerAgency.Entity;
        }
    }
}