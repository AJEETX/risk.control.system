using System.Globalization;
using System.Net;
using risk.control.system.Helpers;
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

    internal class BeneficiaryCreationService(IVerifierProcessor verifierProcessor,
        IBeneficiaryValidator beneficiaryValidator,
        IExtractorService extractorService,
        ICustomApiClient customApiCLient,
        ILogger<BeneficiaryCreationService> logger) : IBeneficiaryCreationService
    {
        private readonly IVerifierProcessor _verifierProcessor = verifierProcessor;
        private readonly IBeneficiaryValidator _beneficiaryValidator = beneficiaryValidator;
        private readonly IExtractorService _extractorService = extractorService;
        private readonly ICustomApiClient _customApiClient = customApiCLient;
        private readonly ILogger<BeneficiaryCreationService> _logger = logger;

        public async Task<(BeneficiaryDetail?, List<UploadError>, List<string>)> AddBeneficiary(ApplicationUser companyUser, UploadCase uploadCase, byte[] data)
        {
            var errors = new List<UploadError>();
            var summaries = new List<string>();
            try
            {
                _beneficiaryValidator.ValidateRequiredFields(uploadCase, errors, summaries);
                var (dob, income) = _beneficiaryValidator.ValidateDetails(uploadCase, errors, summaries);
                if (errors.Count > 0) return (null, errors, summaries);
                var relationTask = _extractorService.GetRelationAsync(uploadCase.Relation!.Trim());
                var pinCodeTask = _extractorService.GetPinCodeAsync(uploadCase.BeneficiaryPincode, uploadCase.BeneficiaryDistrictName!.Trim(), companyUser.ClientCompany!.CountryId!.Value);
                var imageTask = _verifierProcessor.ProcessImage(uploadCase, data, errors, summaries, BENEFICIARY_IMAGE, "Beneficiary");
                var phoneTask = _verifierProcessor.ValidatePhone(companyUser, uploadCase.BeneficiaryContact!.Trim(), errors, summaries);
                await Task.WhenAll(relationTask, pinCodeTask, imageTask, phoneTask);
                var relation = await relationTask;
                var pinCode = await pinCodeTask;
                var (imagePath, ext) = await imageTask;
                if (relation == null)
                {
                    errors.Add(new UploadError { UploadData = "Relation", Error = $"Invalid beneficiary relation: '{uploadCase.Relation.Trim()}'" });
                    return (null, errors, summaries);
                }
                if (pinCode == null)
                {
                    _verifierProcessor.AddLocationError(errors, summaries, uploadCase.BeneficiaryPincode, uploadCase.BeneficiaryDistrictName.Trim());
                }
                var beneficiary = CreateBeneficiary(uploadCase, dob, relation, pinCode!, companyUser, income, imagePath, ext);
                if (pinCode != null) await EnrichLocationData(beneficiary, pinCode);
                return (beneficiary, errors, summaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddBeneficiary failed in Docker. CaseId={CaseId}, Relation='{Relation}'", uploadCase.CaseId!.Trim(), uploadCase.Relation);
                return (null, errors, summaries);
            }
        }

        private BeneficiaryDetail CreateBeneficiary(UploadCase uploadCase, DateTime dob, BeneficiaryRelation relation, PinCode pinCode, ApplicationUser companyUser, Income income, string imagePath, string ext)
        {
            return new BeneficiaryDetail
            {
                Name = WebUtility.HtmlEncode(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(uploadCase.BeneficiaryName!.ToLower())),
                BeneficiaryRelationId = relation.BeneficiaryRelationId,
                DateOfBirth = dob,
                Income = income,
                PhoneNumber = uploadCase.BeneficiaryContact!.Trim(),
                Addressline = uploadCase.BeneficiaryAddressLine!.Trim(),
                PinCodeId = pinCode?.PinCodeId,
                DistrictId = pinCode?.DistrictId,
                StateId = pinCode?.StateId,
                CountryId = pinCode?.CountryId,
                ImagePath = imagePath,
                ProfilePictureExtension = ext,
                Updated = DateTime.UtcNow,
                UpdatedBy = companyUser.Email
            };
        }
        private async Task EnrichLocationData(BeneficiaryDetail beneficiary, PinCode pin)
        {
            var fullAddress = $"{beneficiary.Addressline.Trim()}, {pin.District!.Name}, {pin.State!.Name}, {pin.Country!.Code}, {pin.Code}";
            var (lat, lon) = await _customApiClient.GetCoordinatesFromAddressAsync(fullAddress);
            beneficiary.Latitude = lat;
            beneficiary.Longitude = lon;
            var latLong = lat + "," + lon;
            var url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}",
                    latLong, EnvHelper.Get("GOOGLE_MAP_KEY"));
            beneficiary.BeneficiaryLocationMap = url;
        }
    }
}