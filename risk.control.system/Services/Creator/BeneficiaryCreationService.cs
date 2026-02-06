using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using static risk.control.system.AppConstant.CONSTANTS;

namespace risk.control.system.Services.Creator
{
    public interface IBeneficiaryCreationService
    {
        Task<(BeneficiaryDetail?, List<UploadError>, List<string>)> AddBeneficiary(ApplicationUser companyUser, UploadCase uploadCase, byte[] data);
    }

    internal class BeneficiaryCreationService : IBeneficiaryCreationService
    {
        private readonly IVerifierProcessor verifierProcessor;
        private readonly IBeneficiaryValidator beneficiaryValidator;
        private readonly IExtractorService extractorService;
        private readonly ICustomApiClient customApiCLient;
        private readonly ILogger<BeneficiaryCreationService> logger;

        public BeneficiaryCreationService(IVerifierProcessor verifierProcessor,
            IBeneficiaryValidator beneficiaryValidator,
            IExtractorService extractorService,
            ICustomApiClient customApiCLient,
            ILogger<BeneficiaryCreationService> logger)
        {
            this.verifierProcessor = verifierProcessor;
            this.beneficiaryValidator = beneficiaryValidator;
            this.extractorService = extractorService;
            this.customApiCLient = customApiCLient;
            this.logger = logger;
        }

        public async Task<(BeneficiaryDetail?, List<UploadError>, List<string>)> AddBeneficiary(ApplicationUser companyUser, UploadCase uploadCase, byte[] data)
        {
            var errors = new List<UploadError>();
            var summaries = new List<string>();

            try
            {
                // 1. Validation & Data Extraction
                beneficiaryValidator.ValidateRequiredFields(uploadCase, errors, summaries);
                var (dob, income) = beneficiaryValidator.ValidateDetails(uploadCase, errors, summaries);
                var relation = await extractorService.GetRelationAsync(uploadCase.Relation);
                var pinCode = await extractorService.GetPinCodeAsync(uploadCase.BeneficiaryPincode, uploadCase.BeneficiaryDistrictName, companyUser.ClientCompany.CountryId.Value);

                if (pinCode == null) verifierProcessor.AddLocationError(errors, summaries, uploadCase.BeneficiaryPincode, uploadCase.BeneficiaryDistrictName);

                // 2. Image & Phone processing
                var (imagePath, ext) = await verifierProcessor.ProcessImage(uploadCase, data, errors, summaries, BENEFICIARY_IMAGE, "Beneficiary");
                await verifierProcessor.ValidatePhone(companyUser, uploadCase.BeneficiaryContact, errors, summaries);

                // 3. Mapping
                var beneficiary = new BeneficiaryDetail
                {
                    Name = uploadCase.BeneficiaryName,
                    BeneficiaryRelationId = relation.BeneficiaryRelationId,
                    DateOfBirth = dob,
                    Income = income,
                    PhoneNumber = uploadCase.BeneficiaryContact,
                    Addressline = uploadCase.BeneficiaryAddressLine,
                    PinCodeId = pinCode?.PinCodeId,
                    DistrictId = pinCode?.DistrictId,
                    StateId = pinCode?.StateId,
                    CountryId = pinCode?.CountryId,
                    ImagePath = imagePath,
                    ProfilePictureExtension = ext,
                    Updated = DateTime.UtcNow,
                    UpdatedBy = companyUser.Email
                };

                if (pinCode != null) await EnrichLocationData(beneficiary, pinCode);

                return (beneficiary, errors, summaries);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in AddBeneficiary for CaseId: {CaseId}", uploadCase.CaseId);
                return (null, errors, summaries);
            }
        }

        private async Task EnrichLocationData(BeneficiaryDetail beneficiary, PinCode pin)
        {
            var fullAddress = $"{beneficiary.Addressline}, {pin.District.Name}, {pin.State.Name}, {pin.Country.Code}, {pin.Code}";
            var (lat, lon) = await customApiCLient.GetCoordinatesFromAddressAsync(fullAddress);

            beneficiary.Latitude = lat;
            beneficiary.Longitude = lon;
            beneficiary.BeneficiaryLocationMap = $"https://maps.googleapis.com/maps/api/staticmap?center={lat},{lon}&zoom=14&size=600x300&markers=color:red|{lat},{lon}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
        }
    }
}