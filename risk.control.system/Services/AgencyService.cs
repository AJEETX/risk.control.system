using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IAgencyService
    {
        Task<bool> EditAgency(Vendor vendor, IFormFile vendorDocument, string currentUserEmail, string portal_base_url);
    }
    public class AgencyService : IAgencyService
    {
        private const string vendorMapSize = "800x800";
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ICustomApiCLient customApiCLient;
        private readonly ApplicationDbContext context;
        private readonly ISmsService smsService;

        public AgencyService(IWebHostEnvironment webHostEnvironment, ICustomApiCLient customApiCLient, ApplicationDbContext context, ISmsService SmsService)
        {
            this.webHostEnvironment = webHostEnvironment;
            this.customApiCLient = customApiCLient;
            this.context = context;
            smsService = SmsService;
        }
        public async Task<bool> EditAgency(Vendor vendor, IFormFile vendorDocument, string currentUserEmail, string portal_base_url)
        {
            if (vendorDocument is not null)
            {
                string newFileName = Guid.NewGuid().ToString();
                string fileExtension = Path.GetExtension(Path.GetFileName(vendorDocument.FileName));
                newFileName += fileExtension;
                string path = Path.Combine(webHostEnvironment.WebRootPath, "agency");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                var upload = Path.Combine(webHostEnvironment.WebRootPath, "agency", newFileName);

                using var dataStream = new MemoryStream();
                vendorDocument.CopyTo(dataStream);
                vendor.DocumentImageExtension = fileExtension;
                vendor.DocumentImage = dataStream.ToArray();
                vendorDocument.CopyTo(new FileStream(upload, FileMode.Create));
                vendor.DocumentUrl = "/agency/" + newFileName;
            }
            else
            {
                var vendorUser = context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);
                var existingVendor = await context.Vendor.AsNoTracking().FirstOrDefaultAsync(c => c.VendorId == vendorUser.VendorId);
                if (existingVendor.DocumentImage != null || existingVendor.DocumentUrl != null)
                {
                    vendor.DocumentImage = existingVendor.DocumentImage;
                    vendor.DocumentUrl = existingVendor.DocumentUrl;
                }
            }
            vendor.PinCodeId = vendor.SelectedPincodeId;
            vendor.DistrictId = vendor.SelectedDistrictId;
            vendor.StateId = vendor.SelectedStateId;
            vendor.CountryId = vendor.SelectedCountryId;
            vendor.BankName = vendor.BankName.ToUpper();
            vendor.IFSCCode = vendor.IFSCCode.ToUpper();
            var pinCode = context.PinCode.Include(p => p.Country).Include(p => p.State).Include(p => p.District).FirstOrDefault(s => s.PinCodeId == vendor.SelectedPincodeId);

            //var companyAddress = vendor.Addressline + ", " + pinCode.District.Name + ", " + pinCode.State.Name + ", " + pinCode.Country.Code;
            //var companyCoordinates = await customApiCLient.GetCoordinatesFromAddressAsync(companyAddress);
            //var companyLatLong = companyCoordinates.Latitude + "," + companyCoordinates.Longitude;
            //var url = $"https://maps.googleapis.com/maps/api/staticmap?center={companyLatLong}&zoom=14&size={vendorMapSize}&maptype=roadmap&markers=color:red%7Clabel:S%7C{companyLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            //vendor.AddressLatitude = companyCoordinates.Latitude;
            //vendor.AddressLongitude = companyCoordinates.Longitude;
            //vendor.AddressMapLocation = url;
            vendor.PhoneNumber = vendor.PhoneNumber.TrimStart('0');
            vendor.IsUpdated = true;
            vendor.Updated = DateTime.Now;
            vendor.UpdatedBy = currentUserEmail;
            context.Vendor.Update(vendor);
            var rowsAffected = await context.SaveChangesAsync();
            if (rowsAffected > 0)
            {
                await smsService.DoSendSmsAsync(pinCode.Country.Code, pinCode.Country.ISDCode + vendor.PhoneNumber, "Agency account created. \n\nDomain : " + vendor.Email + "\n" + portal_base_url);
                return true;
            }
            return false;
        }
    }
}
