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
        private readonly IVerifierProcessor verifierProcessor;
        private readonly ICustomerValidator customerValidator;
        private readonly IExtractorService customerExtractorService;
        private readonly ICustomApiClient customApiCLient;
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
            this.customApiCLient = customApiCLient;
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
                var pinCode = await customerExtractorService.GetPinCodeAsync(uploadCase.CustomerPincode, uploadCase.CustomerDistrictName, companyUser.ClientCompany.CountryId.Value);
                if (pinCode == null) verifierProcessor.AddLocationError(errors, summaries, uploadCase.CustomerPincode, uploadCase.CustomerDistrictName);

                // 3. IO & External Logic
                await verifierProcessor.ValidatePhone(companyUser, uploadCase.CustomerContact, errors, summaries);
                var (imagePath, extension) = await verifierProcessor.ProcessImage(uploadCase, data, errors, summaries, CUSTOMER_IMAGE,"Customer");

                // 4. Mapping
                var customer = new CustomerDetail
                {
                    Name = uploadCase.CustomerName,
                    Gender = gender,
                    DateOfBirth = dob,
                    PhoneNumber = uploadCase.CustomerContact,
                    Education = edu,
                    Occupation = occ,
                    Income = income,
                    Addressline = uploadCase.CustomerAddressLine,
                    CountryId = pinCode?.CountryId,
                    PinCodeId = pinCode?.PinCodeId,
                    StateId = pinCode?.StateId,
                    DistrictId = pinCode?.DistrictId,
                    ImagePath = imagePath,
                    ProfilePictureExtension = extension,
                    UpdatedBy = companyUser.Email,
                    Updated = DateTime.UtcNow
                };

                if (pinCode != null) await EnrichLocation(customer, pinCode);

                return (customer, errors, summaries);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading customer detail for case {CaseId}", uploadCase.CaseId);
                return (null, errors, summaries);
            }
        }

        private async Task EnrichLocation(CustomerDetail c, PinCode p)
        {
            var addr = $"{c.Addressline}, {p.District.Name}, {p.State.Name}, {p.Country.Code}, {p.Code}";
            var (lat, lon) = await customApiCLient.GetCoordinatesFromAddressAsync(addr);
            c.Latitude = lat; c.Longitude = lon;
            c.CustomerLocationMap = $"https://maps.googleapis.com/maps/api/staticmap?center={lat},{lon}&zoom=14&size=600x300&markers=color:red|{lat},{lon}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
        }
    }
}
