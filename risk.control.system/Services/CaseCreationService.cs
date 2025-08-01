using System.Globalization;
using System.IO.Compression;

using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using static risk.control.system.AppConstant.CONSTANTS;

namespace risk.control.system.Services
{
    public interface ICaseCreationService
    {
        Task<UploadResult> FileUpload(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, FileOnFileSystemModel model);
    }
    public class CaseCreationService : ICaseCreationService
    {

        private readonly ApplicationDbContext context;
        private readonly ICustomApiCLient customApiCLient;
        private readonly ICloneReportService cloneService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ILogger<CaseCreationService> logger;

        public CaseCreationService(ApplicationDbContext context, ICustomApiCLient customApiCLient,
            ICloneReportService cloneService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<CaseCreationService> logger)
        {
            this.context = context;
            this.customApiCLient = customApiCLient;
            this.cloneService = cloneService;
            this.webHostEnvironment = webHostEnvironment;
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
        private async Task<UploadResult> AddCaseDetail(UploadCase uploadCase, ClientCompanyApplicationUser companyUser, FileOnFileSystemModel model)
        {
            var case_errors = new List<UploadError>();
            var caseErrors = new List<string>();
            try
            {

                var customerTask = AddCustomer(companyUser, uploadCase, model.ByteData);
                var beneficiaryTask = AddBeneficiary(companyUser, uploadCase, model.ByteData);
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
                    caseErrors.Add($"[{nameof(uploadCase.ServiceType)} : null/empty]");
                }
                var servicetype = string.IsNullOrWhiteSpace(uploadCase.ServiceType)
                    ? context.InvestigationServiceType.FirstOrDefault(i => i.InsuranceType == caseType)  // Case 1: ServiceType is null, get first record matching LineOfBusinessId
                    : context.InvestigationServiceType
                        .FirstOrDefault(b => b.Code.ToLower() == uploadCase.ServiceType.ToLower() && b.InsuranceType == caseType)  // Case 2: Try matching Code + LineOfBusinessId
                      ?? context.InvestigationServiceType
                        .FirstOrDefault(b => b.InsuranceType == caseType);  // Case 3: If no match, retry ignoring LineOfBusinessId

                var savedNewImage = GetImagesWithDataInSubfolder(model.ByteData, uploadCase.CaseId?.ToLower(), POLICY_IMAGE);
                if (savedNewImage == null)
                {
                    case_errors.Add(new UploadError { UploadData = "[Policy Image: null/not found]", Error = "null/not found" });
                    caseErrors.Add($"[Policy Image=`{POLICY_IMAGE}` : null/not found]");
                }
                if (!string.IsNullOrWhiteSpace(uploadCase.Amount) && decimal.TryParse(uploadCase.Amount, out var amount))
                {
                    uploadCase.Amount = amount.ToString();
                }
                else
                {
                    case_errors.Add(new UploadError { UploadData = $"[{nameof(uploadCase.Amount)}: Invalid]", Error = $"Invalid assured amount {uploadCase.Amount}" });
                    caseErrors.Add($"[{nameof(uploadCase.Amount)}={uploadCase.Amount} : Invalid]");

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
                        caseErrors.Add($"{nameof(uploadCase.IssueDate)} =`{issueDate:dd-MM-yyyy}` cannot be in the future");
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
                    caseErrors.Add($"[{nameof(uploadCase.IssueDate)}=`{uploadCase.IssueDate}` Invalid]");
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
                        caseErrors.Add($"{nameof(uploadCase.IncidentDate)}=`{dateOfIncident:dd-MM-yyyy}` cannot be in the future");
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
                    caseErrors.Add($"[{nameof(uploadCase.IncidentDate)}=`{uploadCase.IncidentDate}` Invalid]");
                }

                // Check chronological order
                if (isIssueDateValid && isIncidentDateValid && issueDate >= dateOfIncident)
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
                    context.CaseEnabler.FirstOrDefault() :
                    context.CaseEnabler.FirstOrDefault(c => c.Code.ToLower() == uploadCase.Reason.Trim().ToLower())
                    ?? context.CaseEnabler.FirstOrDefault();


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
                   context.CostCentre.FirstOrDefault() :
                   context.CostCentre.FirstOrDefault(c => c.Code.ToLower() == uploadCase.Department.Trim().ToLower())
                   ?? context.CostCentre.FirstOrDefault();

                string noImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", POLICY_IMAGE);

                var policyDetail = new PolicyDetail
                {
                    ContractNumber = uploadCase.CaseId,
                    SumAssuredValue = Convert.ToDecimal(uploadCase.Amount),
                    ContractIssueDate = DateTime.ParseExact(uploadCase.IssueDate, "dd-MM-yyyy", CultureInfo.InvariantCulture),
                    //ClaimType = (ClaimType.DEATH)Enum.Parse(typeof(ClaimType), rowData[3]?.Trim()),
                    InvestigationServiceTypeId = servicetype?.InvestigationServiceTypeId,
                    DateOfIncident = DateTime.ParseExact(uploadCase.IncidentDate, "dd-MM-yyyy", CultureInfo.InvariantCulture),
                    CauseOfLoss = !string.IsNullOrWhiteSpace(uploadCase.Cause?.Trim()) ? uploadCase.Cause.Trim() : "UNKNOWN",
                    CaseEnablerId = caseEnabler.CaseEnablerId,
                    CostCentreId = department.CostCentreId,
                    InsuranceType = caseType,
                    DocumentImage = savedNewImage ?? File.ReadAllBytes(noImagePath),
                    DocumentImageExtension = Path.GetExtension(POLICY_IMAGE),
                    Updated = DateTime.Now,
                    UpdatedBy = companyUser.Email
                };
                //if (!policyDetail.IsValidCaseDetail())
                //{
                //    return null;
                //}
                //if (customer is null || !policyDetail.IsValidCustomerForUpload(customer) || beneficiary is null || !policyDetail.IsValidBeneficiaryForUpload(beneficiary))
                //{
                //    return null;
                //}
                var reportTemplate = await cloneService.DeepCloneReportTemplate(companyUser.ClientCompanyId.Value, caseType);
                var claim = new InvestigationTask
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
                    ORIGIN = model.FileOrFtp,
                    ClientCompanyId = companyUser.ClientCompanyId,
                    CreatorSla = companyUser.ClientCompany.CreatorSla,
                    IsNew = true,
                    ReportTemplateId = reportTemplate.Id,
                    ReportTemplate = reportTemplate
                };
                claim.PolicyDetail = policyDetail;
                claim.CustomerDetail = customer;
                claim.BeneficiaryDetail = beneficiary;
                claim.IsReady2Assign = claim.IsValidCaseData();
                case_errors.AddRange(customer_errors);
                case_errors.AddRange(beneficiary_errors);

                return new UploadResult { InvestigationTask = claim, ErrorDetail = case_errors, Errors = caseErrors };
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                return new UploadResult { InvestigationTask = null, ErrorDetail = case_errors, Errors = caseErrors };
            }
        }
        private async Task<(CustomerDetail, List<UploadError>, List<string>)> AddCustomer(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, byte[] data)
        {
            var errors = new List<UploadError>();
            var errorCustomer = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(uploadCase.CaseId))
                {
                    errors.Add(new UploadError { UploadData = $"[{nameof(uploadCase.CaseId)}: null/empty]", Error = "null/empty" });
                    errorCustomer.Add($"[{nameof(uploadCase.CaseId)}=null/empty]");
                }

