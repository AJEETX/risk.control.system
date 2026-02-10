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
            var serviceTypeTask = GetServiceType(uc.ServiceType, insuranceType);
            var enablerTask = GetCaseEnabler(uc.Reason);
            var costCentreTask = GetCostCentre(uc.Department);
            await Task.WhenAll(serviceTypeTask, enablerTask, costCentreTask);

            var serviceType = await serviceTypeTask;
            var enabler = await enablerTask;
            var costCentre = await costCentreTask;
            // 4. Image Processing
            var (imgPath, ext) = await verifierProcessor.ProcessImage(uc, zipData, errs, sums, POLICY_IMAGE, "CaseDetail");

            var policy = new PolicyDetail
            {
                ContractNumber = uc.CaseId,
                SumAssuredValue = amount,
                ContractIssueDate = issueDate,
                DateOfIncident = incidentDate,
                InvestigationServiceTypeId = serviceType.InvestigationServiceTypeId,
                CaseEnablerId = enabler.CaseEnablerId,
                CostCentreId = costCentre.CostCentreId,
                InsuranceType = insuranceType,
                CauseOfLoss = uc.Cause ?? "UNKNOWN",
                DocumentPath = imgPath,
                DocumentImageExtension = ext,
                Updated = DateTime.UtcNow,
                UpdatedBy = user.Email
            };

            return (policy, errs, sums);
        }

        private async Task<InvestigationServiceType> GetServiceType(string code, InsuranceType type)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            if (string.IsNullOrWhiteSpace(code))
            {
                var service = await context.InvestigationServiceType.FirstOrDefaultAsync(i => i.InsuranceType == type);
                if (service == null)
                {
                    service = await context.InvestigationServiceType.FirstOrDefaultAsync();
                }
                return service;
            }
            else
            {
                var services = await context.InvestigationServiceType
                .FirstOrDefaultAsync(b => b.Code.ToLower() == code.ToLower() && b.InsuranceType == type);
                if (services == null)
                {
                    services = await context.InvestigationServiceType.FirstOrDefaultAsync(b => b.InsuranceType == type);
                    if (services == null)
                    {
                        services = await context.InvestigationServiceType.FirstOrDefaultAsync();
                    }
                }
                return services;
            }
        }

        private async Task<CaseEnabler> GetCaseEnabler(string reason)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            if (string.IsNullOrWhiteSpace(reason))
                return await context.CaseEnabler.FirstOrDefaultAsync();

            return await context.CaseEnabler.FirstOrDefaultAsync(c => c.Code.ToLower() == reason.Trim().ToLower())
                ?? await context.CaseEnabler.FirstOrDefaultAsync();
        }

        private async Task<CostCentre> GetCostCentre(string department)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            if (string.IsNullOrWhiteSpace(department))
                return await context.CostCentre.FirstOrDefaultAsync();

            return await context.CostCentre.FirstOrDefaultAsync(c => c.Code.ToLower() == department.Trim().ToLower())
                ?? await context.CostCentre.FirstOrDefaultAsync();
        }
    }
}