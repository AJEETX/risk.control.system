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
        public CaseCreationService(ApplicationDbContext context, ICustomApiCLient customApiCLient, ICloneReportService cloneService, IWebHostEnvironment webHostEnvironment)
        {
            this.context = context;
            this.customApiCLient = customApiCLient;
            this.cloneService = cloneService;
            this.webHostEnvironment = webHostEnvironment;
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
            var customerTask = AddCustomer(companyUser, uploadCase, model.ByteData);
            var beneficiaryTask = AddBeneficiary(companyUser, uploadCase, model.ByteData);
            await Task.WhenAll(customerTask, beneficiaryTask);

            // Get the results
            var (customer, customer_errors) = await customerTask;
            var (beneficiary, beneficiary_errors) = await beneficiaryTask;
            InsuranceType caseType = InsuranceType.CLAIM;
            if (uploadCase.InsuranceType != InsuranceType.CLAIM.GetEnumDisplayName())
            {
                caseType = InsuranceType.UNDERWRITING;
            }

            if (string.IsNullOrWhiteSpace(uploadCase.ServiceType))
            {
                case_errors.Add(new UploadError { UploadData = "Service type", Error = "null/empty" });
            }
            var servicetype = string.IsNullOrWhiteSpace(uploadCase.ServiceType)
                ? context.InvestigationServiceType.FirstOrDefault(i => i.InsuranceType == caseType)  // Case 1: ServiceType is null, get first record matching LineOfBusinessId
                : context.InvestigationServiceType
                    .FirstOrDefault(b => b.Code.ToLower() == uploadCase.ServiceType.ToLower() && b.InsuranceType == caseType)  // Case 2: Try matching Code + LineOfBusinessId
                  ?? context.InvestigationServiceType
                    .FirstOrDefault(b => b.InsuranceType == caseType);  // Case 3: If no match, retry ignoring LineOfBusinessId

            var savedNewImage = GetImagesWithDataInSubfolder(model.ByteData, uploadCase.CaseId.ToLower(), POLICY_IMAGE);
            if (savedNewImage == null)
            {
                case_errors.Add(new UploadError { UploadData = "Policy image/doc", Error = "null/not found" });
            }
            if (!string.IsNullOrWhiteSpace(uploadCase.Amount) && decimal.TryParse(uploadCase.Amount, out var amount))
            {
                uploadCase.Amount = amount.ToString();
            }
            else
            {
                uploadCase.Amount = "1000";
                case_errors.Add(new UploadError { UploadData = "Assured amount", Error = $"Invalid assured amount {uploadCase.Amount}" });

            }
            DateTime issueDate, dateOfIncident;
            bool isIssueDateValid = false, isIncidentDateValid = false;

            // Validate IssueDate
            if (!string.IsNullOrWhiteSpace(uploadCase.IssueDate) && DateTime.TryParseExact(uploadCase.IssueDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out issueDate))
            {
                if (issueDate >= DateTime.Today)
                {
                    case_errors.Add(new UploadError
                    {
                        UploadData = "Issue date",
                        Error = $"Issue date ({issueDate:dd-MM-yyyy}) cannot be in the future"
                    });
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
                case_errors.Add(new UploadError { UploadData = "Issue date", Error = $"Invalid issue date {uploadCase.IssueDate}" });
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
                case_errors.Add(new UploadError { UploadData = "Incident date", Error = $"Invalid incident date {uploadCase.IncidentDate}" });
            }

            // Check chronological order
            if (isIssueDateValid && isIncidentDateValid && issueDate > dateOfIncident)
            {
                case_errors.Add(new UploadError
                {
                    UploadData = "Date comparison",
                    Error = $"Issue date ({uploadCase.IssueDate}) must be on or before Incident date ({uploadCase.IncidentDate})"
                });
            }


            if (string.IsNullOrWhiteSpace(uploadCase.Reason))
            {
                case_errors.Add(new UploadError { UploadData = " Reason", Error = $"null/empty" });
            }
            var caseEnabler = string.IsNullOrWhiteSpace(uploadCase.Reason) ?
                context.CaseEnabler.FirstOrDefault() :
                context.CaseEnabler.FirstOrDefault(c => c.Code.ToLower() == uploadCase.Reason.Trim().ToLower())
                ?? context.CaseEnabler.FirstOrDefault();


            if (string.IsNullOrWhiteSpace(uploadCase.Department))
            {
                case_errors.Add(new UploadError { UploadData = " Department", Error = $"null/empty" });
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
                CauseOfLoss = !string.IsNullOrWhiteSpace(uploadCase.Cause.Trim()) ? uploadCase.Cause.Trim() : "UNKNOWN",
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
            return new UploadResult { InvestigationTask = claim, ErrorDetail = case_errors };
        }
        private async Task<(CustomerDetail, List<UploadError>)> AddCustomer(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, byte[] data)
        {
            var errors = new List<UploadError>();
            if (string.IsNullOrWhiteSpace(uploadCase.CustomerName))
            {
                errors.Add(new UploadError { UploadData = "Customer name", Error = "null/empty" });
            }
            var pinCode = context.PinCode
                                    .Include(p => p.District)
                                    .Include(p => p.State)
                                    .Include(p => p.Country)
                                    .FirstOrDefault(p => p.Code == uploadCase.CustomerPincode &&
                                    p.District.Name.ToLower().Contains(uploadCase.CustomerDistrictName.ToLower()));
            if (pinCode is null || pinCode.CountryId != companyUser.ClientCompany.CountryId)
            {
                errors.Add(new UploadError
                {
                    UploadData = "customer pincode/district",
                    Error = $"pincode {uploadCase.CustomerPincode}/district {uploadCase.CustomerDistrictName} null/not found"
                });
            }
            var imagesWithData = GetImagesWithDataInSubfolder(data, uploadCase.CaseId.ToLower(), CUSTOMER_IMAGE);
            if (imagesWithData is null)
            {
                errors.Add(new UploadError
                {
                    UploadData = "Customer image",
                    Error = $"Image {CUSTOMER_IMAGE} null/not found"
                });
            }
            if (string.IsNullOrWhiteSpace(uploadCase.CustomerAddressLine))
            {
                errors.Add(new UploadError
                {
                    UploadData = "Customer addressline",
                    Error = "null/empty"
                });
            }
            if (!string.IsNullOrWhiteSpace(uploadCase.Gender) && Enum.TryParse<Gender>(uploadCase.Gender, true, out var gender))
            {
                uploadCase.Gender = gender.ToString();
            }
            else
            {
                uploadCase.Gender = Gender.UNKNOWN.ToString();
                errors.Add(new UploadError
                {
                    UploadData = "Customer gender",
                    Error = $"gender {uploadCase.Gender} invalid"
                });
            }
            if (!string.IsNullOrWhiteSpace(uploadCase.Education) && Enum.TryParse<Education>(uploadCase.Education, true, out var educationEnum))
            {
                uploadCase.Education = educationEnum.ToString();
            }
            else
            {
                uploadCase.Education = Education.UNKNOWN.ToString();
                errors.Add(new UploadError
                {
                    UploadData = "Customer education",
                    Error = $"education {uploadCase.Education} invalid"
                });
            }
            if (!string.IsNullOrWhiteSpace(uploadCase.Occupation) && Enum.TryParse<Occupation>(uploadCase.Occupation, true, out var occupationEnum))
            {
                uploadCase.Occupation = occupationEnum.ToString();
            }
            else
            {
                uploadCase.Occupation = Occupation.UNKNOWN.ToString();
                errors.Add(new UploadError
                {
                    UploadData = "Customer occupation",
                    Error = $"occupation {uploadCase.Occupation} invalid"
                });
            }

            if (!string.IsNullOrWhiteSpace(uploadCase.Income) && Enum.TryParse<Income>(uploadCase.Income, true, out var incomeEnum))
            {
                uploadCase.Income = incomeEnum.ToString();
            }
            else
            {
                uploadCase.Income = Income.UNKNOWN.ToString();
                errors.Add(new UploadError
                {
                    UploadData = "Customer income",
                    Error = $"income {uploadCase.Income} invalid"
                });
            }

            if (!string.IsNullOrWhiteSpace(uploadCase.CustomerDob) && DateTime.TryParseExact(uploadCase.CustomerDob, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var customerDob))
            {
                uploadCase.CustomerDob = customerDob.ToString("dd-MM-yyyy");
            }
            else
            {
                uploadCase.CustomerDob = DateTime.Now.AddYears(-20).ToString("dd-MM-yyyy");
                errors.Add(new UploadError
                {
                    UploadData = "Customer Date of Birth",
                    Error = $"Date of Birth {uploadCase.CustomerDob} invalid"
                });
            }

            if (!string.IsNullOrWhiteSpace(uploadCase.CustomerType) && Enum.TryParse(typeof(CustomerType), uploadCase.CustomerType, out var customerTypeEnum))
            {
                uploadCase.CustomerType = customerTypeEnum.ToString();
            }
            else
            {
                uploadCase.CustomerType = CustomerType.UNKNOWN.ToString();
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

            return (customerDetail, errors);
        }
        private async Task<(BeneficiaryDetail, List<UploadError>)> AddBeneficiary(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, byte[] data)
        {
            var errors = new List<UploadError>();

            if (string.IsNullOrWhiteSpace(uploadCase.BeneficiaryName))
            {
                errors.Add(new UploadError { UploadData = "Beneficiary name", Error = "null/empty" });
            }
            var pinCode = context.PinCode.Include(p => p.District)
                                                .Include(p => p.State)
                                                .Include(p => p.Country)
                                                .FirstOrDefault(p => p.Code == uploadCase.BeneficiaryPincode &&
                                                p.District.Name.ToLower().Contains(uploadCase.BeneficiaryDistrictName.ToLower()));
            if (pinCode is null || pinCode.CountryId != companyUser.ClientCompany.CountryId)
            {
                errors.Add(new UploadError
                {
                    UploadData = "Beneficiary pincode/district",
                    Error = $"pincode {uploadCase?.BeneficiaryPincode}/district {uploadCase?.BeneficiaryDistrictName} null/not found"
                });
            }

            if (string.IsNullOrWhiteSpace(uploadCase.Relation))
            {
                errors.Add(new UploadError
                {
                    UploadData = "Beneficiary relation",
                    Error = $"Relation {uploadCase.Relation} null/empty/invalid"
                });
            }

            var relation = string.IsNullOrWhiteSpace(uploadCase.Relation)
                ? context.BeneficiaryRelation.FirstOrDefault()  // Get first record from the table
                : context.BeneficiaryRelation.FirstOrDefault(b => b.Code.ToLower() == uploadCase.Relation.ToLower()) // Get matching record
                ?? context.BeneficiaryRelation.FirstOrDefault();

            var beneficiaryNewImage = GetImagesWithDataInSubfolder(data, uploadCase.CaseId.ToLower(), BENEFICIARY_IMAGE);
            if (beneficiaryNewImage == null)
            {
                errors.Add(new UploadError
                {
                    UploadData = "Beneficiary image",
                    Error = "null/empty"
                });
            }
            if (!string.IsNullOrWhiteSpace(uploadCase.BeneficiaryIncome) && Enum.TryParse<Income>(uploadCase.BeneficiaryIncome, true, out var incomeEnum))
            {
                uploadCase.BeneficiaryIncome = incomeEnum.ToString();
            }
            else
            {
                uploadCase.BeneficiaryIncome = Income.UNKNOWN.ToString();
                errors.Add(new UploadError
                {
                    UploadData = "Beneficiary income",
                    Error = $"income {uploadCase.BeneficiaryIncome} invalid"
                });
            }
            if (!string.IsNullOrWhiteSpace(uploadCase.BeneficiaryDob) && DateTime.TryParseExact(uploadCase.BeneficiaryDob, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var beneficiaryDob))
            {
                uploadCase.BeneficiaryDob = beneficiaryDob.ToString("dd-MM-yyyy");
            }
            else
            {
                uploadCase.BeneficiaryDob = DateTime.Now.AddYears(-20).ToString("dd-MM-yyyy");
                errors.Add(new UploadError
                {
                    UploadData = "Beneficiary Date of Birth",
                    Error = $"Invalid {uploadCase.BeneficiaryDob}"
                });
            }

            if (string.IsNullOrWhiteSpace(uploadCase.BeneficiaryAddressLine))
            {
                errors.Add(new UploadError
                {
                    UploadData = "Beneficiary addressline",
                    Error = "null/empty"
                });
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

            return (beneficairy, errors);
        }

        public static byte[] GetImagesWithDataInSubfolder(byte[] zipData, string subfolderName, string filename = "")
        {
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
                if (companyUser is null || uploadCase is null || model is null || !ValidateDataCase(uploadCase))
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
