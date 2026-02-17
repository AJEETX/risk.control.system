using System.Net;

using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Creator
{
    public interface ICaseCreateEditService
    {
        Task<InvestigationCreateModel> Create(string userEmail);

        Task<CreateCaseViewModel> AddCaseDetail(string userEmail);

        Task<(bool Success, long? Id, string? CaseId, Dictionary<string, string> Errors)> CreateAsync(string userEmail, CreateCaseViewModel model);

        Task<EditPolicyDto> GetEditPolicyDetail(long id);

        Task<(bool Success, string? CaseId, Dictionary<string, string> Errors)> EditAsync(string userEmail, EditPolicyDto model);
    }

    internal class CaseCreateEditService : ICaseCreateEditService
    {
        private readonly IAddInvestigationService _addInvestigationService;
        private readonly ApplicationDbContext context;
        private readonly INumberSequenceService numberService;
        private readonly IValidateImageService validateImageService;

        public CaseCreateEditService(IAddInvestigationService addInvestigationService, ApplicationDbContext context, INumberSequenceService numberService, IValidateImageService validateImageService)
        {
            _addInvestigationService = addInvestigationService;
            this.context = context;
            this.numberService = numberService;
            this.validateImageService = validateImageService;
        }

        public async Task<InvestigationCreateModel> Create(string userEmail)
        {
            var companyUser = await context.ApplicationUser.Include(c => c.ClientCompany).FirstOrDefaultAsync(c => c.Email == userEmail);
            var caseTask = new InvestigationTask
            {
                ClientCompany = companyUser.ClientCompany
            };
            bool userCanCreate = true;
            int availableCount = 0;
            var trial = companyUser.ClientCompany.LicenseType == LicenseType.Trial;
            if (trial)
            {
                var totalClaimsCreated = context.Investigations.Include(c => c.PolicyDetail).Where(c => !c.Deleted &&
                    c.ClientCompanyId == companyUser.ClientCompanyId)?.ToList();
                availableCount = companyUser.ClientCompany.TotalCreatedClaimAllowed - totalClaimsCreated.Count;

                if (totalClaimsCreated?.Count >= companyUser.ClientCompany.TotalCreatedClaimAllowed)
                {
                    userCanCreate = false;
                }
            }
            var model = new InvestigationCreateModel
            {
                InvestigationTask = caseTask,
                AllowedToCreate = userCanCreate,
                AutoAllocation = companyUser.ClientCompany.AutoAllocation,
                BeneficiaryDetail = new BeneficiaryDetail { },
                AvailableCount = availableCount,
                TotalCount = companyUser.ClientCompany.TotalCreatedClaimAllowed,
                Trial = trial
            };
            return model;
        }

        public async Task<CreateCaseViewModel> AddCaseDetail(string userEmail)
        {
            var contractNumber = await numberService.GetNumberSequence("PX");
            var caseEnabler = await context.CaseEnabler.FirstOrDefaultAsync();
            var costCentre = await context.CostCentre.FirstOrDefaultAsync();
            var service = await context.InvestigationServiceType.FirstOrDefaultAsync(i => i.InsuranceType == InsuranceType.CLAIM);
            var policy = new PolicyDetail
            {
                ContractNumber = contractNumber,
                InsuranceType = InsuranceType.CLAIM,
                InvestigationServiceTypeId = service.InvestigationServiceTypeId,
                CaseEnablerId = caseEnabler.CaseEnablerId,
                SumAssuredValue = 99999,
                ContractIssueDate = DateTime.UtcNow.AddDays(-10),
                DateOfIncident = DateTime.UtcNow.AddDays(-3),
                CauseOfLoss = "LOST IN ACCIDENT",
                CostCentreId = costCentre.CostCentreId
            };
            return new CreateCaseViewModel
            {
                PolicyDetailDto = new PolicyDetailDto
                {
                    ContractNumber = policy.ContractNumber,
                    InsuranceType = policy.InsuranceType.Value,
                    InvestigationServiceTypeId = policy.InvestigationServiceTypeId,
                    CaseEnablerId = policy.CaseEnablerId,
                    SumAssuredValue = policy.SumAssuredValue,
                    ContractIssueDate = policy.ContractIssueDate,
                    DateOfIncident = policy.DateOfIncident,
                    CauseOfLoss = policy.CauseOfLoss,
                    CostCentreId = policy.CostCentreId,
                }
            };
        }

        public async Task<(bool Success, long? Id, string? CaseId, Dictionary<string, string> Errors)> CreateAsync(string userEmail, CreateCaseViewModel model)
        {
            var errors = new Dictionary<string, string>();

            validateImageService.ValidateImage(model.Document, errors);

            ValidateDates(model.PolicyDetailDto, errors);

            if (errors.Any())
                return (false, null, null, errors);

            Sanitize(model.PolicyDetailDto);

            var caseDetail = await _addInvestigationService.CreateCase(userEmail, model);
            return caseDetail == null
                ? (false, null, null, errors)
                : (true, caseDetail.Id, caseDetail.PolicyDetail.ContractNumber, errors);
        }

        private static void ValidateDates(PolicyDetailDto policy, Dictionary<string, string> errors)
        {
            var now = DateTime.UtcNow;

            if (policy.DateOfIncident > now)
                errors["PolicyDetail.DateOfIncident"] = "Incident date cannot be in the future";

            if (policy.ContractIssueDate > now)
                errors["PolicyDetail.ContractIssueDate"] = "Issue date cannot be in the future";

            if (policy.DateOfIncident < policy.ContractIssueDate)
                errors["PolicyDetail.DateOfIncident"] = "Incident cannot be before issue date";
        }

        private static void Sanitize(PolicyDetailDto policy)
        {
            policy.ContractNumber = WebUtility.HtmlEncode(policy.ContractNumber);
            policy.CauseOfLoss = WebUtility.HtmlEncode(policy.CauseOfLoss);
        }

        public async Task<(bool Success, string? CaseId, Dictionary<string, string> Errors)> EditAsync(string userEmail, EditPolicyDto model)
        {
            var errors = new Dictionary<string, string>();
            if (model.Document != null && model.Document.Length > 0)
            {
                validateImageService.ValidateImage(model.Document, errors);
            }
            ValidateDates(model.PolicyDetailDto, errors);

            if (errors.Any())
                return (false, null, errors);

            Sanitize(model.PolicyDetailDto);

            var caseDetail = await _addInvestigationService.EditCase(userEmail, model);
            return caseDetail == null
                ? (false, null, errors)
                : (true, caseDetail.PolicyDetail.ContractNumber, errors);
        }

        public async Task<EditPolicyDto> GetEditPolicyDetail(long id)
        {
            var caseTask = await context.Investigations.AsNoTracking()
                    .Include(c => c.PolicyDetail)
                    .FirstOrDefaultAsync(i => i.Id == id);

            if (caseTask == null)
            {
                return null;
            }
            var model = new EditPolicyDto
            {
                Id = caseTask.Id,
                PolicyDetailDto = new PolicyDetailDto
                {
                    ContractNumber = caseTask.PolicyDetail.ContractNumber,
                    InsuranceType = caseTask.PolicyDetail.InsuranceType.Value,
                    InvestigationServiceTypeId = caseTask.PolicyDetail.InvestigationServiceTypeId,
                    CaseEnablerId = caseTask.PolicyDetail.CaseEnablerId,
                    SumAssuredValue = caseTask.PolicyDetail.SumAssuredValue,
                    ContractIssueDate = caseTask.PolicyDetail.ContractIssueDate,
                    DateOfIncident = caseTask.PolicyDetail.DateOfIncident,
                    CostCentreId = caseTask.PolicyDetail.CostCentreId,
                    CauseOfLoss = caseTask.PolicyDetail.CauseOfLoss,
                },
                ExistingDocumentPath = caseTask.PolicyDetail.DocumentPath
            };
            return model;
        }
    }
}