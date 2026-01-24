using System.Globalization;

using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using static risk.control.system.AppConstant.CONSTANTS;

namespace risk.control.system.Services
{
    public interface IBeneficiaryCreationService
    {
        Task<(BeneficiaryDetail?, List<UploadError>, List<string>)> AddBeneficiary(ApplicationUser companyUser, UploadCase uploadCase, byte[] data);
    }
    internal class BeneficiaryCreationService : IBeneficiaryCreationService
    {

        private readonly ApplicationDbContext context;
        private readonly ICustomApiClient customApiCLient;
        private readonly IFeatureManager featureManager;
        private readonly IFileStorageService fileStorageService;
        private readonly IPhoneService phoneService;
        private readonly ICaseImageCreationService caseImageCreationService;
        private readonly ILogger<BeneficiaryCreationService> logger;

        public BeneficiaryCreationService(ApplicationDbContext context, ICustomApiClient customApiCLient,
            IFeatureManager featureManager,
            IFileStorageService fileStorageService,
            IPhoneService phoneService,
            ICaseImageCreationService caseImageCreationService,
            ILogger<BeneficiaryCreationService> logger)
        {
            this.context = context;
            this.customApiCLient = customApiCLient;
            this.featureManager = featureManager;
            this.fileStorageService = fileStorageService;
            this.phoneService = phoneService;
            this.caseImageCreationService = caseImageCreationService;
            this.logger = logger;
        }

