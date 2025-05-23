﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

namespace risk.control.system.Seeds
{
    public static class DataSeed
    {
        public static async Task SeedDetails(ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment, 
            UserManager<ClientCompanyApplicationUser> clientUserManager, 
            UserManager<VendorApplicationUser> vendorUserManager, 
            ICustomApiCLient customApiCLient,
            IHttpContextAccessor httpAccessor)
        {
            #region //CREATE RISK CASE DETAILS

            //CASE STATUS

            //var initiated = new InvestigationCaseStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.INITIATED,
            //    Code = CONSTANTS.CASE_STATUS.INITIATED,
            //    Updated = DateTime.Now,
            //    MasterData = true,
            //};

            //var initiatedStatus = await context.InvestigationCaseStatus.AddAsync(initiated);

            //var inProgress = new InvestigationCaseStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.INPROGRESS,
            //    Code = CONSTANTS.CASE_STATUS.INPROGRESS,
            //    Updated = DateTime.Now,
            //    MasterData = true,
            //};

            //var inProgressStatus = await context.InvestigationCaseStatus.AddAsync(inProgress);

            //var finished = new InvestigationCaseStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.FINISHED,
            //    Code = CONSTANTS.CASE_STATUS.FINISHED,
            //    Updated = DateTime.Now,
            //    MasterData = true,
            //};

            //var finishedStatus = await context.InvestigationCaseStatus.AddAsync(finished);

            ////CASE SUBSTATUS

            //var created = new InvestigationCaseSubStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
            //    Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
            //    MasterData = true,
            //    InvestigationCaseStatus = initiatedStatus.Entity
            //};
            //var createdSubStatus = await context.InvestigationCaseSubStatus.AddAsync(created);

            //var edited = new InvestigationCaseSubStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR,
            //    Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR,
            //    MasterData = true,
            //    InvestigationCaseStatus = initiatedStatus.Entity,
            //    Updated = DateTime.Now,
            //};
            //var editedSubStatus = await context.InvestigationCaseSubStatus.AddAsync(edited);

            //var assigned = new InvestigationCaseSubStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
            //    Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
            //    MasterData = true,
            //    Updated = DateTime.Now,
            //    InvestigationCaseStatus = inProgressStatus.Entity
            //};

            //var assignedSubStatus = await context.InvestigationCaseSubStatus.AddAsync(assigned);

            //var allocated = new InvestigationCaseSubStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
            //    Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
            //    MasterData = true,
            //    Updated = DateTime.Now,
            //    InvestigationCaseStatus = inProgressStatus.Entity
            //};

            //var allocatedSubStatus = await context.InvestigationCaseSubStatus.AddAsync(allocated);

            //var withdrawn = new InvestigationCaseSubStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
            //    Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
            //    MasterData = true,
            //    Updated = DateTime.Now,
            //    InvestigationCaseStatus = inProgressStatus.Entity
            //};

            //var withdrawnSubStatus = await context.InvestigationCaseSubStatus.AddAsync(withdrawn);

            //var assignedToAgent = new InvestigationCaseSubStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
            //    Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
            //    MasterData = true,
            //    Updated = DateTime.Now,
            //    InvestigationCaseStatus = inProgressStatus.Entity
            //};

            //var assignedToAgentSubStatus = await context.InvestigationCaseSubStatus.AddAsync(assignedToAgent);

            //var submittedtoSupervisor = new InvestigationCaseSubStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
            //    Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
            //    MasterData = true,
            //    Updated = DateTime.Now,
            //    InvestigationCaseStatus = inProgressStatus.Entity
            //};

            //var submittedtoSupervisorSubStatus = await context.InvestigationCaseSubStatus.AddAsync(submittedtoSupervisor);

            //var submittedToAssessor = new InvestigationCaseSubStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR,
            //    Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR,
            //    MasterData = true,
            //    Updated = DateTime.Now,
            //    InvestigationCaseStatus = inProgressStatus.Entity
            //};

            //var submittedToAssessorSubStatus = await context.InvestigationCaseSubStatus.AddAsync(submittedToAssessor);

            //var requestedByCompany = new InvestigationCaseSubStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR,
            //    Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR,
            //    Updated = DateTime.Now,
            //    MasterData = true,
            //    InvestigationCaseStatus = inProgressStatus.Entity
            //};

            //var requestedByCompanySubStatus = await context.InvestigationCaseSubStatus.AddAsync(requestedByCompany);


            //var replyToCompany = new InvestigationCaseSubStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR,
            //    Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR,
            //    Updated = DateTime.Now,
            //    MasterData = true,
            //    InvestigationCaseStatus = inProgressStatus.Entity
            //};

            //var replyToCompanySubStatus = await context.InvestigationCaseSubStatus.AddAsync(replyToCompany);

            //var approved = new InvestigationCaseSubStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR,
            //    Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR,
            //    MasterData = true,
            //    Updated = DateTime.Now,
            //    InvestigationCaseStatus = finishedStatus.Entity
            //};

            //var approvededSubStatus = await context.InvestigationCaseSubStatus.AddAsync(approved);

            //var rejected = new InvestigationCaseSubStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR,
            //    Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR,
            //    Updated = DateTime.Now,
            //    MasterData = true,
            //    InvestigationCaseStatus = finishedStatus.Entity
            //};

            //var rejectedSubStatus = await context.InvestigationCaseSubStatus.AddAsync(rejected);

            //var reassigned = new InvestigationCaseSubStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER,
            //    Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER,
            //    Updated = DateTime.Now,
            //    MasterData = true,
            //    InvestigationCaseStatus = finishedStatus.Entity
            //};
            //var acceptedSubStatus = await context.InvestigationCaseSubStatus.AddAsync(reassigned);

            //var withdrawnByCompany = new InvestigationCaseSubStatus
            //{
            //    Name = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY,
            //    Code = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY,
            //    Updated = DateTime.Now,
            //    MasterData = true,
            //    InvestigationCaseStatus = finishedStatus.Entity
            //};

            //var withdrawnByCompanySubStatus = await context.InvestigationCaseSubStatus.AddAsync(withdrawnByCompany);


            #endregion //CREATE RISK CASE DETAILS

            #region BENEFICIARY-RELATION

            await ClientCompanySetupSeed.Seed(context);

            #endregion BENEFICIARY-RELATION

            #region CLIENT/ VENDOR COMPANY

            var (vendors, companyIds) = await ClientVendorSeed.Seed(context, webHostEnvironment,customApiCLient, clientUserManager, vendorUserManager);

            #endregion CLIENT/ VENDOR COMPANY

            #region PERMISSIONS ROLES

            //PermissionModuleSeed.SeedMailbox(context);

            //PermissionModuleSeed.SeedClaim(context);

            #endregion PERMISSIONS ROLES

        }
    }
}
