using System.Globalization;

using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using static risk.control.system.AppConstant.CONSTANTS;

namespace risk.control.system.Services
{
    public interface ICustomerCreationService
    {
        Task<(CustomerDetail?, List<UploadError>, List<string>)> AddCustomer(ApplicationUser companyUser, UploadCase uploadCase, byte[] data);
    }
    internal class CustomerCreationService : ICustomerCreationService
    {
        private readonly ApplicationDbContext context;
        private readonly IFeatureManager featureManager;
        private readonly IFileStorageService fileStorageService;
        private readonly IPhoneService phoneService;
        private readonly ICustomApiClient customApiCLient;
        private readonly ICaseImageCreationService caseImageCreationService;
        private readonly ILogger<CustomerCreationService> logger;

        public CustomerCreationService(ApplicationDbContext context,
            IFeatureManager featureManager,
            IFileStorageService fileStorageService,
            IPhoneService phoneService,
            ICustomApiClient customApiCLient,
            ICaseImageCreationService caseImageCreationService,
            ILogger<CustomerCreationService> logger)
        {
            this.context = context;
            this.featureManager = featureManager;
            this.fileStorageService = fileStorageService;
            this.phoneService = phoneService;
            this.customApiCLient = customApiCLient;
            this.caseImageCreationService = caseImageCreationService;
            this.logger = logger;
        }

