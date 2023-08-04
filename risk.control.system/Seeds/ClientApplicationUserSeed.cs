using Microsoft.AspNetCore.Identity;
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
            var caMailBox = new Mailbox
            {
                Name = ADMIN.EMAIL
            };
            var clientAdmin = new ClientCompanyApplicationUser()
            {
                Mailbox = caMailBox,
                UserName = ADMIN.USERNAME,
                Email = ADMIN.EMAIL,
                FirstName = ADMIN.FIRST_NAME,
                LastName = ADMIN.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                IsSuperAdmin = false,
                IsClientAdmin = true,
                PhoneNumber = "9876543210",
                Addressline = "123 Agra Road",
                IsVendorAdmin = false,
                ClientCompanyId = clientCompanyId,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = ADMIN.PROFILE_IMAGE
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
            var ccMailBox = new Mailbox
            {
                Name = CREATOR.EMAIL
            };
            var clientCreator = new ClientCompanyApplicationUser()
            {
                Mailbox = ccMailBox,
                UserName = CREATOR.USERNAME,
                Email = CREATOR.EMAIL,
                FirstName = CREATOR.FIRST_NAME,
                LastName = CREATOR.LAST_NAME,
                EmailConfirmed = true,
                Password = Password,
                ClientCompanyId = clientCompanyId,
                PhoneNumberConfirmed = true,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                Addressline = "987 Kanpur Road",
                PhoneNumber = "9876543210",
                IsVendorAdmin = false,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = CREATOR.PROFILE_IMAGE
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
            var asMailBox = new Mailbox
            {
                Name = ASSIGNER.EMAIL
            };
            var clientAssigner = new ClientCompanyApplicationUser()
            {
                Mailbox = asMailBox,
                UserName = ASSIGNER.USERNAME,
                Email = ASSIGNER.EMAIL,
                FirstName = ASSIGNER.FIRST_NAME,
                LastName = ASSIGNER.LAST_NAME,
                EmailConfirmed = true,
                ClientCompanyId = clientCompanyId,
                PhoneNumberConfirmed = true,
                Password = Password,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                Addressline = "453 Lucknow Road",
                PhoneNumber = "9876543210",
                IsVendorAdmin = false,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = ASSIGNER.PROFILE_IMAGE
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
            var ssMailBox = new Mailbox
            {
                Name = ASSESSOR.EMAIL
            };
            var clientAssessor = new ClientCompanyApplicationUser()
            {
                Mailbox = ssMailBox,
                UserName = ASSESSOR.USERNAME,
                Email = ASSESSOR.EMAIL,
                FirstName = ASSESSOR.FIRST_NAME,
                LastName = ASSESSOR.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                ClientCompanyId = clientCompanyId,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                IsVendorAdmin = false,
                Addressline = "453 Patna Road",
                PhoneNumber = "9876543210",
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = ASSESSOR.PROFILE_IMAGE
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