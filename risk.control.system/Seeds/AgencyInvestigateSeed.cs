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
    public class AgencyInvestigateSeed
    {
        private const string vendorMapSize = "800x800";
        public static async Task<Vendor> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
                    InvestigationServiceType investigationServiceType, InvestigationServiceType discreetServiceType, InvestigationServiceType docServiceType, 
                    LineOfBusiness lineOfBusiness, IHttpContextAccessor httpAccessor, ICustomApiCLient customApiCLient, UserManager<VendorApplicationUser> vendorUserManager)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var request = httpAccessor.HttpContext?.Request;
            string host = request?.Host.Value;
            var mobileAppUrl = Applicationsettings.APP_DEMO_URL;
            if (host != null && host.Contains(Applicationsettings.AZURE_APP_URL))
            {
                mobileAppUrl = Applicationsettings.APP_URL;
            }
           
            var globalSettings = context.GlobalSettings.FirstOrDefault();

            var enableMailbox = globalSettings?.EnableMailbox ?? false;
            //CREATE VENDOR COMPANY
            var investigatePinCode = context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.Country.Code.ToLower() == "au");

            var investigateAddressline = "1, Main Road";

            var investigateAddress = investigateAddressline + ", " + investigatePinCode.District.Name + ", " + investigatePinCode.State.Name + ", " + investigatePinCode.Country.Code;
            var investigateCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(investigateAddress);
            var investigateLatLong = investigateCoordinates.Latitude + "," + investigateCoordinates.Longitude;
            var investigateUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={investigateLatLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{investigateLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";


            string investigateImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(Applicationsettings.AGENCY3PHOTO));
            var investigateImage = File.ReadAllBytes(investigateImagePath);

            if (investigateImage == null)
            {
                investigateImage = File.ReadAllBytes(noCompanyImagePath);
            }

            var investigate = new Vendor
            {
                Name = Applicationsettings.AGENCY3NAME,
                Addressline = investigateAddressline,
                Branch = "CLAYTON ROAD",
                Code = Applicationsettings.AGENCY3CODE,
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "HDFC BANK",
                BankAccountNumber = "9876543",
                IFSCCode = "IFSC999",
                PinCode = investigatePinCode,
                Country = investigatePinCode.Country,
                CountryId = investigatePinCode.CountryId,
                StateId = investigatePinCode.StateId,
                DistrictId = investigatePinCode.DistrictId,
                PinCodeId = investigatePinCode.PinCodeId,
                Description = "HEAD OFFICE ",
                Email = Applicationsettings.AGENCY3DOMAIN,
                PhoneNumber = "7964404160",
                DocumentUrl = Applicationsettings.AGENCY3PHOTO,
                DocumentImage = investigateImage,
                Status = VendorStatus.ACTIVE,
                Updated = DateTime.Now,
                EnableMailbox = enableMailbox,
                MobileAppUrl = mobileAppUrl,
                AddressMapLocation = investigateUrl,
                AddressLatitude = investigateCoordinates.Latitude,
                AddressLongitude = investigateCoordinates.Longitude
            };

            var investigateAgency = await context.Vendor.AddAsync(investigate);

            var investigateServices = new List<VendorInvestigationServiceType>
            {
                new VendorInvestigationServiceType{
                    VendorId = investigateAgency.Entity.VendorId,
                    InvestigationServiceTypeId = docServiceType.InvestigationServiceTypeId,
                    Price = 199,
                    DistrictId = investigatePinCode.DistrictId,
                    StateId = investigatePinCode.StateId,
                    CountryId = investigatePinCode.CountryId,
                    LineOfBusinessId = lineOfBusiness.LineOfBusinessId,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == investigatePinCode.Code)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == investigatePinCode.Code)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                },
                new VendorInvestigationServiceType{
                    VendorId = investigateAgency.Entity.VendorId,
                    InvestigationServiceTypeId = discreetServiceType.InvestigationServiceTypeId,
                    Price = 299,
                    DistrictId = investigatePinCode.DistrictId,
                    StateId = investigatePinCode.StateId,
                    CountryId = investigatePinCode.CountryId,
                    LineOfBusinessId = lineOfBusiness.LineOfBusinessId,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == investigatePinCode.Code)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == investigatePinCode.Code)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                },
                new VendorInvestigationServiceType{
                    VendorId = investigateAgency.Entity.VendorId,
                    InvestigationServiceTypeId = investigationServiceType.InvestigationServiceTypeId,
                    Price = 599,
                    DistrictId = investigatePinCode.DistrictId,
                    StateId = investigatePinCode.StateId,
                    CountryId = investigatePinCode.CountryId,
                    LineOfBusinessId = lineOfBusiness.LineOfBusinessId,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == investigatePinCode.Code)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == investigatePinCode.Code)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                }
            };
            investigate.VendorInvestigationServiceTypes = investigateServices;

            await context.SaveChangesAsync(null, false);

            await VendorApplicationUserSeed.Seed(context, webHostEnvironment, vendorUserManager, investigateAgency.Entity, customApiCLient, httpAccessor);

            return investigateAgency.Entity;
        }
    }
}