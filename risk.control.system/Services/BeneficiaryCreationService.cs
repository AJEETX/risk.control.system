using System.Globalization;

using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using static risk.control.system.AppConstant.CONSTANTS;

namespace risk.control.system.Services
{
    public interface IBeneficiaryCreationService
    {
        Task<(BeneficiaryDetail, List<UploadError>, List<string>)> AddBeneficiary(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, byte[] data);
    }
    public class BeneficiaryCreationService : IBeneficiaryCreationService
    {

        private readonly ApplicationDbContext context;
        private readonly ICustomApiCLient customApiCLient;
        private readonly ICaseImageCreationService caseImageCreationService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ILogger<BeneficiaryCreationService> logger;

        public BeneficiaryCreationService(ApplicationDbContext context, ICustomApiCLient customApiCLient,
            ICaseImageCreationService caseImageCreationService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<BeneficiaryCreationService> logger)
        {
            this.context = context;
            this.customApiCLient = customApiCLient;
            this.caseImageCreationService = caseImageCreationService;
            this.webHostEnvironment = webHostEnvironment;
            this.logger = logger;
        }

        public async Task<(BeneficiaryDetail, List<UploadError>, List<string>)> AddBeneficiary(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, byte[] data)
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
                    errorBeneficiary.Add($"[Beneficiary relation=`{uploadCase.Relation}` null/empty]");
                }

                if (string.IsNullOrWhiteSpace(uploadCase.BeneficiaryPincode))
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
                        errorBeneficiary.Add($"[Beneficiary Pincode=`{uploadCase?.BeneficiaryPincode}` And / Or District=`{uploadCase?.BeneficiaryDistrictName}` not found]");
                    }
                }

                var relation = string.IsNullOrWhiteSpace(uploadCase.Relation)
                    ? context.BeneficiaryRelation.FirstOrDefault()  // Get first record from the table
                    : context.BeneficiaryRelation.FirstOrDefault(b => b.Code.ToLower() == uploadCase.Relation.ToLower()) // Get matching record
                    ?? context.BeneficiaryRelation.FirstOrDefault();

                var extension = Path.GetExtension(BENEFICIARY_IMAGE).ToLower();
                var fileName = Guid.NewGuid().ToString() + extension;
                var beneficiaryNewImage = await caseImageCreationService.GetImagesWithDataInSubfolder(data, uploadCase.CaseId?.ToLower(), BENEFICIARY_IMAGE);
                if (beneficiaryNewImage == null)
                {
                    errors.Add(new UploadError
                    {
                        UploadData = "[Beneficiary image : null/empty]",
                        Error = "null/empty"
                    });
                    errorBeneficiary.Add($"[Beneficiary image=`{BENEFICIARY_IMAGE}` null/not found]");
                }
                else
                {
                    var imagePath = Path.Combine(webHostEnvironment.WebRootPath, "beneficiary");
                    if (!Directory.Exists(imagePath))
                    {
                        Directory.CreateDirectory(imagePath);
                    }
                    var filePath = Path.Combine(webHostEnvironment.WebRootPath, "beneficiary", fileName);
                    await File.WriteAllBytesAsync(filePath, beneficiaryNewImage);
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
                    errorBeneficiary.Add($"[Beneficiary income=`{uploadCase.BeneficiaryIncome}`null/ invalid]");
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
                    errorBeneficiary.Add($"[Beneficiary Date of Birth=`{uploadCase.BeneficiaryDob}` invalid]");
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
                    //ProfilePicture = beneficiaryNewImage,
                    ImagePath = "/beneficiary/" + fileName,
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
    }
}
