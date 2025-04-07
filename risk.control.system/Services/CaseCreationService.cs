using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ICaseCreationService
    {
        Task<bool> PerformUpload(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, FileOnFileSystemModel model);
    }
    public class CaseCreationService : ICaseCreationService
    {
        private const string UNDERWRITING = "underwriting";
        private const string POLICY_IMAGE = "policy.jpg";
        private const string CUSTOMER_IMAGE = "customer.jpg";
        private const string BENEFICIARY_IMAGE = "beneficiary.jpg";
        private const string CLAIMS = "claims";
        private readonly ApplicationDbContext _context;
        private readonly ICustomApiCLient customApiCLient;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly Regex regex = new Regex("\"(.*?)\"");
        private const string NO_DATA = "NO DATA";
        public CaseCreationService(ApplicationDbContext context, ICustomApiCLient customApiCLient, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            this.customApiCLient = customApiCLient;
            this.webHostEnvironment = webHostEnvironment;
        }

        public async Task<bool> PerformUpload(ClientCompanyApplicationUser companyUser, UploadCase uploadCase,  FileOnFileSystemModel model)
        {
            try
            {
                if(companyUser is null || uploadCase is null || model is null)
                {
                    return false;
                }
                if (!ValidateDataCase(uploadCase))
                {
                    return false;
                }
                var claimAdded = await AddCase(uploadCase, companyUser, model);
                if(!claimAdded)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return false;
            }
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
        private async Task<bool> AddCase(UploadCase uploadCase, ClientCompanyApplicationUser companyUser, FileOnFileSystemModel model)
        {
            string caseType = CLAIMS;
            if(uploadCase.CaseType != "0")
            {
                caseType = UNDERWRITING;
            }
            var lineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == caseType).LineOfBusinessId;
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var subStatus = companyUser.ClientCompany.AutoAllocation && model.AutoOrManual == CREATEDBY.AUTO ? createdStatus : assignedStatus;
            var claim = new ClaimsInvestigation
            {
                InvestigationCaseStatusId = status.InvestigationCaseStatusId,
                InvestigationCaseStatus = status,
                InvestigationCaseSubStatusId = subStatus.InvestigationCaseSubStatusId,
                InvestigationCaseSubStatus = subStatus,
                Updated = DateTime.Now,
                UpdatedBy = companyUser.Email,
                CurrentUserEmail = companyUser.Email,
                CurrentClaimOwner = companyUser.Email,
                Deleted = false,
                HasClientCompany = true,
                AssignedToAgency = false,
                IsReady2Assign = true,
                IsReviewCase = false,
                UserEmailActioned = companyUser.Email,
                UserEmailActionedTo = companyUser.Email,
                CREATEDBY = model.AutoOrManual,
                ORIGIN = model.FileOrFtp,
                ClientCompanyId = companyUser.ClientCompanyId,
                UserRoleActionedTo = $"{companyUser.ClientCompany.Email}",
                CreatorSla = companyUser.ClientCompany.CreatorSla
            };
            var servicetype = string.IsNullOrWhiteSpace(uploadCase.ServiceType)
                ? _context.InvestigationServiceType.FirstOrDefault(i => i.LineOfBusinessId == lineOfBusinessId)  // Case 1: ServiceType is null, get first record matching LineOfBusinessId
                : _context.InvestigationServiceType
                    .FirstOrDefault(b => b.Code.ToLower() == uploadCase.ServiceType.ToLower() && b.LineOfBusinessId == lineOfBusinessId)  // Case 2: Try matching Code + LineOfBusinessId
                  ?? _context.InvestigationServiceType
                    .FirstOrDefault(b => b.LineOfBusinessId == lineOfBusinessId);  // Case 3: If no match, retry ignoring LineOfBusinessId



            var savedNewImage = GetImagesWithDataInSubfolder(model.ByteData, uploadCase.CaseId.ToLower(),POLICY_IMAGE);

            if(!string.IsNullOrWhiteSpace(uploadCase.Amount) && decimal.TryParse(uploadCase.Amount, out var amount))
            {
                uploadCase.Amount = amount.ToString();
            }
            else
            {
                uploadCase.Amount = "0";
            }

            if(!string.IsNullOrWhiteSpace(uploadCase.IssueDate) && DateTime.TryParseExact(uploadCase.IssueDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var issueDate))
            {
                uploadCase.IssueDate = issueDate.ToString("dd-MM-yyyy");
            }
            else
            {
                uploadCase.IssueDate = DateTime.Now.ToString("dd-MM-yyyy");
            }
            if(!string.IsNullOrWhiteSpace(uploadCase.IncidentDate) && DateTime.TryParseExact(uploadCase.IncidentDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOfIncident))
            {
                uploadCase.IncidentDate = dateOfIncident.ToString("dd-MM-yyyy");
            }
            else
            {
                uploadCase.IncidentDate = DateTime.Now.ToString("dd-MM-yyyy");
            }

            var caseEnabler = string.IsNullOrWhiteSpace(uploadCase.Reason) ?
                _context.CaseEnabler.FirstOrDefault():
                _context.CaseEnabler.FirstOrDefault(c => c.Code.ToLower() == uploadCase.Reason.Trim().ToLower())
                ?? _context.CaseEnabler.FirstOrDefault();

            var department = string.IsNullOrWhiteSpace(uploadCase.Department) ?
               _context.CostCentre.FirstOrDefault() :
               _context.CostCentre.FirstOrDefault(c => c.Code.ToLower() == uploadCase.Department.Trim().ToLower())
               ?? _context.CostCentre.FirstOrDefault();
            
            string noImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", POLICY_IMAGE);

            claim.PolicyDetail = new PolicyDetail
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
                LineOfBusinessId = lineOfBusinessId,
                DocumentImage = savedNewImage ?? File.ReadAllBytes(noImagePath),
                Updated = DateTime.Now,
                UpdatedBy = companyUser.Email
            };
            var customerTask = AddCustomer(companyUser, uploadCase, model.ByteData);
            var beneficiaryTask = AddBeneficiary(companyUser, uploadCase, model.ByteData);
            await Task.WhenAll(customerTask, beneficiaryTask);

            // Get the results
            var customer = await customerTask;
            var beneficiary = await beneficiaryTask;
            if (customer is null || beneficiary is null)
            {
                return false;
            }
            customer.ClaimsInvestigationId = claim.ClaimsInvestigationId;
            beneficiary.ClaimsInvestigationId = claim.ClaimsInvestigationId;

            _context.CustomerDetail.Add(customer);
            _context.BeneficiaryDetail.Add(beneficiary);
            _context.ClaimsInvestigation.Add(claim);
            var log = new InvestigationTransaction
            {
                ClaimsInvestigationId = claim.ClaimsInvestigationId,
                UserEmailActioned = claim.UserEmailActioned,
                UserRoleActionedTo = claim.UserRoleActionedTo,
                CurrentClaimOwner = companyUser.Email,
                HopCount = 0,
                Time2Update = 0,
                InvestigationCaseStatusId = status.InvestigationCaseStatusId,
                InvestigationCaseSubStatusId = createdStatus.InvestigationCaseSubStatusId,
                UpdatedBy = companyUser.Email
            };

            _context.InvestigationTransaction.Add(log);
            return true;
        }
        private async Task<CustomerDetail> AddCustomer(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, byte[] data)
        {
            var pinCode = _context.PinCode
                                    .Include(p => p.District)
                                    .Include(p => p.State)
                                    .Include(p => p.Country)
                                    .FirstOrDefault(p => p.Code == uploadCase.CustomerPincode);
            if (pinCode.CountryId != companyUser.ClientCompany.CountryId)
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
            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={customerLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{customerLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            customerDetail.CustomerLocationMap = url;
            return customerDetail;
        }
        private async Task<BeneficiaryDetail> AddBeneficiary(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, byte[] data)
        {
            var pinCode = _context.PinCode
                                                .Include(p => p.District)
                                                .Include(p => p.State)
                                                .Include(p => p.Country)
                                                .FirstOrDefault(p => p.Code == uploadCase.BeneficiaryPincode);
            if (pinCode is null || pinCode.CountryId != companyUser.ClientCompany.CountryId)
            {
                return null;
            }
            var relation = string.IsNullOrWhiteSpace(uploadCase.Relation)
                ? _context.BeneficiaryRelation.FirstOrDefault()  // Get first record from the table
                : _context.BeneficiaryRelation.FirstOrDefault(b => b.Code.ToLower() == uploadCase.Relation.ToLower())
                ?? _context.BeneficiaryRelation.FirstOrDefault();  // Get matching record
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
                Updated = DateTime.Now,
                UpdatedBy = companyUser.Email
            };
            var address = beneficairy.Addressline + ", " +
                pinCode.District.Name + ", " +
                pinCode.State.Name + ", " +
                pinCode.Country.Code + ", " +
                pinCode.Code;

            var coordinates = await customApiCLient.GetCoordinatesFromAddressAsync(address);

            var beneLatLong = coordinates.Latitude + "," + coordinates.Longitude;
            beneficairy.Latitude = coordinates.Latitude;
            beneficairy.Longitude = coordinates.Longitude;

            var beneUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={beneLatLong}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:A%7C{beneLatLong}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            beneficairy.BeneficiaryLocationMap = beneUrl;
            _context.BeneficiaryDetail.Add(beneficairy);
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
    }
}
