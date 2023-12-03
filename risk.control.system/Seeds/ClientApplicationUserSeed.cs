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
        public static async Task Seed(ApplicationDbContext context, EntityEntry<Country> indiaCountry, UserManager<ClientCompanyApplicationUser> userManager, string clientCompanyId)
        {
            //Seed client admin
            var company = context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == clientCompanyId);

            string adminEmailwithSuffix = Applicationsettings.ADMIN.CODE + "@" + company.Email;
            var caMailBox = new Mailbox
            {
                Name = adminEmailwithSuffix
            };
            var pinCode = context.PinCode.Include(p => p.District).Include(p => p.State).FirstOrDefault(p => p.Code == CURRENT_PINCODE);
            var district = context.District.FirstOrDefault(c => c.DistrictId == pinCode.District.DistrictId);
            var state = context.State.FirstOrDefault(s => s.StateId == pinCode.State.StateId);
            var adminImage = File.ReadAllBytes(ADMIN.PROFILE_IMAGE);

            if (adminImage == null)
            {
                adminImage = File.ReadAllBytes(Applicationsettings.NO_IMAGE);
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
                PhoneNumber = Applicationsettings.MOBILE,
                Addressline = "43 Golden Road",
                IsVendorAdmin = false,
                ClientCompanyId = clientCompanyId,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = district?.DistrictId ?? default!,
                StateId = state?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = ADMIN.PROFILE_IMAGE,
                ProfilePicture = adminImage
            };
            if (userManager.Users.All(u => u.Id != clientAdmin.Id))
            {
                var user = await userManager.FindByEmailAsync(clientAdmin.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientAdmin, Password);
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.CompanyAdmin.ToString());
                    //var clientAdminRole = new ApplicationRole(AppRoles.CompanyAdmin.ToString(), AppRoles.CompanyAdmin.ToString());
                    //clientAdmin.ApplicationRoles.Add(clientAdminRole);

                    //await userManager.AddToRoleAsync(clientAdmin, AppRoles.Creator.ToString());
                    //var clientCreatorRole = new ApplicationRole(AppRoles.Creator.ToString(), AppRoles.Creator.ToString());
                    //clientAdmin.ApplicationRoles.Add(clientCreatorRole);

                    //await userManager.AddToRoleAsync(clientAdmin, AppRoles.Assigner.ToString());
                    //var clientAssignerRole = new ApplicationRole(AppRoles.Assigner.ToString(), AppRoles.Assigner.ToString());
                    //clientAdmin.ApplicationRoles.Add(clientAssignerRole);

                    //await userManager.AddToRoleAsync(clientAdmin, AppRoles.Assessor.ToString());
                    //var clientAssessorRole = new ApplicationRole(AppRoles.Assessor.ToString(), AppRoles.Assessor.ToString());
                    //clientAdmin.ApplicationRoles.Add(clientAssessorRole);
                }
            }

            //Seed client creator
            string creatorEmailwithSuffix = Applicationsettings.CREATOR.CODE + "@" + company.Email;
            var ccMailBox = new Mailbox
            {
                Name = creatorEmailwithSuffix
            };
            var investigatePinCode = context.PinCode.Include(p => p.District).FirstOrDefault(s => s.Code == Applicationsettings.CURRENT_PINCODE4);
            var investigateDistrict = context.District.Include(d => d.State).FirstOrDefault(s => s.DistrictId == investigatePinCode.District.DistrictId);
            var investigateState = context.State.FirstOrDefault(s => s.StateId == investigateDistrict.State.StateId);

            var creatorImage = File.ReadAllBytes(CREATOR.PROFILE_IMAGE);

            if (creatorImage == null)
            {
                creatorImage = File.ReadAllBytes(Applicationsettings.NO_IMAGE);
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
                ClientCompanyId = clientCompanyId,
                PhoneNumberConfirmed = true,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                Addressline = "987 Canterbury Road",
                PhoneNumber = Applicationsettings.MOBILE,
                IsVendorAdmin = false,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = investigateDistrict?.DistrictId ?? default!,
                StateId = investigateState?.StateId ?? default!,
                PinCodeId = investigatePinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = CREATOR.PROFILE_IMAGE,
                ProfilePicture = creatorImage
            };
            if (userManager.Users.All(u => u.Id != clientCreator.Id))
            {
                var user = await userManager.FindByEmailAsync(clientCreator.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientCreator, Password);
                    await userManager.AddToRoleAsync(clientCreator, AppRoles.Creator.ToString());
                    //var clientCreatorRole = new ApplicationRole(AppRoles.Creator.ToString(), AppRoles.Creator.ToString());
                    //clientCreator.ApplicationRoles.Add(clientCreatorRole);
                }
            }

            //Seed client assigner
            string assignerEmailwithSuffix = Applicationsettings.ASSIGNER.CODE + "@" + company.Email;
            var asMailBox = new Mailbox
            {
                Name = assignerEmailwithSuffix
            };

            var assignerImage = File.ReadAllBytes(ASSIGNER.PROFILE_IMAGE);

            if (assignerImage == null)
            {
                assignerImage = File.ReadAllBytes(Applicationsettings.NO_IMAGE);
            }

            var clientAssigner = new ClientCompanyApplicationUser()
            {
                Mailbox = asMailBox,
                UserName = assignerEmailwithSuffix,
                Email = assignerEmailwithSuffix,
                FirstName = ASSIGNER.FIRST_NAME,
                LastName = ASSIGNER.LAST_NAME,
                Active = true,
                EmailConfirmed = true,
                ClientCompanyId = clientCompanyId,
                PhoneNumberConfirmed = true,
                Password = Password,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                Addressline = "453 Main Road",
                PhoneNumber = Applicationsettings.MOBILE,
                IsVendorAdmin = false,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = district?.DistrictId ?? default!,
                StateId = state?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = ASSIGNER.PROFILE_IMAGE,
                ProfilePicture = assignerImage
            };
            if (userManager.Users.All(u => u.Id != clientAssigner.Id))
            {
                var user = await userManager.FindByEmailAsync(clientAssigner.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientAssigner, Password);
                    await userManager.AddToRoleAsync(clientAssigner, AppRoles.Assigner.ToString());
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

            var assessorImage = File.ReadAllBytes(ASSESSOR.PROFILE_IMAGE);

            if (assessorImage == null)
            {
                assessorImage = File.ReadAllBytes(Applicationsettings.NO_IMAGE);
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
                ClientCompanyId = clientCompanyId,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                IsVendorAdmin = false,
                Addressline = "11 Nurlendi Street",
                PhoneNumber = Applicationsettings.MOBILE,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = district?.DistrictId ?? default!,
                StateId = state?.StateId ?? default!,
                PinCodeId = pinCode?.PinCodeId ?? default!,
                ProfilePictureUrl = ASSESSOR.PROFILE_IMAGE,
                ProfilePicture = assessorImage
            };
            if (userManager.Users.All(u => u.Id != clientAssessor.Id))
            {
                var user = await userManager.FindByEmailAsync(clientAssessor.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientAssessor, Password);
                    await userManager.AddToRoleAsync(clientAssessor, AppRoles.Assessor.ToString());
                    //var clientAssessorRole = new ApplicationRole(AppRoles.Assessor.ToString(), AppRoles.Assessor.ToString());
                    //clientAssigner.ApplicationRoles.Add(clientAssessorRole);
                }
            }
        }
    }
}