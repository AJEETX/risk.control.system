using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agent
{
    public interface IAgentService
    {
        Task<ApplicationUser> GetAgent(string mobile, bool sendSMS = false);

        Task<ApplicationUser> ResetUid(string mobile, string portal_base_url, bool sendSMS = false);

        Task<ApplicationUser> GetPin(string agentEmail, string portal_base_url);

        Task<List<CaseInvestigationAgencyResponse>> GetNewCases(string userEmail);

        Task<List<CaseInvestigationAgencyResponse>> GetSubmittedCases(string userEmail);
    }

    internal class AgentService : IAgentService
    {
        private readonly ApplicationDbContext context;
        private readonly IBase64FileService base64FileService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IWebHostEnvironment env;
        private readonly ISmsService smsService;
        private readonly UserManager<ApplicationUser> userVendorManager;

        public AgentService(ApplicationDbContext context,
            IBase64FileService base64FileService,
            RoleManager<ApplicationRole> roleManager,
            IWebHostEnvironment env,
             ISmsService smsService,
            UserManager<ApplicationUser> userVendorManager)
        {
            this.context = context;
            this.base64FileService = base64FileService;
            this.roleManager = roleManager;
            this.env = env;
            this.smsService = smsService;
            this.userVendorManager = userVendorManager;
        }

        public async Task<ApplicationUser> GetAgent(string mobile, bool sendSMS = false)
        {
            var agentRole = await roleManager.FindByNameAsync(AGENT.DISPLAY_NAME);

            var user2Onboard = await context.ApplicationUser.FirstOrDefaultAsync(u => u.PhoneNumber == mobile && !string.IsNullOrWhiteSpace(u.MobileUId));

            var isAgent = await userVendorManager.IsInRoleAsync(user2Onboard, agentRole?.Name);
            if (isAgent)
                return user2Onboard;
            return null!;
        }

        public async Task<List<CaseInvestigationAgencyResponse>> GetNewCases(string userEmail)
        {
            var vendorUser = await context.ApplicationUser.Include(v => v.Country).FirstOrDefaultAsync(c => c.Email == userEmail);
            var assignedToAgentStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
            var claims = await GetClaims()
                    .Where(i => i.VendorId == vendorUser.VendorId &&
                    i.TaskedAgentEmail == userEmail &&
                    !i.Deleted &&
                    i.SubStatus == assignedToAgentStatus).ToListAsync();

            var response = claims
                   .Select(a => new CaseInvestigationAgencyResponse
                   {
                       Id = a.Id,
                       PolicyId = a.PolicyDetail.ContractNumber,
                       Amount = string.Format(CustomExtensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                       Company = a.ClientCompany.Name,
                       Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                       PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                       AssignedToAgency = a.AssignedToAgency,
                       Document = a.PolicyDetail.DocumentPath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.PolicyDetail.DocumentPath)))) : Applicationsettings.NO_POLICY_IMAGE,
                       Customer =
                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail))))),
                       Name = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                       Policy = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                       Status = a.SubStatus,
                       ServiceType = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                       Service = a.PolicyDetail.InvestigationServiceType.Name,
                       Location = a.SubStatus,
                       Created = a.Created.ToString("dd-MM-yyyy"),
                       timePending = a.GetAgentTimePending(),
                       PolicyNum = a.GetPolicyNumForAgency(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR),
                       BeneficiaryPhoto = a.BeneficiaryDetail.ImagePath != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, a.BeneficiaryDetail?.ImagePath)))) : Applicationsettings.NO_USER,
                       BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.Name,
                       TimeElapsed = DateTime.UtcNow.Subtract(a.TaskToAgentTime.Value).TotalSeconds,
                       IsNewAssigned = a.IsNewSubmittedToAgent,
                       IsQueryCase = a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR,
                       PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                       Distance = a.SelectedAgentDrivingDistance,
                       Duration = a.SelectedAgentDrivingDuration
                   })
                    ?.ToList();
            return response;
        }

        public async Task<ApplicationUser> GetPin(string agentEmail, string portal_base_url)
        {
            var agentRole = await roleManager.FindByNameAsync(AGENT.DISPLAY_NAME);

            var user2Onboard = await context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == agentEmail);

            var isAgent = await userVendorManager.IsInRoleAsync(user2Onboard, agentRole?.Name);
            if (isAgent)
                return user2Onboard;
            return null!;
        }

        public async Task<List<CaseInvestigationAgencyResponse>> GetSubmittedCases(string userEmail)
        {
            var agentUser = await context.ApplicationUser.AsNoTracking().Include(v => v.Country).Include(u => u.Vendor).FirstOrDefaultAsync(c => c.Email == userEmail);
            var claims = await GetClaims()
                    .Where(i => i.VendorId == agentUser.VendorId &&
                    i.TaskedAgentEmail == userEmail &&
                    !i.Deleted &&
                    i.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT).ToListAsync();

            var finalDataTasks = claims
                   .Select(async a =>
                   {
                       var culture = CustomExtensions.GetCultureByCountry(agentUser.Country.Code.ToUpper());
                       var isUW = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING;

                       var documentTask = base64FileService.GetBase64FileAsync(a.PolicyDetail.DocumentPath, Applicationsettings.NO_POLICY_IMAGE);
                       var customerPhotoTask = base64FileService.GetBase64FileAsync(isUW ? a.CustomerDetail.ImagePath : a.BeneficiaryDetail.ImagePath);
                       var beneficiaryPhotoTask = base64FileService.GetBase64FileAsync(a.BeneficiaryDetail.ImagePath, Applicationsettings.NO_USER);
                       await Task.WhenAll(documentTask, customerPhotoTask, beneficiaryPhotoTask);

                       return new CaseInvestigationAgencyResponse
                       {
                           Id = a.Id,
                           PolicyId = a.PolicyDetail.ContractNumber,
                           Amount = string.Format(culture, "{0:C}", a.PolicyDetail.SumAssuredValue),
                           AssignedToAgency = a.AssignedToAgency,
                           Pincode = ClaimsInvestigationExtension.GetPincode(isUW, a.CustomerDetail, a.BeneficiaryDetail),
                           PincodeName = ClaimsInvestigationExtension.GetPincodeName(isUW, a.CustomerDetail, a.BeneficiaryDetail),
                           Company = a.ClientCompany.Name,
                           Document = await documentTask,
                           Customer = await customerPhotoTask,
                           Name = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                           Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                           Status = a.SubStatus,
                           ServiceType = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                           Service = a.PolicyDetail.InvestigationServiceType.Name,
                           Location = a.SubStatus,
                           Created = a.Created.ToString("dd-MM-yyyy"),
                           timePending = a.GetAgentTimePending(true),
                           PolicyNum = a.PolicyDetail.ContractNumber,
                           BeneficiaryPhoto = await beneficiaryPhotoTask,
                           BeneficiaryName = a.BeneficiaryDetail.Name,
                           TimeElapsed = DateTime.UtcNow.Subtract(a.SubmittedToSupervisorTime.Value).TotalSeconds,
                           PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                           Distance = a.SelectedAgentDrivingDistance,
                           Duration = a.SelectedAgentDrivingDuration
                       };
                   });
            var finalData = (await Task.WhenAll(finalDataTasks));

            return finalData.ToList();
        }

        public async Task<ApplicationUser> ResetUid(string mobile, string portal_base_url, bool sendSMS = false)
        {
            var agentRole = await roleManager.FindByNameAsync(AGENT.DISPLAY_NAME);

            var user2Onboards = context.ApplicationUser.Include(c => c.Country).Where(
                u => u.Country.ISDCode + u.PhoneNumber.TrimStart('+') == mobile.TrimStart('+') && !string.IsNullOrWhiteSpace(u.MobileUId));

            foreach (var user2Onboard in user2Onboards)
            {
                var isAgent = await userVendorManager.IsInRoleAsync(user2Onboard, agentRole?.Name);
                if (isAgent)
                {
                    user2Onboard.MobileUId = string.Empty;
                    user2Onboard.SecretPin = string.Empty;
                    context.ApplicationUser.Update(user2Onboard);
                    context.SaveChanges();

                    if (sendSMS)
                    {
                        //SEND SMS
                        string message = $"Dear {user2Onboard.Email}\n";
                        message += $"Uid reset for mobile: {mobile}\n";
                        message += $"{portal_base_url}";
                        await smsService.DoSendSmsAsync(user2Onboard.Country.Code, mobile, message);
                    }
                    return user2Onboard;
                }
            }
            return null!;
        }

        private IQueryable<InvestigationTask> GetClaims()
        {
            IQueryable<InvestigationTask> applicationDbContext = context.Investigations
               .Include(c => c.PolicyDetail)
               .Include(c => c.ClientCompany)
               .Include(c => c.BeneficiaryDetail)
               .ThenInclude(b => b.BeneficiaryRelation)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CaseEnabler)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CostCentre)
               .Include(c => c.BeneficiaryDetail)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.Country)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.District)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.InvestigationServiceType)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.State)
               .Include(c => c.Vendor)
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderByDescending(o => o.Created);
        }
    }
}