using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IAgencyService
    {
        Task<bool> EditAgency(Vendor vendor,IFormFile vendorDocument, string currentUserEmail);
    }
    public class AgencyService : IAgencyService
    {
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ApplicationDbContext context;
        private readonly ISmsService smsService;

        public AgencyService(IWebHostEnvironment webHostEnvironment, ApplicationDbContext context, ISmsService SmsService)
        {
            this.webHostEnvironment = webHostEnvironment;
            this.context = context;
            smsService = SmsService;
        }
        public async Task<bool> EditAgency(Vendor vendor, IFormFile vendorDocument, string currentUserEmail)
        {
            if (vendorDocument is not null)
            {
                string newFileName = vendor.Email + Guid.NewGuid().ToString();
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
            vendor.Updated = DateTime.Now;
            vendor.UpdatedBy = currentUserEmail;
            context.Vendor.Update(vendor);
            var rowsAffected = await context.SaveChangesAsync();
            if(rowsAffected > 0)
            {
                await smsService.DoSendSmsAsync(vendor.PhoneNumber, "Agency account created. Domain : " + vendor.Email);
                return true;
            }
            return false;
        }
    }
}
