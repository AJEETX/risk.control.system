using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using static risk.control.system.AppConstant.CONSTANTS;

namespace risk.control.system.Services.Creator
{
    public interface IPolicyProcessor
    {
        Task<(PolicyDetail Policy, List<UploadError> Errors, List<string> Summaries)> ProcessPolicy(UploadCase uc, ApplicationUser user, byte[] zipData);
    }

    public class PolicyProcessor : IPolicyProcessor
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IVerifierProcessor verifierProcessor;
        private readonly IDateParserService dateParserService;

        public PolicyProcessor(IDbContextFactory<ApplicationDbContext> contextFactory, IVerifierProcessor verifierProcessor, IDateParserService dateParserService)
        {
            _contextFactory = contextFactory;
            this.verifierProcessor = verifierProcessor;
            this.dateParserService = dateParserService;
        }

        public async Task<(PolicyDetail Policy, List<UploadError> Errors, List<string> Summaries)> ProcessPolicy(UploadCase uc, ApplicationUser user, byte[] zipData)
        {
            var errs = new List<UploadError>();
            var sums = new List<string>();

            // 1. Determine Insurance Type
            var insuranceType = uc.InsuranceType == InsuranceType.CLAIM.GetEnumDisplayName() ? InsuranceType.CLAIM : InsuranceType.UNDERWRITING;

            // 2. Validate Dates & Amount
            var (issueDate, incidentDate) = dateParserService.ValidateDates(uc, errs, sums);
            decimal.TryParse(uc.Amount, out var amount);

            // 3. Lookups (Service Type, Enabler, Cost Centre)
            var serviceTypeTask = GetServiceType(uc.ServiceType!.Trim(), insuranceType);
            var enablerTask = GetCaseEnabler(uc.Reason!.Trim());
            var costCentreTask = GetCostCentre(uc.Department!.Trim());
            await Task.WhenAll(serviceTypeTask, enablerTask, costCentreTask);

            var serviceType = await serviceTypeTask;
            var enabler = await enablerTask;
            var costCentre = await costCentreTask;
            // 4. Image Processing
            var (imgPath, ext) = await verifierProcessor.ProcessImage(uc, zipData, errs, sums, POLICY_IMAGE, "CaseDetail");

            var policy = new PolicyDetail
            {
                ContractNumber = uc.CaseId?.ToUpper()!,
                SumAssuredValue = amount,
                ContractIssueDate = issueDate,
                DateOfIncident = incidentDate,
                InvestigationServiceTypeId = serviceType.InvestigationServiceTypeId,
                CaseEnablerId = enabler.CaseEnablerId,
                CostCentreId = costCentre.CostCentreId,
                InsuranceType = insuranceType,
                CauseOfLoss = uc.Cause!.Trim() ?? "UNKNOWN",
                DocumentPath = imgPath,
                DocumentImageExtension = ext,
                Updated = DateTime.UtcNow,
                UpdatedBy = user.Email
            };

            return (policy, errs, sums);
        }

        private async Task<InvestigationServiceType> GetServiceType(string code, InsuranceType type)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // 1. Prepare the search term to ensure the query is "SARGable" (index-friendly)
            var normalizedCode = code?.Trim().ToLower();
            var hasCode = !string.IsNullOrWhiteSpace(normalizedCode);

            // 2. Try the most specific match first
            InvestigationServiceType? service = null!;

            if (hasCode)
            {
                service = await context.InvestigationServiceType
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Code.ToLower() == normalizedCode && s.InsuranceType == type);
            }

            // 3. Fallback 1: Match by InsuranceType only
            if (service == null)
            {
                service = await context.InvestigationServiceType
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.InsuranceType == type);
            }

            // 4. Fallback 2: Get the absolute default (first record)
            return service ?? (await context.InvestigationServiceType.AsNoTracking().FirstOrDefaultAsync())!;
        }

        private async Task<CaseEnabler> GetCaseEnabler(string reason)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (string.IsNullOrWhiteSpace(reason))
                return (await context.CaseEnabler.AsNoTracking().FirstOrDefaultAsync())!;
            reason = reason.Trim().ToLower();
            return await context.CaseEnabler.AsNoTracking().FirstOrDefaultAsync(c => c.Code == reason)
                ?? (await context.CaseEnabler.AsNoTracking().FirstOrDefaultAsync())!;
        }

        private async Task<CostCentre> GetCostCentre(string department)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (string.IsNullOrWhiteSpace(department))
                return (await context.CostCentre.AsNoTracking().FirstOrDefaultAsync())!;
            department = department.Trim().ToLower();
            return await context.CostCentre.AsNoTracking().FirstOrDefaultAsync(c => c.Code == department)
                ?? (await context.CostCentre.AsNoTracking().FirstOrDefaultAsync())!;
        }
    }
}