using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class AgentSeed
    {
        public static async Task Seed(ApplicationDbContext context, string agentEmailwithSuffix,
            IWebHostEnvironment webHostEnvironment, ICustomApiClient customApiCLient,
            UserManager<VendorApplicationUser> userManager,
            Vendor vendor, string pinCode, string photo, string firstName, string lastName, IFileStorageService fileStorageService, string addressLine = "")
        {

            string agentImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(photo));
            var agentImage = File.ReadAllBytes(agentImagePath);

            var extension = Path.GetExtension(agentImagePath);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(agentImage, extension, vendor.Email, "user");

            var pincode = await context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(c => c.Code == pinCode);
            var address = addressLine + ", " + pincode.District.Name + ", " + pincode.State.Name + ", " + pincode.Country.Code;
            var coordinates = await customApiCLient.GetCoordinatesFromAddressAsync(address);
            var customerLatLong = coordinates.Latitude + "," + coordinates.Longitude;
            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";


            var vendorAgent = new VendorApplicationUser()
            {
                UserName = agentEmailwithSuffix,
                Email = agentEmailwithSuffix,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                Active = true,
                PhoneNumberConfirmed = true,
                Password = TestingData,
                Vendor = vendor,
                PhoneNumber = Applicationsettings.USER_MOBILE,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                IsVendorAdmin = false,
                Addressline = vendor.Addressline,
                Country = pincode?.Country,
                PinCode = pincode,
                CountryId = pincode?.CountryId,
                DistrictId = pincode?.DistrictId ?? default!,
                StateId = pincode?.StateId ?? default!,
                PinCodeId = pincode?.PinCodeId ?? default!,
                ProfilePictureUrl = relativePath,
                Role = AppRoles.AGENT,
                UserRole = AgencyRole.AGENT,
                Updated = DateTime.Now,
                AddressMapLocation = url,
                AddressLatitude = coordinates.Latitude,
                AddressLongitude = coordinates.Longitude
            };
            if (userManager.Users.All(u => u.Id != vendorAgent.Id))
            {
                var user = await userManager.FindByEmailAsync(vendorAgent.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(vendorAgent, TestingData);
                    await userManager.AddToRoleAsync(vendorAgent, AppRoles.AGENT.ToString());
                    //var vendorAgentRole = new ApplicationRole(AppRoles.AGENT.ToString(), AppRoles.AGENT.ToString());
                    //vendorAgent.ApplicationRoles.Add(vendorAgentRole);
                }
            }
        }
    }
}