        public async Task<(CustomerDetail?, List<UploadError>, List<string>)> AddCustomer(ApplicationUser companyUser, UploadCase uploadCase, byte[] data)
        {
            var errors = new List<UploadError>();
            var errorCustomer = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(uploadCase.CaseId))
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"[{nameof(uploadCase.CaseId)}: ${EmptyNull}]",
                        Error = $"{CONSTANTS.EmptyNull}"
                    });
                    errorCustomer.Add($"[{nameof(uploadCase.CaseId)}= {EmptyNull} ]");
                }

                if (string.IsNullOrWhiteSpace(uploadCase.CustomerName) || uploadCase.CustomerName.Length <2)
                {
                    errors.Add(new UploadError { UploadData = $"[{nameof(uploadCase.CustomerName)}: {EmptyNull}]", Error =$" {EmptyNull}" });
                    errorCustomer.Add($"[{nameof(uploadCase.CustomerName)}= {EmptyNull} ]");
                }

                //if (!string.IsNullOrWhiteSpace(uploadCase.CustomerType) && Enum.TryParse(typeof(CustomerType), uploadCase.CustomerType, out var customerTypeEnum))
                //{
                //    uploadCase.CustomerType = customerTypeEnum.ToString();
                //}
                //else
                //{
                //    uploadCase.CustomerType = CustomerType.UNKNOWN.ToString();
                //}

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
                    errorCustomer.Add($"[Customer gender=`{uploadCase.Gender} {EmptyNull}]");
                }
                bool pinCodeValid = true;
                int pincode = uploadCase.CustomerPincode;

                // Define what constitutes a VALID pincode
                bool isValid4Digit = (pincode >= 1000 && pincode <= 9999);
                bool isValid6Digit = (pincode >= 100000 && pincode <= 999999);

                if (!isValid4Digit && !isValid6Digit)
                {
                    pinCodeValid = false;
                    errors.Add(new UploadError { UploadData = $"{nameof(uploadCase.CustomerPincode)}: {EmptyNull}]", Error = $"{ EmptyNull}" });
                    errorCustomer.Add($"[{nameof(uploadCase.CustomerPincode)}={EmptyNull}]");
                }

                if (string.IsNullOrWhiteSpace(uploadCase.CustomerDistrictName) &&  uploadCase.CustomerDistrictName.Length <= 2)
                {
                    errors.Add(new UploadError { UploadData = $"[{nameof(uploadCase.CustomerDistrictName)}: {EmptyNull}]", Error = $"{EmptyNull}" });
                    errorCustomer.Add($"[{nameof(uploadCase.CustomerDistrictName)}={EmptyNull}");
                }
                PinCode? pinCode = null;

                if (pinCodeValid)
                {
                    pinCode = await context.PinCode
                                           .Include(p => p.District)
                                           .Include(p => p.State)
                                           .Include(p => p.Country)
                                           .FirstOrDefaultAsync(p => p.Code == uploadCase.CustomerPincode &&
                                           p.District.Name.ToLower().Contains(uploadCase.CustomerDistrictName.ToLower()));
                    if (pinCode is null || pinCode.CountryId != companyUser.ClientCompany.CountryId)
                    {
                        errors.Add(new UploadError
                        {
                            UploadData = $"[Customer Pincode {uploadCase.CustomerPincode} And/Or District {uploadCase.CustomerDistrictName} not found]",
                            Error = $"Pincode {uploadCase.CustomerPincode} And/Or District {uploadCase.CustomerDistrictName} not found"
                        });
                        errorCustomer.Add($"[Customer Pincode=`{uploadCase.CustomerPincode}` And/Or District=`{uploadCase.CustomerDistrictName}` not found]");
                    }
                }
                if (await featureManager.IsEnabledAsync(FeatureFlags.VALIDATE_PHONE))
                {
                    var country = await context.Country.FirstOrDefaultAsync(c => c.CountryId == companyUser.ClientCompany.CountryId);
                    var isMobile = phoneService.IsValidMobileNumber(uploadCase.CustomerContact, country.ISDCode.ToString());
                    if (!isMobile)
                    {
                        errors.Add(new UploadError
                        {
                            UploadData = $"[Customer Mobile number {uploadCase.CustomerContact} Invalid]",
                            Error = $"[Mobile number {uploadCase.CustomerContact} Invalid]"
                        });
                        errorCustomer.Add($"[Customer Mobile number {uploadCase.CustomerContact}  {NullInvalid} ]");
                    }
                }

                if (string.IsNullOrWhiteSpace(uploadCase.CustomerAddressLine))
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"[{nameof(uploadCase.CustomerAddressLine)} : {EmptyNull}",
                        Error = $"{EmptyNull}"
                    });
                    errorCustomer.Add($"[{nameof(uploadCase.CustomerAddressLine)}={EmptyNull}]");
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
                    errorCustomer.Add($"[Customer education =`{uploadCase.Education} {NullInvalid}]");
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
                    errorCustomer.Add($"[Customer occupation=`{uploadCase.Occupation} {NullInvalid}]");
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
                    errorCustomer.Add($"[Customer income=`{uploadCase.Income}  {NullInvalid} ]");
                }

                bool isValidDate = DateTime.TryParseExact(uploadCase.CustomerDob, CONSTANTS.ValidDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var customerDob);

                // Logic: Check if date is invalid OR out of a reasonable age range (0-120 years)
                if (!isValidDate || customerDob > DateTime.Now || customerDob < DateTime.Now.AddYears(-120))
                {
                    var errorMsg = $"[Customer Date of Birth: Invalid {uploadCase.CustomerDob}]";

                    errors.Add(new UploadError
                    {
                        UploadData = errorMsg,
                        Error = $"Invalid {uploadCase.BeneficiaryDob}"
                    });
                    errorCustomer.Add($"[Customer Date of Birth=`{uploadCase.BeneficiaryDob}` invalid]");
                }
                else
                {
                    // Re-format to ensure consistency (e.g., if input was 1-1-1990, it becomes 01-01-1990)
                    uploadCase.CustomerDob = customerDob.ToString(CONSTANTS.ValidDateFormat);
                }

                var extension = Path.GetExtension(CUSTOMER_IMAGE).ToLower();
                string filePath = string.Empty;
                var imagesWithData = await caseImageCreationService.GetImagesWithDataInSubfolder(data, uploadCase.CaseId?.ToLower(), CUSTOMER_IMAGE);
                if (imagesWithData is null)
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"[Customer image : Image {CUSTOMER_IMAGE} null/not found]",
                        Error = $"Image {CUSTOMER_IMAGE} null/not found"
                    });
                    errorCustomer.Add($"[Customer Image=`{CUSTOMER_IMAGE}` null/not found]");
                }
                else
                {
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(imagesWithData, extension, "Case", uploadCase.CaseId);
                    filePath = relativePath;
                }
                var customerDetail = new CustomerDetail
                {
                    Name = uploadCase.CustomerName,
                    //CustomerType = (CustomerType)Enum.Parse(typeof(CustomerType), uploadCase.CustomerType),
                    Gender = (Gender)Enum.Parse(typeof(Gender), uploadCase.Gender),
                    DateOfBirth = DateTime.ParseExact(uploadCase.CustomerDob, CONSTANTS.ValidDateFormat, CultureInfo.InvariantCulture),
                    PhoneNumber = (uploadCase.CustomerContact),
                    Education = (Education)Enum.Parse(typeof(Education), uploadCase.Education),
                    Occupation = (Occupation)Enum.Parse(typeof(Occupation), uploadCase.Occupation),
                    Income = (Income)Enum.Parse(typeof(Income), uploadCase.Income),
                    Addressline = uploadCase?.CustomerAddressLine,
                    CountryId = pinCode?.CountryId,
                    PinCodeId = pinCode?.PinCodeId,
                    StateId = pinCode?.StateId,
                    DistrictId = pinCode?.DistrictId,
                    //Description = rowData[20]?.Trim(),
                    //ProfilePicture = imagesWithData,
                    ImagePath = filePath,
                    ProfilePictureExtension = extension,
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
                    string url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}",
                            customerLatLong, Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY"));
                    customerDetail.CustomerLocationMap = url;
                }

                return (customerDetail, errors, errorCustomer);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading benefificary detail");
                return (null, errors, errorCustomer);
            }
        }
    }
}
