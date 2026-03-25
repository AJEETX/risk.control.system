using System.Globalization;
using System.Net;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using static risk.control.system.AppConstant.CONSTANTS;

namespace risk.control.system.Services.Creator
{
    public interface ICustomerCreationService
    {
        Task<(CustomerDetail?, List<UploadError>, List<string>)> AddCustomer(ApplicationUser companyUser, UploadCase uploadCase, byte[] data);
    }

    internal class CustomerCreationService : ICustomerCreationService
    {
        private readonly IVerifierProcessor verifierProcessor;
        private readonly ICustomerValidator customerValidator;
        private readonly IExtractorService customerExtractorService;
        private readonly ICustomApiClient customApiClient;
        private readonly ILogger<CustomerCreationService> logger;

        public CustomerCreationService(IVerifierProcessor verifierProcessor,
            ICustomerValidator customerValidator,
            IExtractorService customerExtractorService,
            ICustomApiClient customApiCLient,
            ILogger<CustomerCreationService> logger)
        {
            this.verifierProcessor = verifierProcessor;
            this.customerValidator = customerValidator;
            this.customerExtractorService = customerExtractorService;
            this.customApiClient = customApiCLient;
            this.logger = logger;
        }

        public async Task<(CustomerDetail?, List<UploadError>, List<string>)> AddCustomer(ApplicationUser companyUser, UploadCase uploadCase, byte[] data)
        {
            var errors = new List<UploadError>();
            var summaries = new List<string>();
            try
            {
                // 1. Validation
                customerValidator.ValidateRequiredFields(uploadCase, errors, summaries);
                var (dob, gender, edu, occ, income) = customerValidator.ValidateDetails(uploadCase, errors, summaries);

                // 2. Data Lookups
                var pinCodeTask = customerExtractorService.GetPinCodeAsync(uploadCase.CustomerPincode, uploadCase.CustomerDistrictName!.Trim(), companyUser.ClientCompany!.CountryId!.Value);

                // 3. IO & External Logic
                var phoneTask = verifierProcessor.ValidatePhone(companyUser, uploadCase.CustomerContact!.Trim(), errors, summaries);
                var imageTask = verifierProcessor.ProcessImage(uploadCase, data, errors, summaries, CUSTOMER_IMAGE, "Customer");

                await Task.WhenAll(pinCodeTask, imageTask, phoneTask);
                var pinCode = await pinCodeTask;
                var (imagePath, extension) = await imageTask;
                if (pinCode == null) verifierProcessor.AddLocationError(errors, summaries, uploadCase.CustomerPincode, uploadCase.CustomerDistrictName.Trim());

                var textInfo = CultureInfo.CurrentCulture.TextInfo;
                // 4. Mapping
                var customer = new CustomerDetail
                {
                    Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(uploadCase.CustomerName!.ToLower())),
                    Gender = gender,
                    DateOfBirth = dob,
                    PhoneNumber = uploadCase.CustomerContact.Trim(),
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
                logger.LogError(ex, "Error uploading customer detail for case {CaseId}", uploadCase.CaseId!.Trim());
                return (null, errors, summaries);
            }
        }

        private async Task EnrichLocation(CustomerDetail c, PinCode p)
        {
            var addr = $"{c.Addressline.Trim()}, {p.District!.Name}, {p.State!.Name}, {p.Country!.Code}, {p.Code}";
            var (lat, lon) = await customApiClient.GetCoordinatesFromAddressAsync(addr);
            c.Latitude = lat; c.Longitude = lon;
            var latLong = lat + "," + lon;

            var url = string.Format("https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=14&size={{0}}x{{1}}&maptype=roadmap&markers=color:red%7Clabel:A%7C{0}&key={1}",
                    latLong, EnvHelper.Get("GOOGLE_MAP_KEY"));
            c.CustomerLocationMap = url;
        }
    }
}