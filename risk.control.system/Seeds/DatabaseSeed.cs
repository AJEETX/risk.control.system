using Microsoft.AspNetCore.Identity;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public static class DatabaseSeed
    {
        public static async Task SeedDatabase(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var vendorUserManager = scope.ServiceProvider.GetRequiredService<UserManager<VendorApplicationUser>>();
            var clientUserManager = scope.ServiceProvider.GetRequiredService<UserManager<ClientCompanyApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            context.Database.EnsureCreated();

            //check for users
            if (context.ApplicationUser.Any())
            {
                return; //if user is not empty, DB has been seed
            }

            //CREATE ROLES
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.PortalAdmin.ToString().Substring(0, 2).ToUpper(), AppRoles.PortalAdmin.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.ClientAdmin.ToString().Substring(0, 2).ToUpper(), AppRoles.ClientAdmin.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.VendorAdmin.ToString().Substring(0, 2).ToUpper(), AppRoles.VendorAdmin.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.ClientCreator.ToString().Substring(0, 2).ToUpper(), AppRoles.ClientCreator.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.ClientAssigner.ToString().Substring(0, 2).ToUpper(), AppRoles.ClientAssigner.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.ClientAssessor.ToString().Substring(0, 2).ToUpper(), AppRoles.ClientAssessor.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.VendorSupervisor.ToString().Substring(0, 2).ToUpper(), AppRoles.VendorSupervisor.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.VendorAgent.ToString().Substring(0, 2).ToUpper(), AppRoles.VendorAgent.ToString()));

            var india = new Country
            {
                Name = "INDIA",
                Code = "IND",
            };
            var indiaCountry = await context.Country.AddAsync(india);

            await PinCodeStateSeed.SeedPincode(context, indiaCountry.Entity);

            await context.SaveChangesAsync(null, false);

            #region LINE OF BUSINESS

            var claims = new LineOfBusiness
            {
                Name = "CLAIMS",
                Code = "CLAIMS",
                MasterData = true,
            };

            var claimCaseType = await context.LineOfBusiness.AddAsync(claims);

            var underwriting = new LineOfBusiness
            {
                Name = "UNDERWRITING",
                Code = "UNDERWRITING",
                MasterData = true,
            };

            var underwritingCaseType = await context.LineOfBusiness.AddAsync(underwriting);

            #endregion

            #region INVESTIGATION SERVICE TYPES

            var claimComprehensive = new InvestigationServiceType
            {
                Name = "COMPREHENSIVE",
                Code = "COMPREHENSIVE",
                MasterData = true,
                LineOfBusinessId = claimCaseType.Entity.LineOfBusinessId
            };
            var claimComprehensiveService = await context.InvestigationServiceType.AddAsync(claimComprehensive);


            var claimNonComprehensive = new InvestigationServiceType
            {
                Name = "NON-COMPREHENSIVE",
                Code = "NON-COMPREHENSIVE",
                MasterData = true,
                LineOfBusinessId = claimCaseType.Entity.LineOfBusinessId
            };

            var claimNonComprehensiveService = await context.InvestigationServiceType.AddAsync(claimNonComprehensive);


            var claimDocumentCollection = new InvestigationServiceType
            {
                Name = "DOCUMENT-COLLECTION",
                Code = "DOCUMENT-COLLECTION",
                MasterData = true,
                LineOfBusinessId = claimCaseType.Entity.LineOfBusinessId
            };

            var claimDocumentCollectionService = await context.InvestigationServiceType.AddAsync(claimDocumentCollection);


            var claimDiscreet = new InvestigationServiceType
            {
                Name = "DISCREET",
                Code = "DISCREET",
                MasterData = true,
                LineOfBusinessId = claimCaseType.Entity.LineOfBusinessId
            };

            var claimDiscreetService = await context.InvestigationServiceType.AddAsync(claimDiscreet);


            var underWritingPreVerification = new InvestigationServiceType
            {
                Name = "PRE-ONBOARDING-VERIFICATION",
                Code = "PRE-ONBOARDING-VERIFICATION",
                MasterData = true,
                LineOfBusinessId = underwritingCaseType.Entity.LineOfBusinessId
            };

            var underWritingPreVerificationService = await context.InvestigationServiceType.AddAsync(underWritingPreVerification);


            var underWritingPostVerification = new InvestigationServiceType
            {
                Name = "POST-ONBOARDING-VERIFICATION",
                Code = "POST-ONBOARDING-VERIFICATION",
                MasterData = true,
                LineOfBusinessId = underwritingCaseType.Entity.LineOfBusinessId
            };

            var underWritingPostVerificationService = await context.InvestigationServiceType.AddAsync(underWritingPostVerification);


            #endregion


            #region //CREATE RISK CASE DETAILS

            var initiated = new InvestigationCaseStatus
            {
                Name = "INITIATED",
                Code = "INITIATED",
                MasterData = true,
            };

            var initiatedStatus = await context.InvestigationCaseStatus.AddAsync(initiated);

            var inProgress = new InvestigationCaseStatus
            {
                Name = "IN-PROGRESS",
                Code = "IN-PROGRESS",
                MasterData = true,
            };

            var inProgressStatus = await context.InvestigationCaseStatus.AddAsync(inProgress);

            var finished = new InvestigationCaseStatus
            {
                Name = "FINISHED",
                Code = "FINISHED",
                MasterData = true,
            };

            var finishedStatus = await context.InvestigationCaseStatus.AddAsync(finished);
            var created = new InvestigationCaseSubStatus
            {
                Name = "CREATED",
                Code = "CREATED",
                MasterData = true,
                InvestigationCaseStatusId = initiatedStatus.Entity.InvestigationCaseStatusId
            };
            var createdSubStatus = await context.InvestigationCaseSubStatus.AddAsync(created);
            var assigned = new InvestigationCaseSubStatus
            {
                Name = "ASSIGNED_TO_ASSIGNER",
                Code = "ASSIGNED_TO_ASSIGNER",
                MasterData = true,
                InvestigationCaseStatusId = inProgressStatus.Entity.InvestigationCaseStatusId
            };

            var assignedSubStatus = await context.InvestigationCaseSubStatus.AddAsync(assigned);

            var rejected = new InvestigationCaseSubStatus
            {
                Name = "REJECTED_BY_ASSESSOR",
                Code = "REJECTED_BY_ASSESSOR",
                MasterData = true,
                InvestigationCaseStatusId = inProgressStatus.Entity.InvestigationCaseStatusId
            };

            var rejectedSubStatus = await context.InvestigationCaseSubStatus.AddAsync(rejected);
            var accepted = new InvestigationCaseSubStatus
            {
                Name = "ACCEPTED_BY_ASSESSOR",
                Code = "ACCEPTED_BY_ASSESSOR",
                MasterData = true,
                InvestigationCaseStatusId = inProgressStatus.Entity.InvestigationCaseStatusId
            };
            var approved = new InvestigationCaseSubStatus
            {
                Name = "APPROVED_BY_ASSESSOR",
                Code = "APPROVED_BY_ASSESSOR",
                MasterData = true,
                InvestigationCaseStatusId = inProgressStatus.Entity.InvestigationCaseStatusId
            };

            var released = new InvestigationCaseSubStatus
            {
                Name = "RELEASED_BY_SUPERVISOR",
                Code = "RELEASED_BY_SUPERVISOR",
                MasterData = true,
                InvestigationCaseStatusId = finishedStatus.Entity.InvestigationCaseStatusId
            };
            var acceptedSubStatus = await context.InvestigationCaseSubStatus.AddAsync(released);
            var withdrawn = new InvestigationCaseSubStatus
            {
                Name = "WITHDRAWN",
                Code = "WITHDRAWN",
                MasterData = true,
                InvestigationCaseStatusId = finishedStatus.Entity.InvestigationCaseStatusId
            };

            var withdrawnSubStatus = await context.InvestigationCaseSubStatus.AddAsync(withdrawn);

            #endregion

            #region BENEFICIARY-RELATION

            await ClientCompanySetupSeed.Seed(context);

            #endregion


            #region CLIENT/ VENDOR COMPANY

            var (abcVendorId, xyzVendorId, clientCompanyId) = await ClientVendorSeed.Seed(context, indiaCountry, claimComprehensiveService.Entity, claimCaseType.Entity);

            #endregion

            #region APPLICATION USERS ROLES

            await PortalAdminSeed.Seed(context, indiaCountry, userManager, roleManager);

            await ClientApplicationUserSeed.Seed(context, indiaCountry, clientUserManager, clientCompanyId);

            await VendorApplicationUserSeed.Seed(context, indiaCountry, vendorUserManager, abcVendorId);

            await VendorApplicationUserSeed.Seed(context, indiaCountry, vendorUserManager, xyzVendorId);

            await context.SaveChangesAsync(null, false);

            #endregion
        }
    }
}
