using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public static class AgencySeed
    {
        private const string vendorMapSize = "800x800";
        public static async Task<Vendor> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
                    ICustomApiClient customApiCLient, UserManager<ApplicationUser> vendorUserManager, SeedInput input, List<InvestigationServiceType> servicesTypes, IFileStorageService fileStorageService)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var globalSettings = await context.GlobalSettings.FirstOrDefaultAsync();

            //CREATE VENDOR COMPANY

            var pinCode = await context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).OrderBy(o => o.State.Code).LastOrDefaultAsync(s => s.Country.Code == input.COUNTRY && s.Code == input.PINCODE);

            var states =await context.State.Include(s => s.Country).Where(s => s.Country.Code == input.COUNTRY).ToListAsync();

            var address = input.ADDRESSLINE + ", " + pinCode.District.Name + ", " + pinCode.State.Name + ", " + pinCode.Country.Code;
            var addressCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(address);
            var latLong = addressCoordinates.Latitude + "," + addressCoordinates.Longitude;
            var addressUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={latLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

            string agencyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(input.PHOTO));
            var agencyImage =await File.ReadAllBytesAsync(agencyImagePath);

            if (agencyImage == null)
            {
                agencyImage =await File.ReadAllBytesAsync(noCompanyImagePath);
            }
            var extension = Path.GetExtension(agencyImagePath);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(agencyImage, extension, input.DOMAIN);
            var checker = new Vendor
            {
                Name = input.NAME,
                Addressline = input.ADDRESSLINE,
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
                PhoneNumber = input.PHONE,
                DocumentUrl = relativePath,
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
            await AgencyUserSeed.Seed(context, webHostEnvironment, vendorUserManager, checkerAgency.Entity, customApiCLient, fileStorageService);

            return checkerAgency.Entity;
        }
    }
}