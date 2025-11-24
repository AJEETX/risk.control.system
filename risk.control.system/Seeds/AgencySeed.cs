using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public class AgencySeed
    {
        private const string vendorMapSize = "800x800";
        public static async Task<Vendor> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
                    ICustomApiCLient customApiCLient, UserManager<VendorApplicationUser> vendorUserManager, SeedInput input, List<InvestigationServiceType> servicesTypes)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var globalSettings = context.GlobalSettings.FirstOrDefault();

            //CREATE VENDOR COMPANY

            var pinCode = context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).OrderBy(o => o.State.Code).LastOrDefault(s => s.Country.Code.ToLowerInvariant() == input.COUNTRY.ToLowerInvariant() && s.Code == input.PINCODE);
            var addressline = input.ADDRESSLINE;

            var states = context.State.Include(s => s.Country).Where(s => s.Country.Code.ToLowerInvariant() == input.COUNTRY.ToLowerInvariant()).ToList();

            var address = addressline + ", " + pinCode.District.Name + ", " + pinCode.State.Name + ", " + pinCode.Country.Code;
            var addressCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(address);
            var latLong = addressCoordinates.Latitude + "," + addressCoordinates.Longitude;
            var addressUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={latLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

            string checkerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(input.PHOTO));
            var checkerImage = File.ReadAllBytes(checkerImagePath);

            if (checkerImage == null)
            {
                checkerImage = File.ReadAllBytes(noCompanyImagePath);
            }

            var checker = new Vendor
            {
                Name = input.NAME,
                Addressline = addressline,
                Branch = input.BRANCH,
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = input.BANK,
                BankAccountNumber = "1234567890",
                IFSCCode = input.IFSC,
                PinCode = pinCode,
                Country = pinCode.Country,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode.DistrictId,
                StateId = pinCode.StateId,
                PinCodeId = pinCode.PinCodeId,
                Description = "HEAD OFFICE ",
                Email = input.DOMAIN,
                PhoneNumber = "9888004739",
                DocumentUrl = input.PHOTO,
                DocumentImage = checkerImage,
                Updated = DateTime.Now,
                Status = VendorStatus.ACTIVE,
                CanChangePassword = globalSettings.CanChangePassword,
                AddressMapLocation = addressUrl,
                AddressLatitude = addressCoordinates.Latitude,
                AddressLongitude = addressCoordinates.Longitude
            };

            var checkerAgency = await context.Vendor.AddAsync(checker);
            await context.SaveChangesAsync(null, false);
            var agencyServices = new List<VendorInvestigationServiceType>();
            foreach (var state in states)
            {
                foreach (var service in servicesTypes)
                {
                    var vendorService = new VendorInvestigationServiceType
                    {
                        VendorId = checkerAgency.Entity.VendorId,
                        InvestigationServiceTypeId = service.InvestigationServiceTypeId,
                        Price = 399,
                        InsuranceType = service.InsuranceType,
                        AllDistrictsCheckbox = true,
                        SelectedDistrictIds = { -1 },
                        StateId = state.StateId,
                        CountryId = state.CountryId,
                        Updated = DateTime.Now,
                    };
                    agencyServices.Add(vendorService);
                }
            }

            checker.VendorInvestigationServiceTypes = agencyServices;

            await context.SaveChangesAsync(null, false);
            await AgencyUserSeed.Seed(context, webHostEnvironment, vendorUserManager, checkerAgency.Entity, customApiCLient);

            return checkerAgency.Entity;
        }
    }
}