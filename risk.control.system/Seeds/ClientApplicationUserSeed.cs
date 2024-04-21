using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Seeds
{
    public static class ClientApplicationUserSeed
    {
        public static async Task Seed(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ClientCompanyApplicationUser> userManager, ClientCompany clientCompany)
        {
            //Seed client admin
            var company = context.ClientCompany.FirstOrDefault(c => c.Email == clientCompany.Email);
            string noUserImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", @Applicationsettings.NO_USER);

            string adminEmailwithSuffix = Applicationsettings.ADMIN.CODE + "@" + company.Email;
            var caMailBox = new Mailbox
            {
                Name = adminEmailwithSuffix
            };
            var pinCode = context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault(p => p.Code == CURRENT_PINCODE);
            var district = context.District.FirstOrDefault(c => c.DistrictId == pinCode.District.DistrictId);
            var state = context.State.Include(s => s.Country).FirstOrDefault(s => s.StateId == pinCode.State.StateId);
            var countryId = context.Country.FirstOrDefault(s => s.CountryId == state.Country.CountryId)?.CountryId ?? default!;

            string adminImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "company-admin.jpeg");
            var adminImage = File.ReadAllBytes(adminImagePath);

            if (adminImage == null)
            {
                adminImage = File.ReadAllBytes(noUserImagePath);
            }
            var clientAdmin = new ClientCompanyApplicationUser()
            {
                Mailbox = caMailBox,
                UserName = adminEmailwithSuffix,
                Email = adminEmailwithSuffix,
                FirstName = ADMIN.FIRST_NAME,
                LastName = ADMIN.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                IsSuperAdmin = false,
                IsClientAdmin = true,
                Active = true,
                PhoneNumber = Applicationsettings.PORTAL_ADMIN_MOBILE,
                Addressline = "22 Golden Road",
                IsVendorAdmin = false,
                ClientCompany = clientCompany,
                CountryId = countryId,
                DistrictId = district?.DistrictId ?? default!,
                StateId = state?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = ADMIN.PROFILE_IMAGE,
                ProfilePicture = adminImage,
                UserRole = CompanyRole.ADMIN,
            };
            if (userManager.Users.All(u => u.Id != clientAdmin.Id))
            {
                var user = await userManager.FindByEmailAsync(clientAdmin.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientAdmin, Password);
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ADMIN.ToString());
                    //var clientAdminRole = new ApplicationRole(AppRoles.ADMIN.ToString(), AppRoles.ADMIN.ToString());
                    //clientAdmin.ApplicationRoles.Add(clientAdminRole);

                    //await userManager.AddToRoleAsync(clientAdmin, AppRoles.CREATOR.ToString());
                    //var clientCreatorRole = new ApplicationRole(AppRoles.CREATOR.ToString(), AppRoles.CREATOR.ToString());
                    //clientAdmin.ApplicationRoles.Add(clientCreatorRole);

                    //await userManager.AddToRoleAsync(clientAdmin, AppRoles.Assigner.ToString());
                    //var clientAssignerRole = new ApplicationRole(AppRoles.Assigner.ToString(), AppRoles.Assigner.ToString());
                    //clientAdmin.ApplicationRoles.Add(clientAssignerRole);

                    //await userManager.AddToRoleAsync(clientAdmin, AppRoles.ASSESSOR.ToString());
                    //var clientAssessorRole = new ApplicationRole(AppRoles.ASSESSOR.ToString(), AppRoles.ASSESSOR.ToString());
                    //clientAdmin.ApplicationRoles.Add(clientAssessorRole);
                }
            }

            //Seed client creator
            string creatorEmailwithSuffix = Applicationsettings.CREATOR.CODE + "@" + company.Email;
            var ccMailBox = new Mailbox
            {
                Name = creatorEmailwithSuffix
            };

            string creatorImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "creator.jpeg");

            var creatorImage = File.ReadAllBytes(creatorImagePath);

            if (creatorImage == null)
            {
                creatorImage = File.ReadAllBytes(noUserImagePath);
            }
            var clientCreator = new ClientCompanyApplicationUser()
            {
                Mailbox = ccMailBox,
                UserName = creatorEmailwithSuffix,
                Email = creatorEmailwithSuffix,
                FirstName = CREATOR.FIRST_NAME,
                LastName = CREATOR.LAST_NAME,
                Active = true,
                EmailConfirmed = true,
                Password = Password,
                ClientCompany = clientCompany,
                PhoneNumberConfirmed = true,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                Addressline = "987 Canterbury Road",
                PhoneNumber = Applicationsettings.PORTAL_ADMIN_MOBILE,
                IsVendorAdmin = false,
                CountryId = countryId,
                DistrictId = district?.DistrictId ?? default!,
                StateId = state?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = CREATOR.PROFILE_IMAGE,
                ProfilePicture = creatorImage,
                UserRole = CompanyRole.CREATOR,
            };
            if (userManager.Users.All(u => u.Id != clientCreator.Id))
            {
                var user = await userManager.FindByEmailAsync(clientCreator.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientCreator, Password);
                    await userManager.AddToRoleAsync(clientCreator, AppRoles.CREATOR.ToString());
                    //var clientCreatorRole = new ApplicationRole(AppRoles.CREATOR.ToString(), AppRoles.CREATOR.ToString());
                    //clientCreator.ApplicationRoles.Add(clientCreatorRole);
                }
            }

            //Seed client assigner
            string managererEmailwithSuffix = Applicationsettings.MANAGER.CODE + "@" + company.Email;
            var asMailBox = new Mailbox
            {
                Name = managererEmailwithSuffix
            };
            string managerImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "assigner.jpeg");

            var managerImage = File.ReadAllBytes(managerImagePath);

            if (managerImage == null)
            {
                managerImage = File.ReadAllBytes(noUserImagePath);
            }

            var manager = new ClientCompanyApplicationUser()
            {
                Mailbox = asMailBox,
                UserName = managererEmailwithSuffix,
                Email = managererEmailwithSuffix,
                FirstName = MANAGER.FIRST_NAME,
                LastName = MANAGER.LAST_NAME,
                Active = true,
                EmailConfirmed = true,
                ClientCompany = clientCompany,
                PhoneNumberConfirmed = true,
                Password = Password,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                Addressline = "453 Main Road",
                PhoneNumber = Applicationsettings.PORTAL_ADMIN_MOBILE,
                IsVendorAdmin = false,
                CountryId = countryId,
                DistrictId = district?.DistrictId ?? default!,
                StateId = state?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = MANAGER.PROFILE_IMAGE,
                ProfilePicture = managerImage,
                UserRole = CompanyRole.MANAGER,
            };
            if (userManager.Users.All(u => u.Id != manager.Id))
            {
                var user = await userManager.FindByEmailAsync(manager.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(manager, Password);
                    await userManager.AddToRoleAsync(manager, AppRoles.MANAGER.ToString());
                    //var clientAssignerRole = new ApplicationRole(AppRoles.Assigner.ToString(), AppRoles.Assigner.ToString());
                    //clientAssigner.ApplicationRoles.Add(clientAssignerRole);
                }
            }

            //Seed client assessor
            string assessorEmailwithSuffix = Applicationsettings.ASSESSOR.CODE + "@" + company.Email;
            var ssMailBox = new Mailbox
            {
                Name = assessorEmailwithSuffix
            };

            string assessorImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "assessor.jpeg");
            var assessorImage = File.ReadAllBytes(assessorImagePath);

            if (assessorImage == null)
            {
                assessorImage = File.ReadAllBytes(noUserImagePath);
            }

            var clientAssessor = new ClientCompanyApplicationUser()
            {
                Mailbox = ssMailBox,
                UserName = assessorEmailwithSuffix,
                Email = assessorEmailwithSuffix,
                FirstName = ASSESSOR.FIRST_NAME,
                LastName = ASSESSOR.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                Active = true,
                ClientCompany = clientCompany,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                IsVendorAdmin = false,
                IsClientManager = true,
                Addressline = "11 Nurlendi Street",
                PhoneNumber = Applicationsettings.PORTAL_ADMIN_MOBILE,
                CountryId = countryId,
                DistrictId = district?.DistrictId ?? default!,
                StateId = state?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = ASSESSOR.PROFILE_IMAGE,
                ProfilePicture = assessorImage,
                UserRole = CompanyRole.ASSESSOR,
            };
            if (userManager.Users.All(u => u.Id != clientAssessor.Id))
            {
                var user = await userManager.FindByEmailAsync(clientAssessor.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientAssessor, Password);
                    await userManager.AddToRoleAsync(clientAssessor, AppRoles.ASSESSOR.ToString());
                    //var clientAssessorRole = new ApplicationRole(AppRoles.ASSESSOR.ToString(), AppRoles.ASSESSOR.ToString());
                    //clientAssigner.ApplicationRoles.Add(clientAssessorRole);
                }
            }
        }
    }
}