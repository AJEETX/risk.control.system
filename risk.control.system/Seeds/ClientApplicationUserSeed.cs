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
                Name = CLIENT_ADMIN.EMAIL
            };
            var clientAdmin = new ClientCompanyApplicationUser()
            {
                Mailbox = caMailBox,
                UserName = CLIENT_ADMIN.USERNAME,
                Email = CLIENT_ADMIN.EMAIL,
                FirstName = CLIENT_ADMIN.FIRST_NAME,
                LastName = CLIENT_ADMIN.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                IsSuperAdmin = false,
                IsClientAdmin = true,
                IsVendorAdmin = false,
                ClientCompanyId = clientCompanyId,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = CLIENT_ADMIN.PROFILE_IMAGE
            };
            if (userManager.Users.All(u => u.Id != clientAdmin.Id))
            {
                var user = await userManager.FindByEmailAsync(clientAdmin.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientAdmin, Password);
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ClientAdmin.ToString());
                    var clientAdminRole = new ApplicationRole(AppRoles.ClientAdmin.ToString(), AppRoles.ClientAdmin.ToString());
                    clientAdmin.ApplicationRoles.Add(clientAdminRole);


                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ClientCreator.ToString());
                    var clientCreatorRole = new ApplicationRole(AppRoles.ClientCreator.ToString(), AppRoles.ClientCreator.ToString());
                    clientAdmin.ApplicationRoles.Add(clientCreatorRole);

                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ClientAssigner.ToString());
                    var clientAssignerRole = new ApplicationRole(AppRoles.ClientAssigner.ToString(), AppRoles.ClientAssigner.ToString());
                    clientAdmin.ApplicationRoles.Add(clientAssignerRole);

                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ClientAssessor.ToString());
                    var clientAssessorRole = new ApplicationRole(AppRoles.ClientAssessor.ToString(), AppRoles.ClientAssessor.ToString());
                    clientAdmin.ApplicationRoles.Add(clientAssessorRole);

                    //await userManager.AddToRoleAsync(clientAdmin, AppRoles.VendorAdmin.ToString());

                    //await userManager.AddToRoleAsync(clientAdmin, AppRoles.VendorSupervisor.ToString());
                    //await userManager.AddToRoleAsync(clientAdmin, AppRoles.VendorAgent.ToString());
                }
            }


            //Seed client creator
            var ccMailBox = new Mailbox
            {
                Name = CLIENT_CREATOR.EMAIL
            };
            var clientCreator = new ClientCompanyApplicationUser()
            {
                Mailbox = ccMailBox,
                UserName = CLIENT_CREATOR.USERNAME,
                Email = CLIENT_CREATOR.EMAIL,
                FirstName = CLIENT_CREATOR.FIRST_NAME,
                LastName = CLIENT_CREATOR.LAST_NAME,
                EmailConfirmed = true,
                Password = Password,
                ClientCompanyId = clientCompanyId,
                PhoneNumberConfirmed = true,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                IsVendorAdmin = false,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = CLIENT_CREATOR.PROFILE_IMAGE
            };
            if (userManager.Users.All(u => u.Id != clientCreator.Id))
            {
                var user = await userManager.FindByEmailAsync(clientCreator.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientCreator, Password);
                    await userManager.AddToRoleAsync(clientCreator, AppRoles.ClientCreator.ToString());
                    var clientCreatorRole = new ApplicationRole(AppRoles.ClientCreator.ToString(), AppRoles.ClientCreator.ToString());
                    clientCreator.ApplicationRoles.Add(clientCreatorRole);
                }
            }

            //Seed client assigner
            var asMailBox = new Mailbox
            {
                Name = CLIENT_ASSIGNER.EMAIL
            };
            var clientAssigner = new ClientCompanyApplicationUser()
            {
                Mailbox = asMailBox,
                UserName = CLIENT_ASSIGNER.USERNAME,
                Email = CLIENT_ASSIGNER.EMAIL,
                FirstName = CLIENT_ASSIGNER.FIRST_NAME,
                LastName = CLIENT_ASSIGNER.LAST_NAME,
                EmailConfirmed = true,
                ClientCompanyId = clientCompanyId,
                PhoneNumberConfirmed = true,
                Password = Password,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                IsVendorAdmin = false,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = CLIENT_ASSIGNER.PROFILE_IMAGE
            };
            if (userManager.Users.All(u => u.Id != clientAssigner.Id))
            {
                var user = await userManager.FindByEmailAsync(clientAssigner.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientAssigner, Password);
                    await userManager.AddToRoleAsync(clientAssigner, AppRoles.ClientAssigner.ToString());
                    var clientAssignerRole = new ApplicationRole(AppRoles.ClientAssigner.ToString(), AppRoles.ClientAssigner.ToString());
                    clientAssigner.ApplicationRoles.Add(clientAssignerRole);
                }
            }

            //Seed client assessor
            var ssMailBox = new Mailbox
            {
                Name = CLIENT_ASSESSOR.EMAIL
            };
            var clientAssessor = new ClientCompanyApplicationUser()
            {
                Mailbox = ssMailBox,
                UserName = CLIENT_ASSESSOR.USERNAME,
                Email = CLIENT_ASSESSOR.EMAIL,
                FirstName = CLIENT_ASSESSOR.FIRST_NAME,
                LastName = CLIENT_ASSESSOR.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                ClientCompanyId = clientCompanyId,
                IsSuperAdmin = false,
                IsClientAdmin = false,
                IsVendorAdmin = false,
                CountryId = indiaCountry.Entity.CountryId,
                DistrictId = context.District.FirstOrDefault(s => s.Name == CURRENT_DISTRICT)?.DistrictId ?? default!,
                StateId = context.State.FirstOrDefault(s => s.Code.StartsWith(CURRENT_STATE))?.StateId ?? default!,
                PinCodeId = context.PinCode.FirstOrDefault(s => s.Code == CURRENT_PINCODE)?.PinCodeId ?? default!,
                ProfilePictureUrl = CLIENT_ASSESSOR.PROFILE_IMAGE
            };
            if (userManager.Users.All(u => u.Id != clientAssessor.Id))
            {
                var user = await userManager.FindByEmailAsync(clientAssessor.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(clientAssessor, Password);
                    await userManager.AddToRoleAsync(clientAssessor, AppRoles.ClientAssessor.ToString());
                    var clientAssessorRole = new ApplicationRole(AppRoles.ClientAssessor.ToString(), AppRoles.ClientAssessor.ToString());
                    clientAssigner.ApplicationRoles.Add(clientAssessorRole);
                }
            }
        }
    }
}
