using System.Globalization;

using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using static risk.control.system.AppConstant.CONSTANTS;

namespace risk.control.system.Services
{
    public interface ICaseDetailCreationService
    {
        Task<UploadResult> AddCaseDetail(UploadCase uploadCase, ApplicationUser companyUser, byte[] model, ORIGIN fileOrFTP);
    }
    internal class CaseDetailCreationService : ICaseDetailCreationService
    {

        private readonly ApplicationDbContext context;
        private readonly ICloneReportService cloneService;
        private readonly IFileStorageService fileStorageService;
        private readonly IBeneficiaryCreationService beneficiaryCreationService;
        private readonly ICustomerCreationService customerCreationService;
        private readonly ICaseImageCreationService caseImageCreationService;
        private readonly ILogger<CaseDetailCreationService> logger;

        public CaseDetailCreationService(ApplicationDbContext context,
            ICloneReportService cloneService,
            IFileStorageService fileStorageService,
            IBeneficiaryCreationService beneficiaryCreationService,
            ICustomerCreationService customerCreationService,
            ICaseImageCreationService caseImageCreationService,
            ILogger<CaseDetailCreationService> logger)
        {
            this.context = context;
            this.cloneService = cloneService;
            this.fileStorageService = fileStorageService;
            this.beneficiaryCreationService = beneficiaryCreationService;
            this.customerCreationService = customerCreationService;
            this.caseImageCreationService = caseImageCreationService;
            this.logger = logger;
        }

