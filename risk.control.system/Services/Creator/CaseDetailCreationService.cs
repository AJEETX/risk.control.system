using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Report;
using static risk.control.system.AppConstant.CONSTANTS;

namespace risk.control.system.Services.Creator
{
    public interface ICaseDetailCreationService
    {
        Task<UploadResult> AddCaseDetail(UploadCase uc, ApplicationUser user, byte[] data, ORIGIN origin);
    }

    internal class CaseDetailCreationService : ICaseDetailCreationService
    {
        private readonly IPolicyProcessor policyProcessor;
        private readonly ICloneReportService cloneService;
        private readonly IBeneficiaryCreationService beneficiaryCreationService;
        private readonly ICustomerCreationService customerCreationService;
        private readonly ILogger<CaseDetailCreationService> logger;

        public CaseDetailCreationService(IPolicyProcessor policyProcessor,
            ICloneReportService cloneService,
            IBeneficiaryCreationService beneficiaryCreationService,
            ICustomerCreationService customerCreationService,
            ILogger<CaseDetailCreationService> logger)
        {
            this.policyProcessor = policyProcessor;
            this.cloneService = cloneService;
            this.beneficiaryCreationService = beneficiaryCreationService;
            this.customerCreationService = customerCreationService;
            this.logger = logger;
        }

        public async Task<UploadResult> AddCaseDetail(UploadCase uc, ApplicationUser user, byte[] data, ORIGIN origin)
        {
            var resultErrors = new List<UploadError>();
            var resultSummaries = new List<string>();

            try
            {
                // 1. Kick off all major tasks in parallel immediately
                var customerTask = customerCreationService.AddCustomer(user, uc, data);
                var beneficiaryTask = beneficiaryCreationService.AddBeneficiary(user, uc, data);
                var policyTask = policyProcessor.ProcessPolicy(uc, user, data);

                // 2. Wait for all core data to be processed
                await Task.WhenAll(customerTask, beneficiaryTask, policyTask);

                // 3. Destructure results (awaiting completed tasks is instant and safe)
                var (cust, custErrs, custSums) = await customerTask;
                var (bene, beneErrs, beneSums) = await beneficiaryTask;
                var (policy, polyErrs, polySums) = await policyTask;

                // Aggregate Errors
                AggregateErrors(resultErrors, resultSummaries, custErrs, custSums, beneErrs, beneSums, polyErrs, polySums);

                // 4. Sequential logic that depends on previous results
                if (policy?.InsuranceType == null)
                    throw new Exception("Policy or Insurance Type missing.");

                var template = await cloneService.DeepCloneReportTemplate(user.ClientCompanyId.Value, policy.InsuranceType.Value);

                var task = new InvestigationTask
                {
                    ORIGIN = origin,
                    PolicyDetail = policy,
                    CustomerDetail = cust,
                    BeneficiaryDetail = bene,
                    ReportTemplateId = template?.Id,
                    ReportTemplate = template,
                    Status = CASE_STATUS.INITIATED,
                    SubStatus = CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED,
                    ClientCompanyId = user.ClientCompanyId,
                    CaseOwner = user.Email,
                    CreatedUser = user.Email,
                    Updated = DateTime.UtcNow,
                    UpdatedBy = user.Email
                };

                task.IsReady2Assign = task.IsValidCaseData();

                return new UploadResult { InvestigationTask = task, ErrorDetail = resultErrors, Errors = resultSummaries };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fatal error orchestrating case {CaseId}", uc.CaseId);
                return new UploadResult { ErrorDetail = resultErrors, Errors = resultSummaries };
            }
        }

        private static void AggregateErrors(List<UploadError> mainList, List<string> mainSums, params object[] sets)
        {
            foreach (var set in sets)
            {
                switch (set)
                {
                    case IEnumerable<UploadError> errorList:
                        mainList.AddRange(errorList);
                        break;

                    case IEnumerable<string> summaryList:
                        mainSums.AddRange(summaryList);
                        break;
                }
            }
        }
    }
}