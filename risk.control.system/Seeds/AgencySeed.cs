using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Seeds
{
    public static class AgencySeed
    {
        private const string vendorMapSize = "800x800";

        public static async Task<Vendor> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, ICustomApiClient customApiCLient,
            UserManager<ApplicationUser> vendorUserManager, SeedInput input, List<InvestigationServiceType> servicesTypes, IFileStorageService fileStorageService)
        {
            var states = await context.State.Include(s => s.Country).Where(s => s.Country!.Code == input.COUNTRY).ToListAsync();
            string agencyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(input.PHOTO)!);
            var agencyImage = await File.ReadAllBytesAsync(agencyImagePath);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(agencyImage, Path.GetExtension(agencyImagePath), input.DOMAIN!);
            var agency = await GetAgency(context, input, customApiCLient, relativePath);
            var addedAgency = await context.Vendor.AddAsync(agency);
            await context.SaveChangesAsync(null, false);
            var agencyServices = new List<VendorInvestigationServiceType>();
            foreach (var state in states)
            {
                foreach (var service in servicesTypes)
                {
                    var vendorService = new VendorInvestigationServiceType
                    {
                        VendorId = addedAgency.Entity.VendorId,
                        InvestigationServiceTypeId = service.InvestigationServiceTypeId,
                        Price = 399,
                        InsuranceType = service.InsuranceType,
                        AllDistrictsCheckbox = true,
                        SelectedDistrictIds = { -1 },
                        StateId = state.StateId,
                        CountryId = state.CountryId,
                        Updated = DateTime.UtcNow,
                    };
                    agencyServices.Add(vendorService);
                }
            }
            agency.VendorInvestigationServiceTypes = agencyServices;
            await context.SaveChangesAsync(null, false);
            await AgencyUserSeed.Seed(context, webHostEnvironment, vendorUserManager, addedAgency.Entity, customApiCLient, fileStorageService);
            return addedAgency.Entity;
        }
        private static async Task<Vendor> GetAgency(ApplicationDbContext context, SeedInput input, ICustomApiClient customApiCLient, string relativePath)
        {
            var globalSettings = await context.GlobalSettings.FirstOrDefaultAsync();
            var pinCode = await context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).OrderBy(o => o.State!.Code).LastOrDefaultAsync(s => s.Country!.Code == input.COUNTRY && s.Code == input.PINCODE);
            var address = input.ADDRESSLINE + ", " + pinCode!.District!.Name + ", " + pinCode.State!.Name + ", " + pinCode.Country!.Code;
            var addressCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(address);
            var latLong = addressCoordinates.Latitude + "," + addressCoordinates.Longitude;
            var addressUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={latLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLong}&key={EnvHelper.Get("GOOGLE_MAP_KEY")}";

            return new Vendor
            {
                Name = input.NAME!,
                Addressline = input.ADDRESSLINE!,
                ActivatedDate = DateTime.UtcNow,
                AgreementDate = DateTime.UtcNow,
                BankName = input.BANK,
                BankAccountNumber = "1234567890",
                IFSCCode = input.IFSC,
                PinCode = pinCode,
                Country = pinCode.Country,
                CountryId = pinCode.CountryId,
                DistrictId = pinCode.DistrictId,
                StateId = pinCode.StateId,
                PinCodeId = pinCode.PinCodeId,
                Email = input.DOMAIN!,
                DocumentUrl = relativePath,
                Updated = DateTime.UtcNow,
                Status = VendorStatus.ACTIVE,
                CanChangePassword = globalSettings!.CanChangePassword,
                AddressMapLocation = addressUrl,
                AddressLatitude = addressCoordinates.Latitude,
                AddressLongitude = addressCoordinates.Longitude
            };
        }
    }
}