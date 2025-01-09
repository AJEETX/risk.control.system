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
    public class VendorSeed
    {
        private const string vendorMapSize = "800x800";
        public static async Task<List<Vendor>> Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment,
                    InvestigationServiceType investigationServiceType, InvestigationServiceType discreetServiceType, InvestigationServiceType docServiceType, 
                    LineOfBusiness lineOfBusiness, IHttpContextAccessor httpAccessor, ICustomApiCLient customApiCLient)
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

            var checkerPinCode = context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE2);
            var checkerAddressline = "1, Nice Road";

            var checkerAddress = checkerAddressline + ", " + checkerPinCode.District.Name + ", " + checkerPinCode.State.Name + ", " + checkerPinCode.Country.Code;
            var checkerCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(checkerAddress);
            var checkerLatLong = checkerCoordinates.Latitude + "," + checkerCoordinates.Longitude;
            var checkerUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={checkerLatLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{checkerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

            string checkerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(Applicationsettings.AGENCY1PHOTO));
            var checkerImage = File.ReadAllBytes(checkerImagePath);

            if (checkerImage == null)
            {
                checkerImage = File.ReadAllBytes(noCompanyImagePath);
            }

            var checker = new Vendor
            {
                Name = Applicationsettings.AGENCY1NAME,
                Addressline = checkerAddressline,
                Branch = "MAHATTAN",
                Code = Applicationsettings.AGENCY1CODE,
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "WESTPAC",
                BankAccountNumber = "1234567",
                IFSCCode = "IFSC100",
                CountryId = checkerPinCode.CountryId,
                DistrictId = checkerPinCode.DistrictId,
                StateId = checkerPinCode.StateId,
                PinCodeId = checkerPinCode.PinCodeId,
                Description = "HEAD OFFICE ",
                Email = Applicationsettings.AGENCY1DOMAIN,
                PhoneNumber = "8888004739",
                DocumentUrl = Applicationsettings.AGENCY1PHOTO,
                DocumentImage = checkerImage,
                Updated = DateTime.Now,
                Status = VendorStatus.ACTIVE,
                EnableMailbox = enableMailbox,
                MobileAppUrl = mobileAppUrl,
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
                    Price = 199,
                    LineOfBusiness = lineOfBusiness,
                    DistrictId = checkerPinCode.DistrictId,
                    StateId = checkerPinCode.StateId,
                    CountryId = checkerPinCode.CountryId,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == checkerPinCode.Code)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == checkerPinCode.Code)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                },
                new VendorInvestigationServiceType{
                    VendorId = checkerAgency.Entity.VendorId,
                    InvestigationServiceTypeId = docServiceType.InvestigationServiceTypeId,
                    Price = 99,
                    DistrictId = checkerPinCode.DistrictId,
                    StateId = checkerPinCode.StateId,
                    CountryId = checkerPinCode.CountryId,
                    LineOfBusiness = lineOfBusiness,
                    PincodeServices = new List<ServicedPinCode>
                    {
                        new ServicedPinCode
                        {
                            Pincode = context.PinCode.FirstOrDefault(s => s.Code == checkerPinCode.Code)?.Code ?? default !,
                            Name = context.PinCode.FirstOrDefault(s => s.Code == checkerPinCode.Code)?.Name ?? default !
                        }
                    },
                    Updated = DateTime.Now,
                }
            };
            checker.VendorInvestigationServiceTypes = checkerServices;

            var verifyPinCode = context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE3);
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
                EnableMailbox = enableMailbox,
                MobileAppUrl = mobileAppUrl,
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

            await context.SaveChangesAsync(null, false);

            var investigatePinCode = context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE4);

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
                Addressline = investigateAddress,
                Branch = "CLAYTON ROAD",
                Code = Applicationsettings.AGENCY3CODE,
                ActivatedDate = DateTime.Now,
                AgreementDate = DateTime.Now,
                BankName = "HDFC BANK",
                BankAccountNumber = "9876543",
                IFSCCode = "IFSC999",
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

            var vendors = new List<Vendor> { checker, verify, investigate };
            return vendors;
        }
    }
}