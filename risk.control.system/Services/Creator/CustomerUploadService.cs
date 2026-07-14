using System.Globalization;
using System.Net;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using static risk.control.system.AppConstant.CONSTANTS;

namespace risk.control.system.Services.Creator
{
    public interface ICustomerUploadService
    {
        Task<(CustomerDetail?, List<UploadError>, List<string>)> AddCustomer(ApplicationUser companyUser, UploadCase uploadCase, byte[] data);
    }

    internal class CustomerUploadService(IVerifierProcessor verifierProcessor,
        ICustomerValidator customerValidator,
        IExtractorService customerExtractorService,
        ICustomApiClient customApiCLient,
        ILogger<CustomerUploadService> logger) : ICustomerUploadService
    {
        private readonly IVerifierProcessor _verifierProcessor = verifierProcessor;
        private readonly ICustomerValidator _customerValidator = customerValidator;
        private readonly IExtractorService _customerExtractorService = customerExtractorService;
        private readonly ICustomApiClient _customApiClient = customApiCLient;
        private readonly ILogger<CustomerUploadService> _logger = logger;

        public async Task<(CustomerDetail?, List<UploadError>, List<string>)> AddCustomer(ApplicationUser companyUser, UploadCase uploadCase, byte[] data)
        {
            var errors = new List<UploadError>();
            var summaries = new List<string>();
            try
            {
                // 1. Validation
                _customerValidator.ValidateRequiredFields(uploadCase, errors, summaries);
                var (dob, gender, edu, occ, income) = _customerValidator.ValidateDetails(uploadCase, errors, summaries);

                // 2. Data Lookups
                var pinCodeTask = _customerExtractorService.GetPinCodeAsync(uploadCase.CustomerPincode, uploadCase.CustomerDistrictName!.Trim(), companyUser.ClientCompany!.CountryId!.Value);

                // 3. IO & External Logic
                var phoneTask = _verifierProcessor.ValidatePhone(companyUser, uploadCase.CustomerContact!.Trim(), errors, summaries);
                var imageTask = _verifierProcessor.ProcessFaceImage(uploadCase, data, errors, summaries, CUSTOMER_IMAGE, "Life-Assured");

                await Task.WhenAll(pinCodeTask, imageTask, phoneTask, phoneTask);
                var pinCode = await pinCodeTask;
                var (imagePath, extension) = await imageTask;
                var phoneValid = await phoneTask;

                if (pinCode == null) _verifierProcessor.AddLocationError(errors, summaries, uploadCase.CustomerPincode, uploadCase.CustomerDistrictName.Trim());

                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                // 4. Mapping
                var customer = new CustomerDetail
                {
                    Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(uploadCase.CustomerName!.ToLower())),
                    Gender = gender,
                    DateOfBirth = dob,
                    PhoneNumber = uploadCase.CustomerContact.Trim(),
                    IsValidPhoneNumber = phoneValid,
                    Education = edu,
                    Occupation = occ,
                    Income = income,
                    Addressline = uploadCase.CustomerAddressLine!.Trim(),
                    CountryId = pinCode?.CountryId,
                    PinCodeId = pinCode?.PinCodeId,
                    StateId = pinCode?.StateId,
                    DistrictId = pinCode?.DistrictId,
                    ImagePath = imagePath,
                    ProfilePictureExtension = extension,
                    UpdatedBy = companyUser.Email,
                    Updated = System.DateTime.UtcNow
                };

                if (pinCode != null) await EnrichLocation(customer, pinCode);

                return (customer, errors, summaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading customer detail for case {CaseId}", uploadCase.CaseId!.Trim());
                return (null, errors, summaries);
            }
        }

        private async Task EnrichLocation(CustomerDetail c, PinCode p)
        {
            var addr = $"{c.Addressline.Trim()}, {p.District!.Name}, {p.State!.Name}, {p.Country!.Code}, {p.Code}";
            var (lat, lon) = await _customApiClient.GetCoordinatesFromAddressAsync(addr);
            c.Latitude = lat; c.Longitude = lon;
            var latLong = lat + "," + lon;

            var url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}",
                    latLong, EnvHelper.Get("GOOGLE_MAP_KEY"));
            c.CustomerLocationMap = url;
        }
    }
}