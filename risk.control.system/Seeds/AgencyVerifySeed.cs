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
    public class AgencyVerifySeed
    {
        private const string vendorMapSize = "800x800";
        public static async Task<Vendor> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
                    InvestigationServiceType investigationServiceType, InvestigationServiceType discreetServiceType, InvestigationServiceType docServiceType, 
                    LineOfBusiness lineOfBusiness, IHttpContextAccessor httpAccessor, ICustomApiCLient customApiCLient, UserManager<VendorApplicationUser> vendorUserManager)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var globalSettings = context.GlobalSettings.FirstOrDefault();

            //CREATE VENDOR COMPANY

            var verifyPinCode = context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.Country.Code.ToLower() =="au" );
            var verifyAddressline = "10, Clear Road";

            var verifyAddress = verifyAddressline + ", " + verifyPinCode.District.Name + ", " + verifyPinCode.State.Name + ", " + verifyPinCode.Country.Code;
            var verifyCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(verifyAddress);
            var verifyLatLong = verifyCoordinates.Latitude + "," + verifyCoordinates.Longitude;
            var verifyUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={verifyLatLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{verifyLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";


            string verifyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(Applicationsettings.AGENCY2PHOTO));
            var verifyImage = File.ReadAllBytes(verifyImagePath);

            if (verifyImage == null)
            {
                verifyImage = File.ReadAllBytes(noCompanyImagePath);
            }

            var verify = new Vendor
            {
                Name = Applicationsettings.AGENCY2NAME,
                Addressline = verifyAddressline,
                Branch = "BLACKBURN",
                Code = Applicationsettings.AGENCY2CODE,
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "SBI BANK",
                BankAccountNumber = "9876543",
                IFSCCode = "IFSC999",
                PinCode = verifyPinCode,
                Country = verifyPinCode.Country,
                CountryId = verifyPinCode.CountryId,
                StateId = verifyPinCode.StateId,
                DistrictId = verifyPinCode.DistrictId,
                PinCodeId = verifyPinCode.PinCodeId,
                Description = "HEAD OFFICE ",
                Email = Applicationsettings.AGENCY2DOMAIN,
                PhoneNumber = "4444404739",
                DocumentUrl = Applicationsettings.AGENCY2PHOTO,
                DocumentImage = verifyImage,
                Status = VendorStatus.ACTIVE,
                Updated = DateTime.Now,
                EnableMailbox = globalSettings.EnableMailbox,
                MobileAppUrl = globalSettings.MobileAppUrl,
                CanChangePassword = globalSettings.CanChangePassword,
                AddressMapLocation = verifyUrl,
                AddressLatitude = verifyCoordinates.Latitude,
                AddressLongitude = verifyCoordinates.Longitude
            };

            var verifyAgency = await context.Vendor.AddAsync(verify);
            var verifyServices = new List<VendorInvestigationServiceType>
            {
                new VendorInvestigationServiceType{
                    VendorId = verifyAgency.Entity.VendorId,
                    InvestigationServiceTypeId = investigationServiceType.InvestigationServiceTypeId,
                    Price = 399,
                    DistrictId = verifyPinCode.DistrictId,
                    StateId = verifyPinCode.StateId,
                    CountryId = verifyPinCode.CountryId,
                    LineOfBusinessId = lineOfBusiness.LineOfBusinessId,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == verifyPinCode.Code)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == verifyPinCode.Code)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                },
                new VendorInvestigationServiceType{
                    VendorId = verifyAgency.Entity.VendorId,
                    InvestigationServiceTypeId = discreetServiceType.InvestigationServiceTypeId,
                    Price = 299,
                    DistrictId = verifyPinCode.DistrictId,
                    StateId = verifyPinCode.StateId,
                    CountryId = verifyPinCode.CountryId,
                    LineOfBusinessId = lineOfBusiness.LineOfBusinessId,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == verifyPinCode.Code)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == verifyPinCode.Code)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                }
            };
            verify.VendorInvestigationServiceTypes = verifyServices;

            await VendorApplicationUserSeed.Seed(context, webHostEnvironment, vendorUserManager, verify, customApiCLient, httpAccessor);

            await context.SaveChangesAsync(null, false);

            return verify;
        }
    }
}