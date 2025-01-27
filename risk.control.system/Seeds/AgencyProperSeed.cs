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
    public class AgencyProperSeed
    {
        private const string vendorMapSize = "800x800";
        public static async Task<Vendor> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
                    InvestigationServiceType investigationServiceType, InvestigationServiceType discreetServiceType, InvestigationServiceType docServiceType, 
                    LineOfBusiness lineOfBusiness, IHttpContextAccessor httpAccessor, ICustomApiCLient customApiCLient, UserManager<VendorApplicationUser> vendorUserManager)
        {
            string noCompanyImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_IMAGE);

            var globalSettings = context.GlobalSettings.FirstOrDefault();

            //CREATE VENDOR COMPANY

            var properPinCode = context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.Country.Code.ToLower() == "in");
            var properAddressline = "12, GT Road";

            var properAddress = properAddressline + ", " + properPinCode.District.Name + ", " + properPinCode.State.Name + ", " + properPinCode.Country.Code;
            var properCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(properAddress);
            var properLatLong = properCoordinates.Latitude + "," + properCoordinates.Longitude;
            var properUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={properLatLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{properLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

            string properImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(Applicationsettings.AGENCY4PHOTO));
            var properImage = File.ReadAllBytes(properImagePath);

            if (properImage == null)
            {
                properImage = File.ReadAllBytes(noCompanyImagePath);
            }

            var proper = new Vendor
            {
                Name = Applicationsettings.AGENCY4NAME,
                Addressline = properAddressline,
                Branch = "Ram nagar",
                Code = Applicationsettings.AGENCY4CODE,
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "HDFC",
                BankAccountNumber = "1234567",
                IFSCCode = "IFSC100",
                PinCode = properPinCode,
                Country = properPinCode.Country,
                CountryId = properPinCode.CountryId,
                DistrictId = properPinCode.DistrictId,
                StateId = properPinCode.StateId,
                PinCodeId = properPinCode.PinCodeId,
                Description = "HEAD OFFICE ",
                Email = Applicationsettings.AGENCY4DOMAIN,
                PhoneNumber = "1234004739",
                DocumentUrl = Applicationsettings.AGENCY4PHOTO,
                DocumentImage = properImage,
                Updated = DateTime.Now,
                Status = VendorStatus.ACTIVE,
                EnableMailbox = globalSettings.EnableMailbox,
                MobileAppUrl = globalSettings.MobileAppUrl,
                CanChangePassword = globalSettings.CanChangePassword,
                AddressMapLocation = properUrl,
                AddressLatitude = properCoordinates.Latitude,
                AddressLongitude = properCoordinates.Longitude
            };

            var properAgency = await context.Vendor.AddAsync(proper);
            await context.SaveChangesAsync(null, false);

            var properServices = new List<VendorInvestigationServiceType>
            {
                new VendorInvestigationServiceType{
                    VendorId = properAgency.Entity.VendorId,
                    InvestigationServiceTypeId = investigationServiceType.InvestigationServiceTypeId,
                    Price = 199,
                    LineOfBusiness = lineOfBusiness,
                    DistrictId = properPinCode.DistrictId,
                    StateId = properPinCode.StateId,
                    CountryId = properPinCode.CountryId,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == properPinCode.Code)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == properPinCode.Code)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                },
                new VendorInvestigationServiceType{
                    VendorId = properAgency.Entity.VendorId,
                    InvestigationServiceTypeId = docServiceType.InvestigationServiceTypeId,
                    Price = 99,
                    DistrictId = properPinCode.DistrictId,
                    StateId = properPinCode.StateId,
                    CountryId = properPinCode.CountryId,
                    LineOfBusiness = lineOfBusiness,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == properPinCode.Code)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == properPinCode.Code)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                }
            };
            proper.VendorInvestigationServiceTypes = properServices;

            await context.SaveChangesAsync(null, false);
            await VendorApplicationUserSeed.Seed(context, webHostEnvironment, vendorUserManager, properAgency.Entity, customApiCLient, httpAccessor);

            return properAgency.Entity;
        }
    }
}