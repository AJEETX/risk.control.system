using Microsoft.AspNetCore.Identity;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public static class DatabaseSeed
    {
        public static async Task SeedDatabase(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var webHostEnvironment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var httpClientService = scope.ServiceProvider.GetRequiredService<IHttpClientService>();
            var vendorUserManager = scope.ServiceProvider.GetRequiredService<UserManager<VendorApplicationUser>>();
            var clientUserManager = scope.ServiceProvider.GetRequiredService<UserManager<ClientCompanyApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            //context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            //check for users
            if (context.ApplicationUser.Any())
            {
                return; //if user is not empty, DB has been seed
            }

            //CREATE ROLES
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.PORTAL_ADMIN.ToString().Substring(0, 2).ToUpper(), AppRoles.PORTAL_ADMIN.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.ADMIN.ToString().Substring(0, 2).ToUpper(), AppRoles.ADMIN.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.AGENCY_ADMIN.ToString().Substring(0, 2).ToUpper(), AppRoles.AGENCY_ADMIN.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.CREATOR.ToString().Substring(0, 2).ToUpper(), AppRoles.CREATOR.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.MANAGER.ToString().Substring(0, 2).ToUpper(), AppRoles.MANAGER.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.ASSESSOR.ToString().Substring(0, 2).ToUpper(), AppRoles.ASSESSOR.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.SUPERVISOR.ToString().Substring(0, 2).ToUpper(), AppRoles.SUPERVISOR.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(AppRoles.AGENT.ToString().Substring(0, 2).ToUpper(), AppRoles.AGENT.ToString()));

            var australia = new Country
            {
                Name = "AUSTRALIA",
                Code = "AU",
            };
            var australiaCountry = await context.Country.AddAsync(australia);

            await PinCodeStateSeed.SeedPincode(context, australiaCountry.Entity);

            var india = new Country
            {
                Name = "INDIA",
                Code = "IND",
            };
            var indiaCountry = await context.Country.AddAsync(india);

            await PinCodeStateSeed.SeedPincode_India(context, indiaCountry.Entity);

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

            #endregion LINE OF BUSINESS

            #region INVESTIGATION SERVICE TYPES

            var claimComprehensive = new InvestigationServiceType
            {
                Name = "COMPREHENSIVE",
                Code = "COMP",
                MasterData = true,
                LineOfBusiness = claimCaseType.Entity
            };
            var claimComprehensiveService = await context.InvestigationServiceType.AddAsync(claimComprehensive);

            var claimNonComprehensive = new InvestigationServiceType
            {
                Name = "NON-COMPREHENSIVE",
                Code = "NON-COMP",
                MasterData = true,
                LineOfBusiness = claimCaseType.Entity
            };

            var claimNonComprehensiveService = await context.InvestigationServiceType.AddAsync(claimNonComprehensive);

            var claimDocumentCollection = new InvestigationServiceType
            {
                Name = "DOCUMENT-COLLECTION",
                Code = "DOC",
                MasterData = true,
                LineOfBusiness = claimCaseType.Entity
            };

            var claimDocumentCollectionService = await context.InvestigationServiceType.AddAsync(claimDocumentCollection);

            var claimDiscreet = new InvestigationServiceType
            {
                Name = "DISCREET",
                Code = "DISCREET",
                MasterData = true,
                LineOfBusiness = claimCaseType.Entity
            };

            var claimDiscreetService = await context.InvestigationServiceType.AddAsync(claimDiscreet);

            var underWritingPreVerification = new InvestigationServiceType
            {
                Name = "PRE-ONBOARDING-VERIFICATION",
                Code = "PRE-OV",
                MasterData = true,
                LineOfBusiness = underwritingCaseType.Entity
            };

            var underWritingPreVerificationService = await context.InvestigationServiceType.AddAsync(underWritingPreVerification);

            var underWritingPostVerification = new InvestigationServiceType
            {
                Name = "POST-ONBOARDING-VERIFICATION",
                Code = "POST-OV",
                MasterData = true,
                LineOfBusiness = underwritingCaseType.Entity
            };

            var underWritingPostVerificationService = await context.InvestigationServiceType.AddAsync(underWritingPostVerification);

            #endregion INVESTIGATION SERVICE TYPES

            #region //CREATE RISK CASE DETAILS

            //CASE STATUS

            var initiated = new InvestigationCaseStatus
            {
                Name = CONSTANTS.CASE_STATUS.INITIATED,
                Code = CONSTANTS.CASE_STATUS.INITIATED,
                MasterData = true,
            };

            var initiatedStatus = await context.InvestigationCaseStatus.AddAsync(initiated);

            var inProgress = new InvestigationCaseStatus
            {
                Name = CONSTANTS.CASE_STATUS.INPROGRESS,
                Code = CONSTANTS.CASE_STATUS.INPROGRESS,
                MasterData = true,
            };

            var inProgressStatus = await context.InvestigationCaseStatus.AddAsync(inProgress);

            var finished = new InvestigationCaseStatus
            {
                Name = CONSTANTS.CASE_STATUS.FINISHED,
                Code = CONSTANTS.CASE_STATUS.FINISHED,
                MasterData = true,
            };

            var finishedStatus = await context.InvestigationCaseStatus.AddAsync(finished);

            //CASE SUBSTATUS

            var created = new InvestigationCaseSubStatus
            {
                Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
                Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
                MasterData = true,
                InvestigationCaseStatus = initiatedStatus.Entity
            };
            var createdSubStatus = await context.InvestigationCaseSubStatus.AddAsync(created);

            var edited = new InvestigationCaseSubStatus
            {
                Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR,
                Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR,
                MasterData = true,
                InvestigationCaseStatus = initiatedStatus.Entity
            };
            var editedSubStatus = await context.InvestigationCaseSubStatus.AddAsync(edited);

            var assigned = new InvestigationCaseSubStatus
            {
                Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                MasterData = true,
                InvestigationCaseStatus = inProgressStatus.Entity
            };

            var assignedSubStatus = await context.InvestigationCaseSubStatus.AddAsync(assigned);

            var allocated = new InvestigationCaseSubStatus
            {
                Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
                MasterData = true,
                InvestigationCaseStatus = inProgressStatus.Entity
            };

            var allocatedSubStatus = await context.InvestigationCaseSubStatus.AddAsync(allocated);

            var withdrawn = new InvestigationCaseSubStatus
            {
                Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                MasterData = true,
                InvestigationCaseStatus = inProgressStatus.Entity
            };

            var withdrawnSubStatus = await context.InvestigationCaseSubStatus.AddAsync(withdrawn);

            var assignedToAgent = new InvestigationCaseSubStatus
            {
                Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
                MasterData = true,
                InvestigationCaseStatus = inProgressStatus.Entity
            };

            var assignedToAgentSubStatus = await context.InvestigationCaseSubStatus.AddAsync(assignedToAgent);

            var submittedtoSupervisor = new InvestigationCaseSubStatus
            {
                Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
                MasterData = true,
                InvestigationCaseStatus = inProgressStatus.Entity
            };

            var submittedtoSupervisorSubStatus = await context.InvestigationCaseSubStatus.AddAsync(submittedtoSupervisor);

            var submittedToAssessor = new InvestigationCaseSubStatus
            {
                Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR,
                Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR,
                MasterData = true,
                InvestigationCaseStatus = inProgressStatus.Entity
            };

            var submittedToAssessorSubStatus = await context.InvestigationCaseSubStatus.AddAsync(submittedToAssessor);

            var approved = new InvestigationCaseSubStatus
            {
                Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR,
                Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR,
                MasterData = true,
                InvestigationCaseStatus = finishedStatus.Entity
            };

            var approvededSubStatus = await context.InvestigationCaseSubStatus.AddAsync(approved);

            var rejected = new InvestigationCaseSubStatus
            {
                Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR,
                Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR,
                MasterData = true,
                InvestigationCaseStatus = finishedStatus.Entity
            };

            var rejectedSubStatus = await context.InvestigationCaseSubStatus.AddAsync(rejected);

            var reassigned = new InvestigationCaseSubStatus
            {
                Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER,
                Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER,
                MasterData = true,
                InvestigationCaseStatus = finishedStatus.Entity
            };
            var acceptedSubStatus = await context.InvestigationCaseSubStatus.AddAsync(reassigned);

            var withdrawnByCompany = new InvestigationCaseSubStatus
            {
                Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY,
                Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY,
                MasterData = true,
                InvestigationCaseStatus = finishedStatus.Entity
            };

            var withdrawnByCompanySubStatus = await context.InvestigationCaseSubStatus.AddAsync(withdrawnByCompany);

            #endregion //CREATE RISK CASE DETAILS

            #region BENEFICIARY-RELATION

            await ClientCompanySetupSeed.Seed(context);

            #endregion BENEFICIARY-RELATION

            #region CLIENT/ VENDOR COMPANY

            var (vendors, companyIds) = await ClientVendorSeed.Seed(context, claimComprehensiveService.Entity,
                claimDiscreetService.Entity, claimDocumentCollectionService.Entity, claimCaseType.Entity);

            #endregion CLIENT/ VENDOR COMPANY

            #region PERMISSIONS ROLES

            //PermissionModuleSeed.SeedMailbox(context);

            //PermissionModuleSeed.SeedClaim(context);

            #endregion PERMISSIONS ROLES

            #region APPLICATION USERS ROLES

            await PortalAdminSeed.Seed(context, webHostEnvironment, indiaCountry, userManager, roleManager);

            foreach (var companyId in companyIds)
            {
                await ClientApplicationUserSeed.Seed(context, webHostEnvironment, clientUserManager, companyId);
            }
            await context.SaveChangesAsync(null, false);

            foreach (var vendor in vendors)
            {
                await VendorApplicationUserSeed.Seed(context, webHostEnvironment, vendorUserManager, vendor);
            }

            await context.SaveChangesAsync(null, false);

            #endregion APPLICATION USERS ROLES
        }
    }
}