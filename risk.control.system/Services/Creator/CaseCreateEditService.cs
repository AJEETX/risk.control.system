using System.Net;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Api;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Creator
{
    public interface ICaseCreateEditService
    {
        Task<ClaimCreationState> GetCreationStateAsync(string userEmail);

        Task<CreateCaseViewModel> GetCreateViewModelAsync(string userEmail);

        Task<InvestigationCreateModel> Create(string userEmail);

        Task<CreateCaseViewModel> AddCaseDetail(string userEmail);

        Task<(bool Success, long? Id, string? CaseId, Dictionary<string, string> Errors)> CreateAsync(string userEmail, CreateCaseViewModel model);

        Task<EditPolicyDto> GetEditPolicyDetail(long id);

        Task<(bool Success, string? CaseId, Dictionary<string, string> Errors)> EditAsync(string userEmail, EditPolicyDto model);

        Task LoadDropDowns(PolicyDetailDto model, string userEmail);
    }

    internal class CaseCreateEditService : ICaseCreateEditService
    {
        private readonly IAddInvestigationService _addInvestigationService;
        private readonly ApplicationDbContext _context;
        private readonly INumberSequenceService numberService;
        private readonly IInvestigationService _investigationService;
        private readonly IValidateImageService _validateImageService;

        public CaseCreateEditService(
            IAddInvestigationService addInvestigationService,
            ApplicationDbContext context,
            INumberSequenceService numberService,
            IInvestigationService investigationService,
            IValidateImageService validateImageService)
        {
            _addInvestigationService = addInvestigationService;
            this._context = context;
            this.numberService = numberService;
            this._investigationService = investigationService;
            this._validateImageService = validateImageService;
        }

        public async Task LoadDropDowns(PolicyDetailDto model, string userEmail)
        {
            var currentUser = await _context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

            model.CurrencySymbol = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

            model.CaseEnablers = new SelectList(_context.CaseEnabler.AsNoTracking().OrderBy(s => s.Code), "CaseEnablerId", "Name", model.CaseEnablerId);

            model.CostCentres = new SelectList(_context.CostCentre.AsNoTracking().OrderBy(s => s.Code), "CostCentreId", "Name", model.CostCentreId);

            model.InsuranceTypes = new SelectList(Enum.GetValues(typeof(InsuranceType)).Cast<InsuranceType>(), model.InsuranceType);

            model.InvestigationServiceTypes = new SelectList(_context.InvestigationServiceType.AsNoTracking().Where(i => i.InsuranceType == model.InsuranceType).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", model.InvestigationServiceTypeId);
        }

        public async Task<ClaimCreationState> GetCreationStateAsync(string userEmail)
        {
            var user = await _context.ApplicationUser
                .AsNoTracking()
                .Include(u => u.ClientCompany)
                .Include(u => u.Country)
                .FirstOrDefaultAsync(u => u.Email == userEmail)
                ?? throw new KeyNotFoundException("User not found");

            var state = new ClaimCreationState
            {
                UserCanCreate = true,
                HasClaims = true,
                FileSampleIdentifier = user.Country.Code.ToLower(),
                IsTrial = user.ClientCompany.LicenseType == LicenseType.Trial
            };

            if (state.IsTrial)
            {
                var totalReadyToAssign = await _investigationService.GetAutoCount(userEmail);
                var totalClaimsCreated = await _context.Investigations.AsNoTracking()
                    .CountAsync(c => !c.Deleted && c.ClientCompanyId == user.ClientCompanyId);

                state.HasClaims = totalReadyToAssign > 0;
                state.MaxAllowed = user.ClientCompany.TotalCreatedClaimAllowed;
                state.AvailableCount = state.MaxAllowed - totalClaimsCreated;

                // Logic check: Can they create more?
                state.UserCanCreate = user.ClientCompany.TotalToAssignMaxAllowed > totalReadyToAssign;
            }

            return state;
        }

        public async Task<InvestigationCreateModel> Create(string userEmail)
        {
            var companyUser = await _context.ApplicationUser.Include(c => c.ClientCompany).FirstOrDefaultAsync(c => c.Email == userEmail);
            var caseTask = new InvestigationTask
            {
                ClientCompany = companyUser.ClientCompany
            };
            bool userCanCreate = true;
            int availableCount = 0;
            var trial = companyUser.ClientCompany.LicenseType == LicenseType.Trial;
            if (trial)
            {
                var totalClaimsCreated = _context.Investigations.Include(c => c.PolicyDetail).Where(c => !c.Deleted &&
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
            var caseEnabler = await _context.CaseEnabler.FirstOrDefaultAsync();
            var costCentre = await _context.CostCentre.FirstOrDefaultAsync();
            var service = await _context.InvestigationServiceType.FirstOrDefaultAsync(i => i.InsuranceType == InsuranceType.CLAIM);
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

            _validateImageService.ValidateImage(model.Document, errors);

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
                _validateImageService.ValidateImage(model.Document, errors);
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
            var caseTask = await _context.Investigations.AsNoTracking()
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

        public async Task<CreateCaseViewModel> GetCreateViewModelAsync(string userEmail)
        {
            var user = await _context.ApplicationUser
                .AsNoTracking()
                .Include(c => c.ClientCompany)
                .ThenInclude(c => c.Country)
                .FirstOrDefaultAsync(c => c.Email == userEmail)
                ?? throw new KeyNotFoundException("User not found");

            CreateCaseViewModel model;

            if (user.ClientCompany.HasSampleData)
            {
                model = await AddCaseDetail(userEmail);
                // We reuse the mapping logic here
                await PopulateMetadataAsync(model.PolicyDetailDto, user);
            }
            else
            {
                model = new CreateCaseViewModel();
                await PopulateMetadataAsync(model.PolicyDetailDto, user);
            }

            return model;
        }

        private async Task PopulateMetadataAsync(PolicyDetailDto dto, ApplicationUser user)
        {
            // Set Currency
            dto.CurrencySymbol = CustomExtensions
                .GetCultureByCountry(user.ClientCompany.Country.Code.ToUpper())
                .NumberFormat.CurrencySymbol;

            // Fetch Dropdowns (Parallelized for performance)
            var enablersTask = _context.CaseEnabler.AsNoTracking().OrderBy(s => s.Code).ToListAsync();
            var costCentresTask = _context.CostCentre.AsNoTracking().OrderBy(s => s.Code).ToListAsync();
            var serviceTypesTask = _context.InvestigationServiceType.AsNoTracking().OrderBy(s => s.Code).ToListAsync();

            await Task.WhenAll(enablersTask, costCentresTask, serviceTypesTask);

            dto.CaseEnablers = enablersTask.Result.Select(s => new SelectListItem
            { Text = s.Name, Value = s.CaseEnablerId.ToString(), Selected = s.CaseEnablerId == dto.CaseEnablerId });

            dto.CostCentres = costCentresTask.Result.Select(s => new SelectListItem
            { Text = s.Name, Value = s.CostCentreId.ToString(), Selected = s.CostCentreId == dto.CostCentreId });

            dto.InvestigationServiceTypes = serviceTypesTask.Result
                .Where(i => dto.InsuranceType == null || i.InsuranceType == dto.InsuranceType)
                .Select(s => new SelectListItem { Text = s.Name, Value = s.InvestigationServiceTypeId.ToString(), Selected = s.InvestigationServiceTypeId == dto.InvestigationServiceTypeId });

            dto.InsuranceTypes = Enum.GetValues(typeof(InsuranceType))
                .Cast<InsuranceType>()
                .Select(e => new SelectListItem { Text = e.ToString(), Value = e.ToString(), Selected = e == dto.InsuranceType });
        }
    }
}