        public async Task<(BeneficiaryDetail?, List<UploadError>, List<string>)> AddBeneficiary(ApplicationUser companyUser, UploadCase uploadCase, byte[] data)
        {
            var errors = new List<UploadError>();
            var errorBeneficiary = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(uploadCase.BeneficiaryName) && (uploadCase.BeneficiaryName.Length < 2))
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"{nameof(uploadCase.BeneficiaryName)} : ${CONSTANTS.EmptyNull}]",
                        Error = $"{CONSTANTS.EmptyNull}"
                    });
                    errorBeneficiary.Add($"{nameof(uploadCase.BeneficiaryName)}={EmptyNull}]");
                }

                if (string.IsNullOrWhiteSpace(uploadCase.Relation) || uploadCase.Relation.Length <= 2)
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"[Beneficiary relation : {uploadCase.Relation} null/empty/invalid]",
                        Error = $"Relation {uploadCase.Relation} null/empty/invalid"
                    });
                    errorBeneficiary.Add($"[Beneficiary relation=`{uploadCase.Relation}` null/empty]");
                }
                bool pinCodeValid = true;
                int pincode = uploadCase.BeneficiaryPincode;

                // Define what constitutes a VALID pincode
                bool isValid4Digit = (pincode >= 1000 && pincode <= 9999);
                bool isValid6Digit = (pincode >= 100000 && pincode <= 999999);

                if (!isValid4Digit && !isValid6Digit)
                {
                    pinCodeValid = false;
                    errors.Add(new UploadError { UploadData = $"[Beneficiary pincode: {EmptyNull}]", Error = CONSTANTS.EmptyNull });
                    errorBeneficiary.Add($"[Beneficiary pincode=${CONSTANTS.EmptyNull}]");
                }

                if (string.IsNullOrWhiteSpace(uploadCase.BeneficiaryDistrictName) || uploadCase.BeneficiaryDistrictName.Length <= 2)
                {
                    errors.Add(new UploadError { UploadData = $"[Beneficiary District Name : {EmptyNull}]", Error = $"{EmptyNull}" });
                    errorBeneficiary.Add($"[Beneficiary District Name={EmptyNull}]");
                }
                PinCode? pinCode = null;
                if (pinCodeValid)
                {
                    pinCode = await context.PinCode.Include(p => p.District)
                                                    .Include(p => p.State)
                                                    .Include(p => p.Country)
                                                    .FirstOrDefaultAsync(p => p.Code == uploadCase.BeneficiaryPincode &&
                                                    p.District.Name.ToLower().Contains(uploadCase.BeneficiaryDistrictName.ToLower()));
                    if (pinCode is null || pinCode.CountryId != companyUser.ClientCompany.CountryId)
                    {
                        errors.Add(new UploadError
                        {
                            UploadData = $"[Beneficiary Pincode: {uploadCase?.BeneficiaryPincode}And / Or District : {uploadCase?.BeneficiaryDistrictName} not found]",
                            Error = $"pincode {uploadCase?.BeneficiaryPincode}/district {uploadCase?.BeneficiaryDistrictName} not found"
                        });
                        errorBeneficiary.Add($"[Beneficiary Pincode=`{uploadCase?.BeneficiaryPincode}` And / Or District=`{uploadCase?.BeneficiaryDistrictName}` not found]");
                    }
                }

                if (await featureManager.IsEnabledAsync(FeatureFlags.VALIDATE_PHONE))
                {
                    var country = await context.Country.FirstOrDefaultAsync(c => c.CountryId == companyUser.ClientCompany.CountryId);
                    if (country is null)
                    {
                        errors.Add(new UploadError
                        {
                            UploadData = $"[Beneficiary Country: {NullInvalid}]",
                            Error = $"{NullInvalid}"
                        });
                        errorBeneficiary.Add($"[Beneficiary Country={NullInvalid}]");
                        errors.Add(new UploadError
                        {
                            UploadData = $"[Beneficiary Mobile number {uploadCase.BeneficiaryContact} Invalid]",
                            Error = $"[Mobile number {uploadCase.BeneficiaryContact} Invalid]"
                        });
                        errorBeneficiary.Add($"[Beneficiary Mobile number {uploadCase.BeneficiaryContact} Invalid]");
                    }
                    else
                    {
                        var isMobile = phoneService.IsValidMobileNumber(uploadCase.BeneficiaryContact, country.ISDCode.ToString());
                        if (!isMobile)
                        {
                            errors.Add(new UploadError
                            {
                                UploadData = $"[Beneficiary Mobile number {uploadCase.BeneficiaryContact} Invalid]",
                                Error = $"[Mobile number {uploadCase.BeneficiaryContact} Invalid]"
                            });
                            errorBeneficiary.Add($"[Beneficiary Mobile number {uploadCase.BeneficiaryContact} Invalid]");
                        }
                    }
                }
                var relation = string.IsNullOrWhiteSpace(uploadCase.Relation)
                    ? await context.BeneficiaryRelation.FirstOrDefaultAsync()  // Get first record from the table
                    : await context.BeneficiaryRelation.FirstOrDefaultAsync(b => b.Code.ToLower() == uploadCase.Relation.ToLower())
                    ?? await context.BeneficiaryRelation.FirstOrDefaultAsync();

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
                    errorBeneficiary.Add($"[Beneficiary income=`{uploadCase.BeneficiaryIncome}`{NullInvalid}]");
                }
                bool isValidDate = DateTime.TryParseExact(uploadCase.BeneficiaryDob, CONSTANTS.ValidDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var beneficiaryDob);

                // Logic: Check if date is invalid OR out of a reasonable age range (0-120 years)
                if (!isValidDate || beneficiaryDob > DateTime.Now || beneficiaryDob < DateTime.Now.AddYears(-120))
                {
                    var errorMsg = $"[Beneficiary Date of Birth: Invalid {uploadCase.BeneficiaryDob}]";

                    errors.Add(new UploadError
                    {
                        UploadData = errorMsg,
                        Error = $"Invalid {uploadCase.BeneficiaryDob}"
                    });
                    errorBeneficiary.Add($"[Beneficiary Date of Birth=`{uploadCase.BeneficiaryDob}` invalid]");
                }
                else
                {
                    // Re-format to ensure consistency (e.g., if input was 1-1-1990, it becomes 01-01-1990)
                    uploadCase.BeneficiaryDob = beneficiaryDob.ToString(CONSTANTS.ValidDateFormat);
                }

                if (string.IsNullOrWhiteSpace(uploadCase.BeneficiaryAddressLine) || uploadCase.BeneficiaryAddressLine.Length < 3)
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"[Beneficiary addressline : {EmptyNull}]",
                        Error = "null/empty"
                    });
                    errorBeneficiary.Add($"[Beneficiary addressline={EmptyNull}]");
                }
                var extension = Path.GetExtension(BENEFICIARY_IMAGE).ToLower();
                string filePath = string.Empty;
                var beneficiaryNewImage = await caseImageCreationService.GetImagesWithDataInSubfolder(data, uploadCase.CaseId?.ToLower(), BENEFICIARY_IMAGE);
                if (beneficiaryNewImage == null)
                {
                    errors.Add(new UploadError
                    {
                        UploadData = $"[Beneficiary image : {EmptyNull}]",
                        Error = "null/empty"
                    });
                    errorBeneficiary.Add($"[Beneficiary image=`{BENEFICIARY_IMAGE} {EmptyNull}]");
                }
                else
                {
                    var (fileName, relativePath) = await fileStorageService.SaveAsync(beneficiaryNewImage, extension, "Case", uploadCase.CaseId);
                    filePath = relativePath;
                }

                var beneficairy = new BeneficiaryDetail
                {
                    Name = uploadCase.BeneficiaryName,
                    BeneficiaryRelationId = relation.BeneficiaryRelationId,
                    DateOfBirth = DateTime.ParseExact(uploadCase.BeneficiaryDob, "dd-MM-yyyy", CultureInfo.InvariantCulture),
                    Income = (Income)Enum.Parse(typeof(Income), uploadCase.BeneficiaryIncome),
                    PhoneNumber = uploadCase.BeneficiaryContact,
                    Addressline = uploadCase.BeneficiaryAddressLine,
                    PinCodeId = pinCode?.PinCodeId,
                    DistrictId = pinCode?.DistrictId,
                    StateId = pinCode?.StateId,
                    CountryId = pinCode?.CountryId,
                    ImagePath = filePath,
                    ProfilePictureExtension = extension,
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
                    string url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}",
                            latLong, Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY"));
                    beneficairy.BeneficiaryLocationMap = url;
                }

                return (beneficairy, errors, errorBeneficiary);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Weeoe creating beneficiarly upload");
            }
            return (null, errors, errorBeneficiary);
        }
    }
}