                if (string.IsNullOrWhiteSpace(uploadCase.CustomerName))
                {
                    errors.Add(new UploadError { UploadData = $"[{nameof(uploadCase.CustomerName)}: null/empty]", Error = "null/empty" });
                    errorCustomer.Add($"[{nameof(uploadCase.CustomerName)}=null/empty]");
                }

                if (!string.IsNullOrWhiteSpace(uploadCase.CustomerType) && Enum.TryParse(typeof(CustomerType), uploadCase.CustomerType, out var customerTypeEnum))
                {
                    uploadCase.CustomerType = customerTypeEnum.ToString();
                }
                else
                {
                    uploadCase.CustomerType = CustomerType.UNKNOWN.ToString();
                }

                if (!string.IsNullOrWhiteSpace(uploadCase.Gender) && Enum.TryParse<Gender>(uploadCase.Gender, true, out var gender))
                {
                    uploadCase.Gender = gender.ToString();
                }
                else
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"[Customer gender : Invalid {uploadCase.Gender}]",
                        Error = $"gender {uploadCase.Gender} invalid"
                    });
                    errorCustomer.Add($"[Customer gender=Invalid {uploadCase.Gender}]");
                }

                if (string.IsNullOrWhiteSpace(uploadCase.CustomerPincode))
                {
                    errors.Add(new UploadError { UploadData = $"{nameof(uploadCase.CustomerPincode)}: null/empty]", Error = "null/empty" });
                    errorCustomer.Add($"{nameof(uploadCase.CustomerPincode)}=null/empty]");
                }

                if (string.IsNullOrWhiteSpace(uploadCase.CustomerDistrictName))
                {
                    errors.Add(new UploadError { UploadData = $"[{nameof(uploadCase.CustomerDistrictName)}: null/empty]", Error = "null/empty" });
                    errorCustomer.Add($"[{nameof(uploadCase.CustomerDistrictName)}=null/empty]");
                }
                PinCode? pinCode = null;

                if (!string.IsNullOrWhiteSpace(uploadCase.CustomerPincode) && !string.IsNullOrWhiteSpace(uploadCase.CustomerDistrictName))
                {
                    pinCode = context.PinCode
                                           .Include(p => p.District)
                                           .Include(p => p.State)
                                           .Include(p => p.Country)
                                           .FirstOrDefault(p => p.Code == uploadCase.CustomerPincode &&
                                           p.District.Name.ToLower().Contains(uploadCase.CustomerDistrictName.ToLower()));
                    if (pinCode is null || pinCode.CountryId != companyUser.ClientCompany.CountryId)
                    {
                        errors.Add(new UploadError
                        {
                            UploadData = $"[Customer Pincode {uploadCase.CustomerPincode} And/Or District {uploadCase.CustomerDistrictName} not found]",
                            Error = $"Pincode {uploadCase.CustomerPincode} And/Or District {uploadCase.CustomerDistrictName} not found"
                        });
                        errorCustomer.Add($"[Customer Pincode={uploadCase.CustomerPincode} And/Or District={uploadCase.CustomerDistrictName} not found]");
                    }
                }

                var imagesWithData = GetImagesWithDataInSubfolder(data, uploadCase.CaseId?.ToLower(), CUSTOMER_IMAGE);
                if (imagesWithData is null)
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"[Customer image : Image {CUSTOMER_IMAGE} null/not found]",
                        Error = $"Image {CUSTOMER_IMAGE} null/not found"
                    });
                    errorCustomer.Add($"[Customer Image={CUSTOMER_IMAGE} null/not found]");
                }
                if (string.IsNullOrWhiteSpace(uploadCase.CustomerAddressLine))
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"[{nameof(uploadCase.CustomerAddressLine)} : null/empty",
                        Error = "null/empty"
                    });
                    errorCustomer.Add($"[{nameof(uploadCase.CustomerAddressLine)}=null/empty]");
                }

                if (!string.IsNullOrWhiteSpace(uploadCase.Education) && Enum.TryParse<Education>(uploadCase.Education, true, out var educationEnum))
                {
                    uploadCase.Education = educationEnum.ToString();
                }
                else
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"[Customer education : {uploadCase.Education} invalid]",
                        Error = $"Education {uploadCase.Education} invalid"
                    });
                    errorCustomer.Add($"[Customer education ={uploadCase.Education} invalid]");
                }
                if (!string.IsNullOrWhiteSpace(uploadCase.Occupation) && Enum.TryParse<Occupation>(uploadCase.Occupation, true, out var occupationEnum))
                {
                    uploadCase.Occupation = occupationEnum.ToString();
                }
                else
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"[Customer occupation : {uploadCase.Occupation} invalid]",
                        Error = $"occupation {uploadCase.Occupation} invalid"
                    });
                    errorCustomer.Add($"[Customer occupation={uploadCase.Occupation} invalid]");
                }

                if (!string.IsNullOrWhiteSpace(uploadCase.Income) && Enum.TryParse<Income>(uploadCase.Income, true, out var incomeEnum))
                {
                    uploadCase.Income = incomeEnum.ToString();
                }
                else
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"[Customer income : {uploadCase.Income} invalid]",
                        Error = $"income {uploadCase.Income} invalid"
                    });
                    errorCustomer.Add($"[Customer income={uploadCase.Income} invalid]");
                }

                if (!string.IsNullOrWhiteSpace(uploadCase.CustomerDob) && DateTime.TryParseExact(uploadCase.CustomerDob, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var customerDob))
                {
                    uploadCase.CustomerDob = customerDob.ToString("dd-MM-yyyy");
                }
                else
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"[Customer Date of Birth : {uploadCase.CustomerDob} invalid]",
                        Error = $"Date of Birth={uploadCase.CustomerDob} invalid"
                    });
                    errorCustomer.Add($"[Customer Date of Birth={uploadCase.CustomerDob} invalid]");
                }

                string noImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", CUSTOMER_IMAGE);

                var customerDetail = new CustomerDetail
                {
                    Name = uploadCase.CustomerName,
                    CustomerType = (CustomerType)Enum.Parse(typeof(CustomerType), uploadCase.CustomerType),
                    Gender = (Gender)Enum.Parse(typeof(Gender), uploadCase.Gender),
                    DateOfBirth = DateTime.ParseExact(uploadCase.CustomerDob, "dd-MM-yyyy", CultureInfo.InvariantCulture),
                    ContactNumber = (uploadCase.CustomerContact),
                    Education = (Education)Enum.Parse(typeof(Education), uploadCase.Education),
                    Occupation = (Occupation)Enum.Parse(typeof(Occupation), uploadCase.Occupation),
                    Income = (Income)Enum.Parse(typeof(Income), uploadCase.Income),
                    Addressline = uploadCase?.CustomerAddressLine,
                    CountryId = pinCode?.CountryId,
                    PinCodeId = pinCode?.PinCodeId,
                    StateId = pinCode?.StateId,
                    DistrictId = pinCode?.DistrictId,
                    //Description = rowData[20]?.Trim(),
                    ProfilePicture = imagesWithData,
                    ProfilePictureExtension = Path.GetExtension(CUSTOMER_IMAGE),
                    UpdatedBy = companyUser.Email,
                    Updated = DateTime.Now
                };
                if (pinCode != null)
                {
                    var address = customerDetail.Addressline + ", " +
                    pinCode?.District?.Name + ", " +
                    pinCode?.State?.Name + ", " +
                    pinCode?.Country?.Code + ", " +
                    pinCode?.Code;

                    var (Latitude, Longitude) = await customApiCLient.GetCoordinatesFromAddressAsync(address);
                    customerDetail.Latitude = Latitude;
                    customerDetail.Longitude = Longitude;
                    var customerLatLong = Latitude + "," + Longitude;
                    var url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}",
                            customerLatLong, Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY"));
                    customerDetail.CustomerLocationMap = url;
                }

                return (customerDetail, errors, errorCustomer);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                return (null, errors, errorCustomer);
            }
        }
        private async Task<(BeneficiaryDetail, List<UploadError>, List<string>)> AddBeneficiary(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, byte[] data)
        {
            var errors = new List<UploadError>();
            var errorBeneficiary = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(uploadCase.BeneficiaryName))
                {
                    errors.Add(new UploadError { UploadData = $"{nameof(uploadCase.BeneficiaryName)} : null/empty]", Error = "null/empty" });
                    errorBeneficiary.Add($"{nameof(uploadCase.BeneficiaryName)}=null/empty]");
                }

                if (string.IsNullOrWhiteSpace(uploadCase.Relation))
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"[Beneficiary relation : {uploadCase.Relation} null/empty/invalid]",
                        Error = $"Relation {uploadCase.Relation} null/empty/invalid"
                    });
                    errorBeneficiary.Add($"[Beneficiary relation={uploadCase.Relation} null/empty/invalid]");
                }

                if (string.IsNullOrWhiteSpace(uploadCase.CustomerPincode))
                {
                    errors.Add(new UploadError { UploadData = "[Beneficiary pincode: null/empty]", Error = "null/empty" });
                    errorBeneficiary.Add("[Beneficiary pincode=null/empty]");
                }

                if (string.IsNullOrWhiteSpace(uploadCase.BeneficiaryDistrictName))
                {
                    errors.Add(new UploadError { UploadData = "[Beneficiary District Name : null/empty]", Error = "null/empty" });
                    errorBeneficiary.Add("[Beneficiary District Name=null/empty]");
                }
                PinCode? pinCode = null;
                if (!string.IsNullOrWhiteSpace(uploadCase.BeneficiaryPincode) && !string.IsNullOrWhiteSpace(uploadCase.BeneficiaryDistrictName))
                {
                    pinCode = context.PinCode.Include(p => p.District)
                                                    .Include(p => p.State)
                                                    .Include(p => p.Country)
                                                    .FirstOrDefault(p => p.Code == uploadCase.BeneficiaryPincode &&
                                                    p.District.Name.ToLower().Contains(uploadCase.BeneficiaryDistrictName.ToLower()));
                    if (pinCode is null || pinCode.CountryId != companyUser.ClientCompany.CountryId)
                    {
                        errors.Add(new UploadError
                        {
                            UploadData = $"[Beneficiary Pincode: {uploadCase?.BeneficiaryPincode}And / Or District : {uploadCase?.BeneficiaryDistrictName} not found]",
                            Error = $"pincode {uploadCase?.BeneficiaryPincode}/district {uploadCase?.BeneficiaryDistrictName} not found"
                        });
                        errorBeneficiary.Add($"[Beneficiary Pincode={uploadCase?.BeneficiaryPincode} And / Or District={uploadCase?.BeneficiaryDistrictName} not found]");
                    }
                }

                var relation = string.IsNullOrWhiteSpace(uploadCase.Relation)
                    ? context.BeneficiaryRelation.FirstOrDefault()  // Get first record from the table
                    : context.BeneficiaryRelation.FirstOrDefault(b => b.Code.ToLower() == uploadCase.Relation.ToLower()) // Get matching record
                    ?? context.BeneficiaryRelation.FirstOrDefault();

                var beneficiaryNewImage = GetImagesWithDataInSubfolder(data, uploadCase.CaseId?.ToLower(), BENEFICIARY_IMAGE);
                if (beneficiaryNewImage == null)
                {
                    errors.Add(new UploadError
                    {
                        UploadData = "[Beneficiary image : null/empty]",
                        Error = "null/empty"
                    });
                    errorBeneficiary.Add("[Beneficiary image=null/empty]");
                }
                if (!string.IsNullOrWhiteSpace(uploadCase.BeneficiaryIncome) && Enum.TryParse<Income>(uploadCase.BeneficiaryIncome, true, out var incomeEnum))
                {
                    uploadCase.BeneficiaryIncome = incomeEnum.ToString();
                }
                else
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"Beneficiary income : {uploadCase.BeneficiaryIncome} invalid]",
                        Error = $"income {uploadCase.BeneficiaryIncome} invalid"
                    });
                    errorBeneficiary.Add($"Beneficiary income={uploadCase.BeneficiaryIncome} invalid]");
                }
                if (!string.IsNullOrWhiteSpace(uploadCase.BeneficiaryDob) && DateTime.TryParseExact(uploadCase.BeneficiaryDob, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var beneficiaryDob))
                {
                    uploadCase.BeneficiaryDob = beneficiaryDob.ToString("dd-MM-yyyy");
                }
                else
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"[Beneficiary Date of Birth: Invalid {uploadCase.BeneficiaryDob}]",
                        Error = $"Invalid {uploadCase.BeneficiaryDob}"
                    });
                    errorBeneficiary.Add($"[Beneficiary Date of Birth={uploadCase.BeneficiaryDob} Invalid ]");
                }

                if (string.IsNullOrWhiteSpace(uploadCase.BeneficiaryAddressLine))
                {
                    errors.Add(new UploadError
                    {
                        UploadData = "[Beneficiary addressline : null/empty]",
                        Error = "null/empty"
                    });
                    errorBeneficiary.Add("[Beneficiary addressline=null/empty]");
                }

                string noImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", BENEFICIARY_IMAGE);
                var beneficairy = new BeneficiaryDetail
                {
                    Name = uploadCase.BeneficiaryName,
                    BeneficiaryRelationId = relation.BeneficiaryRelationId,
                    DateOfBirth = DateTime.ParseExact(uploadCase.BeneficiaryDob, "dd-MM-yyyy", CultureInfo.InvariantCulture),
                    Income = (Income)Enum.Parse(typeof(Income), uploadCase.BeneficiaryIncome),
                    ContactNumber = uploadCase.BeneficiaryContact,
                    Addressline = uploadCase.BeneficiaryAddressLine,
                    PinCodeId = pinCode?.PinCodeId,
                    DistrictId = pinCode?.DistrictId,
                    StateId = pinCode?.StateId,
                    CountryId = pinCode?.CountryId,
                    ProfilePicture = beneficiaryNewImage,
                    ProfilePictureExtension = Path.GetExtension(BENEFICIARY_IMAGE),
                    Updated = DateTime.Now,
                    UpdatedBy = companyUser.Email
                };
                if (pinCode != null)
                {
                    var address = beneficairy.Addressline + ", " +
                    pinCode?.District?.Name + ", " +
                    pinCode?.State?.Name + ", " +
                    pinCode?.Country?.Code + ", " +
                    pinCode?.Code;

                    var (Latitude, Longitude) = await customApiCLient.GetCoordinatesFromAddressAsync(address);

                    var latLong = Latitude + "," + Longitude;
                    beneficairy.Latitude = Latitude;
                    beneficairy.Longitude = Longitude;
                    var url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}",
                            latLong, Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY"));
                    beneficairy.BeneficiaryLocationMap = url;
                }

                return (beneficairy, errors, errorBeneficiary);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                return (null, errors, errorBeneficiary);
            }
        }

        public static byte[] GetImagesWithDataInSubfolder(byte[] zipData, string subfolderName, string filename = "")
        {
            if (string.IsNullOrWhiteSpace(subfolderName) || string.IsNullOrWhiteSpace(filename))
            {
                return null;
            }
            List<(string FileName, byte[] ImageData)> images = new List<(string, byte[])>();

            using (MemoryStream zipStream = new MemoryStream(zipData))
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                // Loop through each entry in the archive
                foreach (var entry in archive.Entries)
                {
                    // Convert path to standard format (Windows)
                    string folderPath = entry.FullName.Replace("/", "\\");

                    // Check if the entry is inside the desired subfolder and is an image file
                    if (folderPath.ToLower().Contains("\\" + subfolderName + "\\") && IsImageFile(entry.FullName))
                    {
                        // Extract image data
                        using (MemoryStream imageStream = new MemoryStream())
                        {
                            using (Stream entryStream = entry.Open())
                            {
                                entryStream.CopyTo(imageStream);
                            }

                            // Add file name and byte array to the result list
                            images.Add((entry.Name, imageStream.ToArray()));
                        }
                    }
                }
            }

            var image = images.FirstOrDefault(i => i.FileName == filename);
            if (image.ImageData != null)
            {
                var compressed = CompressImage.ProcessCompress(image.ImageData, ".jpg");
                return compressed;
            }
            return null;
        }

        private static bool IsImageFile(string filePath)
        {
            // Check if the file is an image based on file extension
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
            string extension = Path.GetExtension(filePath)?.ToLower();
            return imageExtensions.Contains(extension);
        }

        public async Task<UploadResult> FileUpload(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, FileOnFileSystemModel model)
        {
            try
            {
                if (companyUser is null || uploadCase is null || model is null)
                {
                    return null;
                }
                var claimUploaded = await AddCaseDetail(uploadCase, companyUser, model);
                if (claimUploaded == null)
                {
                    return null;
                }
                return claimUploaded;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }
    }
}
