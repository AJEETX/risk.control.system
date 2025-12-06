using System.Globalization;

using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using static risk.control.system.AppConstant.CONSTANTS;

namespace risk.control.system.Services
{
    public interface ICustomerCreationService
    {
        Task<(CustomerDetail, List<UploadError>, List<string>)> AddCustomer(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, byte[] data);
    }
    internal class CustomerCreationService : ICustomerCreationService
    {
        private readonly ApplicationDbContext context;
        private readonly IFeatureManager featureManager;
        private readonly IPhoneService phoneService;
        private readonly ICustomApiCLient customApiCLient;
        private readonly ICaseImageCreationService caseImageCreationService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ILogger<CustomerCreationService> logger;

        public CustomerCreationService(ApplicationDbContext context,
            IFeatureManager featureManager,
            IPhoneService phoneService,
            ICustomApiCLient customApiCLient,
            ICaseImageCreationService caseImageCreationService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<CustomerCreationService> logger)
        {
            this.context = context;
            this.featureManager = featureManager;
            this.phoneService = phoneService;
            this.customApiCLient = customApiCLient;
            this.caseImageCreationService = caseImageCreationService;
            this.webHostEnvironment = webHostEnvironment;
            this.logger = logger;
        }

        public async Task<(CustomerDetail, List<UploadError>, List<string>)> AddCustomer(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, byte[] data)
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
                    errorCustomer.Add($"[Customer gender=`{uploadCase.Gender}`null/ invalid ]");
                }

                if (string.IsNullOrWhiteSpace(uploadCase.CustomerPincode))
                {
                    errors.Add(new UploadError { UploadData = $"{nameof(uploadCase.CustomerPincode)}: null/empty]", Error = "null/empty" });
                    errorCustomer.Add($"[{nameof(uploadCase.CustomerPincode)}=null/empty]");
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
                        errorCustomer.Add($"[Customer Pincode=`{uploadCase.CustomerPincode}` And/Or District=`{uploadCase.CustomerDistrictName}` not found]");
                    }
                }
                if (await featureManager.IsEnabledAsync(FeatureFlags.VALIDATE_PHONE))
                {
                    var country = context.Country.FirstOrDefault(c => c.CountryId == companyUser.ClientCompany.CountryId);
                    var isMobile = phoneService.IsValidMobileNumber(uploadCase.CustomerContact, country.ISDCode.ToString());
                    if (!isMobile)
                    {
                        errors.Add(new UploadError
                        {
                            UploadData = $"[Customer Mobile number {uploadCase.CustomerContact} Invalid]",
                            Error = $"[Mobile number {uploadCase.CustomerContact} Invalid]"
                        });
                        errorCustomer.Add($"[Customer Mobile number {uploadCase.CustomerContact} Invalid]");
                    }
                }

                var extension = Path.GetExtension(CUSTOMER_IMAGE).ToLower();
                var fileName = Guid.NewGuid().ToString() + extension;
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
                    var imagePath = Path.Combine(webHostEnvironment.WebRootPath, "customer");
                    if (!Directory.Exists(imagePath))
                    {
                        Directory.CreateDirectory(imagePath);
                    }
                    var filePath = Path.Combine(webHostEnvironment.WebRootPath, "customer", fileName);
                    await File.WriteAllBytesAsync(filePath, imagesWithData);
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
                    errorCustomer.Add($"[Customer education =`{uploadCase.Education}`null/ invalid]");
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
                    errorCustomer.Add($"[Customer occupation=`{uploadCase.Occupation}` null/invalid]");
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
                    errorCustomer.Add($"[Customer income=`{uploadCase.Income}` null/invalid]");
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
                    errorCustomer.Add($"[Customer Date of Birth=`{uploadCase.CustomerDob}` null/invalid]");
                }

                string noImagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", CUSTOMER_IMAGE);

                var customerDetail = new CustomerDetail
                {
                    Name = uploadCase.CustomerName,
                    //CustomerType = (CustomerType)Enum.Parse(typeof(CustomerType), uploadCase.CustomerType),
                    Gender = (Gender)Enum.Parse(typeof(Gender), uploadCase.Gender),
                    DateOfBirth = DateTime.ParseExact(uploadCase.CustomerDob, "dd-MM-yyyy", CultureInfo.InvariantCulture),
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
                    ImagePath = "/customer/" + fileName,
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
    }
}
