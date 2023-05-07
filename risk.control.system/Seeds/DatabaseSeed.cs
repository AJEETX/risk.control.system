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

            #region COUNTRY STATE PINCODE

            //string indiaStateData = await File.ReadAllTextAsync(stateWisePincodeFilePath);
            //var statesObject = JsonSerializer.Deserialize<List<PinCodeState>>(indiaStateData);
            //bool firstRow = true;
            //foreach (string row in indiaStateData.Split('\n'))
            //{
            //    if (!string.IsNullOrEmpty(row))
            //    {
            //        if(firstRow)
            //        {
            //            firstRow = false;
            //            continue;
            //        }
            //        var rowData = row.Split(',');

            //        if (rowData.Length > 0)
            //        {
            //            var state = new State
            //            {
            //                Code = rowData[0],
            //                Name = rowData[1][..^1],
            //                CountryId = indiaCountry.Entity.CountryId,
            //            };
            //            var stateAdded =  await context.State.AddAsync(state);  
            //        }
            //    }
            //}

            //var up = new State
            //{
            //    CountryId = indiaCountry.Entity.CountryId,
            //    Name = "UTTAR PRADESH",
            //    Code = "UP"
            //};

            //var delhi = new State
            //{
            //    CountryId = indiaCountry.Entity.CountryId,
            //    Name = "NEW DELHI",
            //    Code = "NDL"
            //};

            //var delhiState = await context.State.AddAsync(delhi);
            //var upState = await context.State.AddAsync(up);

            //var newDelhi = new PinCode
            //{
            //    Name = "NEW DELHI",
            //    Code = "110001",
            //    State = delhiState.Entity,
            //    Country = indiaCountry.Entity
            //};

            //var newDelhiPinCode = await context.PinCode.AddAsync(newDelhi);

            //var northDelhi = new PinCode
            //{
            //    Name = "NORTH DELHI",
            //    Code = "110002",
            //    State = delhiState.Entity,
            //    Country = indiaCountry.Entity
            //};

            //var northDelhiPinCode = await context.PinCode.AddAsync(northDelhi);

            //var indirapuram = new PinCode
            //{
            //    Name = "INDIRAPURAM",
            //    Code = "201014",
            //    State = upState.Entity,
            //    Country = indiaCountry.Entity
            //};

            //var indiraPuramPinCode = await context.PinCode.AddAsync(indirapuram);

            //var bhelupur = new PinCode
            //{
            //    Name = "BHELUPUR",
            //    Code = "221001",
            //    State = upState.Entity,
            //    Country = indiaCountry.Entity
            //};

            //var bhelupurPinCode = await context.PinCode.AddAsync(bhelupur);

            //var australia = new Country
            //{
            //    Name = "AUSTRALIA",
            //    Code = "AUS",
            //};

            //var australiaCountry = await context.Country.AddAsync(australia);

            //var victoria = new State
            //{
            //    CountryId = australiaCountry.Entity.CountryId,
            //    Name = "VICTORIA",
            //    Code = "VIC"
            //};

            //var victoriaState = await context.State.AddAsync(victoria);

            //var tasmania = new State
            //{
            //    CountryId = australiaCountry.Entity.CountryId,
            //    Name = "TASMANIA",
            //    Code = "TAS"
            //};

            //var tasmaniaState = await context.State.AddAsync(tasmania);

            //var forestHill = new PinCode
            //{
            //    Name = "FOREST HILL",
            //    Code = "3131",
            //    State = victoriaState.Entity,
            //    Country = australiaCountry.Entity
            //};

            //var forestHillPinCode = await context.PinCode.AddAsync(forestHill);

            //var vermont = new PinCode
            //{
            //    Name = "VERMONT",
            //    Code = "3133",
            //    State = victoriaState.Entity,
            //    Country = australiaCountry.Entity
            //};

            //var vermontPinCode = await context.PinCode.AddAsync(vermont);

            //var tasmaniaCity = new PinCode
            //{
            //    Name = "TASMANIA CITY",
            //    Code = "7000",
            //    State = tasmaniaState.Entity,
            //    Country = australiaCountry.Entity
            //};

            //var tasmaniaCityCode = await context.PinCode.AddAsync(tasmaniaCity);
            //var canada = new Country
            //{
            //    Name = "CANADA",
            //    Code = "CAN",
            //};

            //var canadaCountry = await context.Country.AddAsync(canada);

            //var ontario = new State
            //{
            //    CountryId = canadaCountry.Entity.CountryId,
            //    Name = "ONTARIO",
            //    Code = "ON"
            //};

            //var ontarioState = await context.State.AddAsync(ontario);

            //var torontoCity = new PinCode
            //{
            //    Name = "TORONTO",
            //    Code = "9101",
            //    State = ontarioState.Entity,
            //    Country = canadaCountry.Entity
            //};

            //var torontoCityCode = await context.PinCode.AddAsync(tasmaniaCity);

            #endregion

            #region INVESTIGATION SERVICE TYPES

            var claimComprehensive = new InvestigationServiceType
            {
                Name = "COMPREHENSIVE",
                Code = "COMPREHENSIVE",
            };
            var claimComprehensiveService = await context.InvestigationServiceType.AddAsync(claimComprehensive);


            var claimNonComprehensive = new InvestigationServiceType
            {
                Name = "NON-COMPREHENSIVE",
                Code = "NON-COMPREHENSIVE",
            };

            var claimNonComprehensiveService = await context.InvestigationServiceType.AddAsync(claimNonComprehensive);


            var claimDocumentCollection = new InvestigationServiceType
            {
                Name = "DOCUMENT-COLLECTION",
                Code = "DOCUMENT-COLLECTION",
            };

            var claimDocumentCollectionService = await context.InvestigationServiceType.AddAsync(claimDocumentCollection);


            var claimDiscreet = new InvestigationServiceType
            {
                Name = "DISCREET",
                Code = "DISCREET",
            };

            var claimDiscreetService = await context.InvestigationServiceType.AddAsync(claimDiscreet);


            var underWritingPreVerification = new InvestigationServiceType
            {
                Name = "PRE-ONBOARDING-VERIFICATION",
                Code = "PRE-ONBOARDING-VERIFICATION",
            };

            var underWritingPreVerificationService = await context.InvestigationServiceType.AddAsync(underWritingPreVerification);


            var underWritingPostVerification = new InvestigationServiceType
            {
                Name = "POST-ONBOARDING-VERIFICATION",
                Code = "POST-ONBOARDING-VERIFICATION",
            };

            var underWritingPostVerificationService = await context.InvestigationServiceType.AddAsync(underWritingPostVerification);


            #endregion

            #region LINE OF BUSINESS

            var claims = new LineOfBusiness
            {
                Name = "CLAIMS",
                Code = "CLAIMS",
                InvestigationServiceTypes = new List<InvestigationServiceType> { claimComprehensiveService.Entity, claimNonComprehensiveService.Entity, claimDocumentCollectionService.Entity, claimDiscreetService.Entity }
            };

            var claimCaseType = await context.LineOfBusiness.AddAsync(claims);

            var underwriting = new LineOfBusiness
            {
                Name = "UNDERWRITING",
                Code = "UNDERWRITING",
                InvestigationServiceTypes = new List<InvestigationServiceType> { underWritingPreVerificationService.Entity, underWritingPostVerificationService.Entity }
            };

            var underwritingCaseType = await context.LineOfBusiness.AddAsync(underwriting);

            #endregion

            #region //CREATE RISK CASE DETAILS

            var created = new InvestigationCaseStatus
            {
                Name = "CREATED",
                Code = "CREATED"
            };
            var currentCaseStatus1 = await context.InvestigationCaseStatus.AddAsync(created);
            var assigned = new InvestigationCaseStatus
            {
                Name = "ASSIGNED",
                Code = "ASSIGNED"
            };

            var currentCaseStatus2 = await context.InvestigationCaseStatus.AddAsync(assigned);
            var rejected = new InvestigationCaseStatus
            {
                Name = "REJECTED",
                Code = "REJECTED"
            };

            var currentCaseStatus3 = await context.InvestigationCaseStatus.AddAsync(rejected);
            var accepted = new InvestigationCaseStatus
            {
                Name = "ACCEPTED",
                Code = "ACCEPTED"
            };

            var currentCaseStatus4 = await context.InvestigationCaseStatus.AddAsync(accepted);
            var withdrawn = new InvestigationCaseStatus
            {
                Name = "WITHDRAWN",
                Code = "WITHDRAWN"
            };

            var currentCaseStatus5 = await context.InvestigationCaseStatus.AddAsync(withdrawn);

            var clientCreatorCreated = new InvestigationCaseStatus
            {
                Name = "CLIENT_CREATOR_CREATED",
                Code = "CLIENT_CREATOR_CREATED"
            };

            var currentCaseStatus6 = await context.InvestigationCaseStatus.AddAsync(clientCreatorCreated);

            var clientAssignerAccepted = new InvestigationCaseStatus
            {
                Name = "CLIENT_ASSIGNER_ACCEPTED",
                Code = "CLIENT_ASSIGNER_ACCEPTED"
            };

            var currentCaseStatus7 = await context.InvestigationCaseStatus.AddAsync(clientAssignerAccepted);

            var vendorSupervisorAccepted = new InvestigationCaseStatus
            {
                Name = "VENDOR_SUPERVISOR_ACCEPTED",
                Code = "VENDOR_SUPERVISOR_ACCEPTED"
            };

            var currentCaseStatus8 = await context.InvestigationCaseStatus.AddAsync(vendorSupervisorAccepted);
            var vendorAgentAccepted = new InvestigationCaseStatus
            {
                Name = "VENDOR_AGENT_ACCEPTED",
                Code = "VENDOR_AGENT_ACCEPTED"
            };

            var currentCaseStatus9 = await context.InvestigationCaseStatus.AddAsync(vendorAgentAccepted);
            var vendorAgentSubmitted = new InvestigationCaseStatus
            {
                Name = "VENDOR_AGENT_SUBMITTED",
                Code = "VENDOR_AGENT_SUBMITTED"
            };

            var currentCaseStatus10 = await context.InvestigationCaseStatus.AddAsync(vendorAgentSubmitted);

            var vendorSupervisorSubmitted = new InvestigationCaseStatus
            {
                Name = "VENDOR_SUPERVISOR_SUBMITTED",
                Code = "VENDOR_SUPERVISOR_SUBMITTED"
            };

            var currentCaseStatus11 = await context.InvestigationCaseStatus.AddAsync(vendorSupervisorSubmitted);

            var clientAsssessorAccepted = new InvestigationCaseStatus
            {
                Name = "CLIENT_ASSESSOR_ACCEPTED",
                Code = "CLIENT_ASSESSOR_ACCEPTED"
            };

            var currentCaseStatus12 = await context.InvestigationCaseStatus.AddAsync(clientAsssessorAccepted);

            var clientAsssessorSubmitted = new InvestigationCaseStatus
            {
                Name = "CLIENT_ASSESSOR_SUBMITTED",
                Code = "CLIENT_ASSESSOR_SUBMITTED"
            };

            var currentCaseStatus13 = await context.InvestigationCaseStatus.AddAsync(clientAsssessorSubmitted);
            var clientAsssessorRejected = new InvestigationCaseStatus
            {
                Name = "CLIENT_ASSESSOR_REJECTED",
                Code = "CLIENT_ASSESSOR_REJECTED"
            };

            var currentCaseStatus14 = await context.InvestigationCaseStatus.AddAsync(clientAsssessorSubmitted);
            var clientAsssessorReturnRejected = new InvestigationCaseStatus
            {
                Name = "CLIENT_ASSESSOR_RETURN_REJECTED",
                Code = "CLIENT_ASSESSOR_RETURN_REJECTED"
            };

            var currentCaseStatus15 = await context.InvestigationCaseStatus.AddAsync(clientAsssessorReturnRejected);
            #endregion


            #region INVESTIGATION CASES

            var claimComprehensiveCase = new InvestigationCase
            {
                Name = "TEST CLAIM CASE 1",
                Description = "TEST CLAIM CASE DESCRIPTION comprehensive service 1",
                LineOfBusinessId = claimCaseType.Entity.LineOfBusinessId,
                InvestigationServiceTypeId = claimComprehensiveService.Entity.InvestigationServiceTypeId,
                InvestigationCaseStatusId = currentCaseStatus1.Entity.InvestigationCaseStatusId,
                Created = DateTime.Now
            };
            var claimComprehensiveCase2 = new InvestigationCase
            {
                Name = "TEST CLAIM CASE 2",
                Description = "TEST CLAIM CASE DESCRIPTION comprehensive service 2",
                LineOfBusinessId = claimCaseType.Entity.LineOfBusinessId,
                InvestigationServiceTypeId = claimComprehensiveService.Entity.InvestigationServiceTypeId,
                InvestigationCaseStatusId = currentCaseStatus1.Entity.InvestigationCaseStatusId,
                Created = DateTime.Now
            };
            var claimComprehensiveCase3 = new InvestigationCase
            {
                Name = "TEST CLAIM CASE 3",
                Description = "TEST CLAIM CASE DESCRIPTION comprehensive service 3",
                LineOfBusinessId = claimCaseType.Entity.LineOfBusinessId,
                InvestigationServiceTypeId = claimComprehensiveService.Entity.InvestigationServiceTypeId,
                InvestigationCaseStatusId = currentCaseStatus1.Entity.InvestigationCaseStatusId,
                Created = DateTime.Now
            };

            var claimNonComprehensiveCase = new InvestigationCase
            {
                Name = "TEST CLAIM CASE 1",
                Description = "TEST CLAIM CASE DESCRIPTION non-comprehensive service 1",
                LineOfBusinessId = claimCaseType.Entity.LineOfBusinessId,
                InvestigationServiceTypeId = claimNonComprehensiveService.Entity.InvestigationServiceTypeId,
                InvestigationCaseStatusId = currentCaseStatus2.Entity.InvestigationCaseStatusId,
                Created = DateTime.Now
            };

            var claimNonComprehensiveCase2 = new InvestigationCase
            {
                Name = "TEST CLAIM CASE 2",
                Description = "TEST CLAIM CASE DESCRIPTION non-comprehensive service 2",
                LineOfBusinessId = claimCaseType.Entity.LineOfBusinessId,
                InvestigationServiceTypeId = claimNonComprehensiveService.Entity.InvestigationServiceTypeId,
                InvestigationCaseStatusId = currentCaseStatus2.Entity.InvestigationCaseStatusId,
                Created = DateTime.Now
            };

            var underwritingPreCase = new InvestigationCase
            {
                Name = "UNDER-WRITING PRE CASE 1",
                Description = "UNDER-WRITING PRE CASE DESCRIPTION 1",
                LineOfBusinessId = underwritingCaseType.Entity.LineOfBusinessId,
                InvestigationServiceTypeId = underWritingPreVerificationService.Entity.InvestigationServiceTypeId,
                InvestigationCaseStatusId = currentCaseStatus3.Entity.InvestigationCaseStatusId,
                Created = DateTime.Now
            };

            var underwritingPreCase2 = new InvestigationCase
            {
                Name = "UNDER-WRITING PRE CASE 2",
                Description = "UNDER-WRITING PRE CASE DESCRIPTION 2",
                LineOfBusinessId = underwritingCaseType.Entity.LineOfBusinessId,
                InvestigationServiceTypeId = underWritingPreVerificationService.Entity.InvestigationServiceTypeId,
                InvestigationCaseStatusId = currentCaseStatus3.Entity.InvestigationCaseStatusId,
                Created = DateTime.Now
            };

            var claimDocumentCollectionCase = new InvestigationCase
            {
                Name = "TEST CLAIM CASE DOCUMENT COLLECTION",
                Description = "TEST CLAIM DOCUMENT COLLECTION CASE DESCRIPTION",
                LineOfBusinessId = claimCaseType.Entity.LineOfBusinessId,
                InvestigationServiceTypeId = claimDocumentCollectionService.Entity.InvestigationServiceTypeId,
                InvestigationCaseStatusId = currentCaseStatus4.Entity.InvestigationCaseStatusId,
                Created = DateTime.Now
            };

            var claimDocumentCollectionCase2 = new InvestigationCase
            {
                Name = "TEST CLAIM CASE DOCUMENT COLLECTION 2",
                Description = "TEST CLAIM DOCUMENT COLLECTION CASE DESCRIPTION 2",
                LineOfBusinessId = claimCaseType.Entity.LineOfBusinessId,
                InvestigationServiceTypeId = claimDocumentCollectionService.Entity.InvestigationServiceTypeId,
                InvestigationCaseStatusId = currentCaseStatus4.Entity.InvestigationCaseStatusId,
                Created = DateTime.Now
            };

            var underwritingPostCase = new InvestigationCase
            {
                Name = "UNDER-WRITING POST CASE",
                Description = "TEST CLAIM POST CASE DESCRIPTION",
                LineOfBusinessId = underwritingCaseType.Entity.LineOfBusinessId,
                InvestigationServiceTypeId = underWritingPostVerificationService.Entity.InvestigationServiceTypeId,
                InvestigationCaseStatusId = currentCaseStatus5.Entity.InvestigationCaseStatusId,
                Created = DateTime.Now
            };

            var claimDiscreetCase = new InvestigationCase
            {
                Name = "TEST CLAIM DISCREET CASE ",
                Description = "TEST CLAIM DISCREET CASE DESCRIPTION",
                LineOfBusinessId = claimCaseType.Entity.LineOfBusinessId,
                InvestigationServiceTypeId = claimDiscreetService.Entity.InvestigationServiceTypeId,
                InvestigationCaseStatusId = currentCaseStatus4.Entity.InvestigationCaseStatusId,
                Created = DateTime.Now
            };

            #endregion

            await context.InvestigationCase.AddAsync(claimComprehensiveCase);
            await context.InvestigationCase.AddAsync(claimComprehensiveCase2);
            await context.InvestigationCase.AddAsync(claimComprehensiveCase3);
            await context.InvestigationCase.AddAsync(claimNonComprehensiveCase);
            await context.InvestigationCase.AddAsync(claimNonComprehensiveCase2);
            await context.InvestigationCase.AddAsync(claimDocumentCollectionCase);
            await context.InvestigationCase.AddAsync(claimDocumentCollectionCase2);
            await context.InvestigationCase.AddAsync(underwritingPreCase);
            await context.InvestigationCase.AddAsync(underwritingPreCase2);
            await context.InvestigationCase.AddAsync(underwritingPostCase);
            await context.InvestigationCase.AddAsync(claimDiscreetCase);

            await context.SaveChangesAsync();

            #region CLIENT/ VENDOR COMPANY

            var (vendorId, clientCompanyId) = await ClientVendorSeed.Seed(context, indiaCountry, claimComprehensiveService.Entity, claimCaseType.Entity);

            #endregion

            #region APPLICATION USERS ROLES

            await PortalAdminSeed.Seed(context, indiaCountry, userManager, roleManager);

            await ClientApplicationUserSeed.Seed(context, indiaCountry, clientUserManager, clientCompanyId);

            await VendorApplicationUserSeed.Seed(context, indiaCountry, vendorUserManager, vendorId);

            #endregion
        }
    }
}
