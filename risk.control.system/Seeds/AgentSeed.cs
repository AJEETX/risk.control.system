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
            IWebHostEnvironment webHostEnvironment, ICustomApiCLient customApiCLient,
            UserManager<VendorApplicationUser> userManager,
            Vendor vendor, string pinCode, string photo, string firstName, string lastName, string addressLine = "")
        {

            var noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);
            string agentImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", Path.GetFileName(photo));
            var agentImage = File.ReadAllBytes(agentImagePath);

            if (agentImage == null)
            {
                agentImage = File.ReadAllBytes(noUserImagePath);
            }
            var pincode = context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefault(c => c.Code == pinCode);
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
                Password = Password,
                Vendor = vendor,
                PhoneNumber = Applicationsettings.USER_MOBILE,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                IsVendorAdmin = false,
                Addressline = "23 Vincent Avenue",
                Country = pincode?.Country,
                PinCode = pincode,
                CountryId = pincode?.CountryId,
                DistrictId = pincode?.DistrictId ?? default!,
                StateId = pincode?.StateId ?? default!,
                PinCodeId = pincode?.PinCodeId ?? default!,
                ProfilePictureUrl = photo,
                ProfilePicture = agentImage,
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
                    await userManager.CreateAsync(vendorAgent, Password);
                    await userManager.AddToRoleAsync(vendorAgent, AppRoles.AGENT.ToString());
                    //var vendorAgentRole = new ApplicationRole(AppRoles.AGENT.ToString(), AppRoles.AGENT.ToString());
                    //vendorAgent.ApplicationRoles.Add(vendorAgentRole);
                }
            }
        }
    }
}
