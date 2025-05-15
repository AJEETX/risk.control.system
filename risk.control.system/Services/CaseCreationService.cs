using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using static risk.control.system.AppConstant.CONSTANTS;

using Google.Api;

using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ICaseCreationService
    {
        Task<InvestigationTask> FileUpload(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, FileOnFileSystemModel model);

    }
    public class CaseCreationService : ICaseCreationService
    {
        
        private readonly ApplicationDbContext context;
        private readonly ICustomApiCLient customApiCLient;
        private readonly ICloneReportService cloneService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly Regex regex = new Regex("\"(.*?)\"");
        private const string NO_DATA = "NO DATA";
        public CaseCreationService(ApplicationDbContext context, ICustomApiCLient customApiCLient, ICloneReportService cloneService, IWebHostEnvironment webHostEnvironment)
        {
            this.context = context;
            this.customApiCLient = customApiCLient;
            this.cloneService = cloneService;
            this.webHostEnvironment = webHostEnvironment;
        }

        private bool ValidateDataCase(UploadCase uploadCase)
        {
            if(string.IsNullOrWhiteSpace(uploadCase.CaseId) ||
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
        private async Task<InvestigationTask> AddCaseDetail(UploadCase uploadCase, ClientCompanyApplicationUser companyUser, FileOnFileSystemModel model)
        {

            var customerTask = AddCustomer(companyUser, uploadCase, model.ByteData);
            var beneficiaryTask = AddBeneficiary(companyUser, uploadCase, model.ByteData);
            await Task.WhenAll(customerTask, beneficiaryTask);

            // Get the results
            var customer = await customerTask;
            var beneficiary = await beneficiaryTask;
            InsuranceType caseType = InsuranceType.CLAIM;
            if (uploadCase.CaseType != "0")
            {
                caseType = InsuranceType.UNDERWRITING;
            }
            
            var servicetype = string.IsNullOrWhiteSpace(uploadCase.ServiceType)
                ? context.InvestigationServiceType.FirstOrDefault(i => i.InsuranceType == caseType)  // Case 1: ServiceType is null, get first record matching LineOfBusinessId
                : context.InvestigationServiceType
                    .FirstOrDefault(b => b.Code.ToLower() == uploadCase.ServiceType.ToLower() && b.InsuranceType == caseType)  // Case 2: Try matching Code + LineOfBusinessId
                  ?? context.InvestigationServiceType
                    .FirstOrDefault(b => b.InsuranceType == caseType);  // Case 3: If no match, retry ignoring LineOfBusinessId



            var savedNewImage = GetImagesWithDataInSubfolder(model.ByteData, uploadCase.CaseId.ToLower(), POLICY_IMAGE);

            if (!string.IsNullOrWhiteSpace(uploadCase.Amount) && decimal.TryParse(uploadCase.Amount, out var amount))
            {
                uploadCase.Amount = amount.ToString();
            }
            else
            {
                uploadCase.Amount = "0";
            }

            if (!string.IsNullOrWhiteSpace(uploadCase.IssueDate) && DateTime.TryParseExact(uploadCase.IssueDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var issueDate))
            {
                uploadCase.IssueDate = issueDate.ToString("dd-MM-yyyy");
            }
            else
            {
                uploadCase.IssueDate = DateTime.Now.ToString("dd-MM-yyyy");
            }
            if (!string.IsNullOrWhiteSpace(uploadCase.IncidentDate) && DateTime.TryParseExact(uploadCase.IncidentDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOfIncident))
            {
                uploadCase.IncidentDate = dateOfIncident.ToString("dd-MM-yyyy");
            }
            else
            {
                uploadCase.IncidentDate = DateTime.Now.ToString("dd-MM-yyyy");
            }

            var caseEnabler = string.IsNullOrWhiteSpace(uploadCase.Reason) ?
                context.CaseEnabler.FirstOrDefault() :
                context.CaseEnabler.FirstOrDefault(c => c.Code.ToLower() == uploadCase.Reason.Trim().ToLower())
                ?? context.CaseEnabler.FirstOrDefault();

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
            if (!policyDetail.IsValidCaseDetail())
            {
                return null;
            }
            if (customer is null || !policyDetail.IsValidCustomerForUpload(customer) || beneficiary is null || !policyDetail.IsValidBeneficiaryForUpload(beneficiary))
            {
                return null;
            }
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
            return claim;
        }
        private async Task<CustomerDetail> AddCustomer(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, byte[] data)
        {
            var pinCode = context.PinCode
                                    .Include(p => p.District)
                                    .Include(p => p.State)
                                    .Include(p => p.Country)
                                    .FirstOrDefault(p => p.Code == uploadCase.CustomerPincode);
            if (pinCode is null || pinCode.CountryId != companyUser.ClientCompany.CountryId)
            {
                return null;
            }
            var imagesWithData = GetImagesWithDataInSubfolder(data, uploadCase.CaseId.ToLower(), CUSTOMER_IMAGE);
            if (!string.IsNullOrWhiteSpace(uploadCase.Gender) && Enum.TryParse(typeof(Gender), uploadCase.Gender, out var gender)) 
            {
                uploadCase.Gender = gender.ToString();
            }
            else
            {
                uploadCase.Gender = Gender.UNKNOWN.ToString();
            }
            if(!string.IsNullOrWhiteSpace(uploadCase.Education)  && Enum.TryParse(typeof(Education), uploadCase.Education, out var educationEnum))
            {
                uploadCase.Education = educationEnum.ToString();
            }
            else
            {
                uploadCase.Education = Education.UNKNOWN.ToString();
            }
            if(!string.IsNullOrWhiteSpace(uploadCase.Occupation) && Enum.TryParse(typeof(Occupation), uploadCase.Occupation, out var occupationEnum))
            {
                uploadCase.Occupation = occupationEnum.ToString();
            }
            else
            {
                uploadCase.Occupation = Occupation.UNKNOWN.ToString();
            }

            if(!string.IsNullOrWhiteSpace(uploadCase.Income) && Enum.TryParse(typeof(Income), uploadCase.Income, out var incomeEnum))
            {
                uploadCase.Income = incomeEnum.ToString();
            }
            else
            {
                uploadCase.Income = Income.UNKNOWN.ToString();
            }

            if(!string.IsNullOrWhiteSpace(uploadCase.CustomerDob) && DateTime.TryParseExact(uploadCase.CustomerDob, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var customerDob))
            {
                uploadCase.CustomerDob = customerDob.ToString("dd-MM-yyyy");
            }
            else
            {
                uploadCase.CustomerDob = DateTime.Now.AddYears(-20).ToString("dd-MM-yyyy");
            }

            if(!string.IsNullOrWhiteSpace(uploadCase.CustomerType) && Enum.TryParse(typeof(CustomerType), uploadCase.CustomerType, out var customerTypeEnum))
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
                Addressline = uploadCase.CustomerAddressLine,
                CountryId = pinCode.CountryId,
                PinCodeId = pinCode.PinCodeId,
                StateId = pinCode.StateId,
                DistrictId = pinCode.DistrictId,
                //Description = rowData[20]?.Trim(),
                ProfilePicture = imagesWithData,
                ProfilePictureExtension = Path.GetExtension(CUSTOMER_IMAGE),
                UpdatedBy = companyUser.Email,
                Updated = DateTime.Now
            };
            var address = customerDetail.Addressline + ", " +
                pinCode.District.Name + ", " +
                pinCode.State.Name + ", " +
                pinCode.Country.Code + ", " +
                pinCode.Code;

            var coordinates = await customApiCLient.GetCoordinatesFromAddressAsync(address);
            customerDetail.Latitude = coordinates.Latitude;
            customerDetail.Longitude = coordinates.Longitude;
            var customerLatLong = coordinates.Latitude + "," + coordinates.Longitude;
            var url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}",
                    customerLatLong, Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY"));
            customerDetail.CustomerLocationMap = url;
            return customerDetail;
        }
        private async Task<BeneficiaryDetail> AddBeneficiary(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, byte[] data)
        {
            var pinCode = context.PinCode
                                                .Include(p => p.District)
                                                .Include(p => p.State)
                                                .Include(p => p.Country)
                                                .FirstOrDefault(p => p.Code == uploadCase.BeneficiaryPincode);
            if (pinCode is null || pinCode.CountryId != companyUser.ClientCompany.CountryId)
            {
                return null;
            }
            var relation = string.IsNullOrWhiteSpace(uploadCase.Relation)
                ? context.BeneficiaryRelation.FirstOrDefault()  // Get first record from the table
                : context.BeneficiaryRelation.FirstOrDefault(b => b.Code.ToLower() == uploadCase.Relation.ToLower())
                ?? context.BeneficiaryRelation.FirstOrDefault();  // Get matching record
            var beneficiaryNewImage = GetImagesWithDataInSubfolder(data, uploadCase.CaseId.ToLower(), BENEFICIARY_IMAGE);
            if (!string.IsNullOrWhiteSpace(uploadCase.BeneficiaryIncome) && Enum.TryParse(typeof(Income), uploadCase.BeneficiaryIncome, out var incomeEnum))
            {
                uploadCase.BeneficiaryIncome = incomeEnum.ToString();
            }
            else
            {
                uploadCase.BeneficiaryIncome = Income.UNKNOWN.ToString();
            }
            if (!string.IsNullOrWhiteSpace(uploadCase.BeneficiaryDob) && DateTime.TryParseExact(uploadCase.BeneficiaryDob, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var beneficiaryDob))
            {
                uploadCase.BeneficiaryDob = beneficiaryDob.ToString("dd-MM-yyyy");
            }
            else
            {
                uploadCase.BeneficiaryDob = DateTime.Now.AddYears(-20).ToString("dd-MM-yyyy");
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
                PinCodeId = pinCode.PinCodeId,
                DistrictId = pinCode.District.DistrictId,
                StateId = pinCode.State.StateId,
                CountryId = pinCode.Country.CountryId,
                ProfilePicture = beneficiaryNewImage,
                ProfilePictureExtension = Path.GetExtension(BENEFICIARY_IMAGE),
                Updated = DateTime.Now,
                UpdatedBy = companyUser.Email
            };
            var address = beneficairy.Addressline + ", " +
                pinCode.District.Name + ", " +
                pinCode.State.Name + ", " +
                pinCode.Country.Code + ", " +
                pinCode.Code;

            var coordinates = await customApiCLient.GetCoordinatesFromAddressAsync(address);

            var latLong = coordinates.Latitude + "," + coordinates.Longitude;
            beneficairy.Latitude = coordinates.Latitude;
            beneficairy.Longitude = coordinates.Longitude;
            var url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}",
                    latLong, Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY"));
            beneficairy.BeneficiaryLocationMap = url;
            return beneficairy;
        }

        public static  byte[] GetImagesWithDataInSubfolder(byte[] zipData, string subfolderName, string filename = "")
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

            var image =  images.FirstOrDefault(i=> i.FileName == filename);
            if(image.ImageData != null)
            {
                return image.ImageData;
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
        
        
        public async Task<InvestigationTask> FileUpload(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, FileOnFileSystemModel model)
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
