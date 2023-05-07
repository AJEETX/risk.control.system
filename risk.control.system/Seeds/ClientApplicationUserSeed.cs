using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
            var clientAdmin = new ClientCompanyApplicationUser()
            {
                UserName = CLIENT_ADMIN.USERNAME,
                Email = CLIENT_ADMIN.EMAIL,
                FirstName = CLIENT_ADMIN.FIRST_NAME,
                LastName = CLIENT_ADMIN.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                isSuperAdmin = false,
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
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ClientCreator.ToString());
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ClientAssigner.ToString());
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ClientAssessor.ToString());
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.VendorAdmin.ToString());
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.VendorSupervisor.ToString());
                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.VendorAgent.ToString());
                }
            }

            //Seed client creator
            var clientCreator = new ClientCompanyApplicationUser()
            {
                UserName = CLIENT_CREATOR.USERNAME,
                Email = CLIENT_CREATOR.EMAIL,
                FirstName = CLIENT_CREATOR.FIRST_NAME,
                LastName = CLIENT_CREATOR.LAST_NAME,
                EmailConfirmed = true,
                Password = Password,
                ClientCompanyId = clientCompanyId,
                PhoneNumberConfirmed = true,
                isSuperAdmin = false,
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
                }
            }

            //Seed client assigner
            var clientAssigner = new ClientCompanyApplicationUser()
            {
                UserName = CLIENT_ASSIGNER.USERNAME,
                Email = CLIENT_ASSIGNER.EMAIL,
                FirstName = CLIENT_ASSIGNER.FIRST_NAME,
                LastName = CLIENT_ASSIGNER.LAST_NAME,
                EmailConfirmed = true,
                ClientCompanyId = clientCompanyId,
                PhoneNumberConfirmed = true,
                Password = Password,
                isSuperAdmin = false,
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
                }
            }

            //Seed client assessor
            var clientAssessor = new ClientCompanyApplicationUser()
            {
                UserName = CLIENT_ASSESSOR.USERNAME,
                Email = CLIENT_ASSESSOR.EMAIL,
                FirstName = CLIENT_ASSESSOR.FIRST_NAME,
                LastName = CLIENT_ASSESSOR.LAST_NAME,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = Password,
                ClientCompanyId = clientCompanyId,
                isSuperAdmin = false,
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
                }
            }
        }
    }
}
