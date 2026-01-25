using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using static risk.control.system.AppConstant.CONSTANTS;

namespace risk.control.system.Services
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
                // Parallel Execution for Speed
                var customerTask = customerCreationService.AddCustomer(user, uc, data);
                var beneficiaryTask = beneficiaryCreationService.AddBeneficiary(user, uc, data);

                await Task.WhenAll(customerTask, beneficiaryTask);

                // Extract Results
                var (cust, custErrs, custSums) = await customerTask;
                var (bene, beneErrs, beneSums) = await beneficiaryTask;

                var (policy, polyErrs, polySums) = await policyProcessor.ProcessPolicy(uc, user, data);

                // Aggregate Errors
                AggregateErrors(resultErrors, resultSummaries, custErrs, custSums, beneErrs, beneSums, polyErrs, polySums);

                // Deep Clone Template based on Insurance Type
                var template = await cloneService.DeepCloneReportTemplate(user.ClientCompanyId.Value, policy.InsuranceType.Value);

                // Assemble the InvestigationTask
                var task = new InvestigationTask
                {
                    ORIGIN = origin,
                    PolicyDetail = policy,
                    CustomerDetail = cust,
                    BeneficiaryDetail = bene,
                    ReportTemplateId = template.Id,
                    ReportTemplate = template,
                    Status = CASE_STATUS.INITIATED,
                    SubStatus = CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED,
                    ClientCompanyId = user.ClientCompanyId,
                    CaseOwner = user.Email,
                    CreatedUser = user.Email,
                    CreatorSla = user.ClientCompany.CreatorSla,
                    Updated = DateTime.Now,
                    UpdatedBy = user.Email,
                    IsNew = true
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

        private static void AggregateErrors( List<UploadError> mainList, List<string> mainSums, params object[] sets)
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