        private static bool ValidateDataCase(UploadCase uploadCase)
        {
            if (string.IsNullOrWhiteSpace(uploadCase.CaseId) ||
                string.IsNullOrWhiteSpace(uploadCase.CustomerName) ||
                string.IsNullOrWhiteSpace(uploadCase.CustomerDob) ||
                string.IsNullOrWhiteSpace(uploadCase.CustomerContact) ||
                string.IsNullOrWhiteSpace(uploadCase.CustomerAddressLine) ||
                string.IsNullOrWhiteSpace(uploadCase.CustomerPincode) ||
                string.IsNullOrWhiteSpace(uploadCase.BeneficiaryName) ||
                string.IsNullOrWhiteSpace(uploadCase.BeneficiaryDob) ||
                string.IsNullOrWhiteSpace(uploadCase.BeneficiaryContact) ||
                string.IsNullOrWhiteSpace(uploadCase.BeneficiaryAddressLine) ||
                string.IsNullOrWhiteSpace(uploadCase.BeneficiaryPincode))
            {
                return false;
            }
            return true;
        }
        public async Task<UploadResult> AddCaseDetail(UploadCase uploadCase, ApplicationUser companyUser, byte[] model, ORIGIN fileOrFTP)
        {
            var case_errors = new List<UploadError>();
            var caseErrors = new List<string>();
            try
            {
                var customerTask = customerCreationService.AddCustomer(companyUser, uploadCase, model);
                var beneficiaryTask = beneficiaryCreationService.AddBeneficiary(companyUser, uploadCase, model);
                await Task.WhenAll(customerTask, beneficiaryTask);

                // Get the results
                var (customer, customer_errors, customer_err) = await customerTask;
                var (beneficiary, beneficiary_errors, beneficiary_err) = await beneficiaryTask;
                caseErrors.AddRange(customer_err);
                caseErrors.AddRange(beneficiary_err);
                InsuranceType caseType = InsuranceType.CLAIM;
                if (uploadCase.InsuranceType != InsuranceType.CLAIM.GetEnumDisplayName())
                {
                    caseType = InsuranceType.UNDERWRITING;
                }

                if (string.IsNullOrWhiteSpace(uploadCase.ServiceType))
                {
                    case_errors.Add(new UploadError { UploadData = $"[{nameof(uploadCase.ServiceType)} :null/empty]", Error = "null/empty" });
                    caseErrors.Add($"[{nameof(uploadCase.ServiceType)}=null/empty]");
                }
                var servicetype = string.IsNullOrWhiteSpace(uploadCase.ServiceType)
                    ? await context.InvestigationServiceType.FirstOrDefaultAsync(i => i.InsuranceType == caseType)  // Case 1: ServiceType is null, get first record matching LineOfBusinessId
                    : await context.InvestigationServiceType
                        .FirstOrDefaultAsync(b => b.Code.ToLower() == uploadCase.ServiceType.ToLower() && b.InsuranceType == caseType)  // Case 2: Try matching Code + LineOfBusinessId
                      ?? await context.InvestigationServiceType
                        .FirstOrDefaultAsync(b => b.InsuranceType == caseType);  // Case 3: If no match, retry ignoring LineOfBusinessId


                if (!string.IsNullOrWhiteSpace(uploadCase.Amount) && decimal.TryParse(uploadCase.Amount, out var amount))
                {
                    uploadCase.Amount = amount.ToString();
                }
                else
                {
                    case_errors.Add(new UploadError { UploadData = $"[{nameof(uploadCase.Amount)}: Invalid]", Error = $"Invalid assured amount {uploadCase.Amount}" });
                    caseErrors.Add($"[{nameof(uploadCase.Amount)}=`{uploadCase.Amount}` null/invalid]");

                }
                DateTime issueDate, dateOfIncident;
                bool isIssueDateValid = false, isIncidentDateValid = false;

                // Validate IssueDate
                if (!string.IsNullOrWhiteSpace(uploadCase.IssueDate) && DateTime.TryParseExact(uploadCase.IssueDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out issueDate))
                {
                    if (issueDate > DateTime.Today)
                    {
                        case_errors.Add(new UploadError
                        {
                            UploadData = "Issue Date",
                            Error = $"Issue date ({issueDate:dd-MM-yyyy}) cannot be in the future"
                        });
                        caseErrors.Add($"[{nameof(uploadCase.IssueDate)} =`{issueDate:dd-MM-yyyy}` cannot be in the future]");
                    }
                    else
                    {
                        isIssueDateValid = true;
                        uploadCase.IssueDate = issueDate.ToString("dd-MM-yyyy");
                    }
                }
                else
                {
                    issueDate = DateTime.Now;
                    uploadCase.IssueDate = issueDate.ToString("dd-MM-yyyy");
                    case_errors.Add(new UploadError { UploadData = $"[{nameof(uploadCase.IssueDate)}: {uploadCase.IssueDate} Invalid]", Error = $"Invalid issue date {uploadCase.IssueDate}" });
                    caseErrors.Add($"[{nameof(uploadCase.IssueDate)}=`{uploadCase.IssueDate}` null/invalid]");
                }

                // Validate IncidentDate
                if (!string.IsNullOrWhiteSpace(uploadCase.IncidentDate) && DateTime.TryParseExact(uploadCase.IncidentDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOfIncident))
                {
                    if (dateOfIncident > DateTime.Today)
                    {
                        case_errors.Add(new UploadError
                        {
                            UploadData = "Incident date",
                            Error = $"Incident date ({dateOfIncident:dd-MM-yyyy}) cannot be in the future"
                        });
                        caseErrors.Add($"[{nameof(uploadCase.IncidentDate)}=`{dateOfIncident:dd-MM-yyyy}` cannot be in the future]");
                    }
                    else
                    {
                        isIncidentDateValid = true;
                        uploadCase.IncidentDate = dateOfIncident.ToString("dd-MM-yyyy");
                    }
                }
                else
                {
                    dateOfIncident = DateTime.Now;
                    uploadCase.IncidentDate = dateOfIncident.ToString("dd-MM-yyyy");
                    case_errors.Add(new UploadError { UploadData = $"[{nameof(uploadCase.IncidentDate)}: {uploadCase.IncidentDate} Invalid]", Error = $"Invalid incident date {uploadCase.IncidentDate}" });
                    caseErrors.Add($"[{nameof(uploadCase.IncidentDate)}=`{uploadCase.IncidentDate}` null/invalid]");
                }

                // Check chronological order
                if (isIssueDateValid && isIncidentDateValid && issueDate > dateOfIncident)
                {
                    case_errors.Add(new UploadError
                    {
                        UploadData = $"[Date comparison : Issue date ({uploadCase.IssueDate}) must be on or before Incident date ({uploadCase.IncidentDate})]",
                        Error = $"Issue date =`{uploadCase.IssueDate}` must be on or before Incident date =`{uploadCase.IncidentDate}`"
                    });
                    caseErrors.Add($"[Date comparison : {nameof(uploadCase.IssueDate)}=`{uploadCase.IssueDate}` must be on or before {nameof(uploadCase.IncidentDate)} =`{uploadCase.IncidentDate}`]");
                }

                if (string.IsNullOrWhiteSpace(uploadCase.Reason))
                {
                    case_errors.Add(new UploadError { UploadData = $"[{nameof(uploadCase.Reason)} : null/empty]", Error = $"null/empty" });
                    caseErrors.Add($"[{nameof(uploadCase.Reason)}=null/empty]");
                }
                var caseEnabler = string.IsNullOrWhiteSpace(uploadCase.Reason) ?
                    await context.CaseEnabler.FirstOrDefaultAsync() :
                    await context.CaseEnabler.FirstOrDefaultAsync(c => c.Code.ToLower() == uploadCase.Reason.Trim().ToLower())
                    ?? await context.CaseEnabler.FirstOrDefaultAsync();


                if (string.IsNullOrWhiteSpace(uploadCase.Department))
                {
                    case_errors.Add(new UploadError { UploadData = $"[{nameof(uploadCase.Department)} : null/empty]", Error = $"null/empty" });
                    caseErrors.Add($"[{nameof(uploadCase.Department)}=null/empty]");
                }

                if (string.IsNullOrWhiteSpace(uploadCase.Cause))
                {
                    case_errors.Add(new UploadError { UploadData = $"[{nameof(uploadCase.Cause)} : null/empty]", Error = $"null/empty" });
                    caseErrors.Add($"[{nameof(uploadCase.Cause)}=null/empty]");
                }

                var department = string.IsNullOrWhiteSpace(uploadCase.Department) ?
                   await context.CostCentre.FirstOrDefaultAsync() :
                   await context.CostCentre.FirstOrDefaultAsync(c => c.Code.ToLower() == uploadCase.Department.Trim().ToLower())
                   ?? await context.CostCentre.FirstOrDefaultAsync();

                var savedNewImage = await caseImageCreationService.GetImagesWithDataInSubfolder(model, uploadCase.CaseId?.ToLower(), POLICY_IMAGE);
                var extension = Path.GetExtension(POLICY_IMAGE).ToLower();
                string filePath = string.Empty;
                if (savedNewImage == null)
                {
                    case_errors.Add(new UploadError { UploadData = "[Policy Image: null/not found]", Error = "null/not found" });
                    caseErrors.Add($"[Policy Image=`{POLICY_IMAGE}`  null/not found]");
                }
                else
                {
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(savedNewImage, extension, "Case", uploadCase.CaseId);
                    filePath = relativePath;
                }
                var policyDetail = new PolicyDetail
                {
                    ContractNumber = uploadCase.CaseId,
                    SumAssuredValue = Convert.ToDecimal(uploadCase.Amount),
                    ContractIssueDate = DateTime.ParseExact(uploadCase.IssueDate, "dd-MM-yyyy", CultureInfo.InvariantCulture),
                    //ClaimType = (ClaimType.DEATH)Enum.Parse(typeof(ClaimType), rowData[3]?.Trim()),
                    InvestigationServiceTypeId = servicetype.InvestigationServiceTypeId,
                    DateOfIncident = DateTime.ParseExact(uploadCase.IncidentDate, "dd-MM-yyyy", CultureInfo.InvariantCulture),
                    CauseOfLoss = !string.IsNullOrWhiteSpace(uploadCase.Cause?.Trim()) ? uploadCase.Cause.Trim() : "UNKNOWN",
                    CaseEnablerId = caseEnabler.CaseEnablerId,
                    CostCentreId = department.CostCentreId,
                    InsuranceType = caseType,
                    DocumentPath = filePath,
                    DocumentImageExtension = extension,
                    Updated = DateTime.Now,
                    UpdatedBy = companyUser.Email
                };
                var reportTemplate = await cloneService.DeepCloneReportTemplate(companyUser.ClientCompanyId.Value, caseType);
                var caseTask = new InvestigationTask
                {
                    CreatedUser = companyUser.Email,
                    Status = CONSTANTS.CASE_STATUS.INITIATED,
                    SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED,
                    CaseOwner = companyUser.Email,
                    Updated = DateTime.Now,
                    UpdatedBy = companyUser.Email,
                    Deleted = false,
                    AssignedToAgency = false,
                    IsReady2Assign = ValidateDataCase(uploadCase),
                    ORIGIN = fileOrFTP,
                    ClientCompanyId = companyUser.ClientCompanyId,
                    CreatorSla = companyUser.ClientCompany.CreatorSla,
                    IsNew = true,
                    ReportTemplateId = reportTemplate.Id,
                    ReportTemplate = reportTemplate
                };
                caseTask.PolicyDetail = policyDetail;
                caseTask.CustomerDetail = customer;
                caseTask.BeneficiaryDetail = beneficiary;
                caseTask.IsReady2Assign = caseTask.IsValidCaseData();
                case_errors.AddRange(customer_errors);
                case_errors.AddRange(beneficiary_errors);

                return new UploadResult { InvestigationTask = caseTask, ErrorDetail = case_errors, Errors = caseErrors };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Case Detail");
                return new UploadResult { InvestigationTask = null, ErrorDetail = case_errors, Errors = caseErrors };
            }
        }
    }
}
