using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Creator
{
    public interface IVerifierProcessor
    {
        Task<(string Path, string Extension)> ProcessImage(UploadCase uc, byte[] zipData, List<UploadError> errs, List<string> sums, string imageName, string caseEntityName);

        Task ValidatePhone(ApplicationUser user, string contactNumber, List<UploadError> errs, List<string> sums);

        void AddLocationError(List<UploadError> errs, List<string> sums, int pinCode, string districtName);
    }

    internal class VerifierProcessor : IVerifierProcessor
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ICaseImageCreationService caseImageCreationService;
        private readonly IPhoneService phoneService;
        private readonly IFileStorageService fileStorageService;
        private readonly IFeatureManager featureManager;

        public VerifierProcessor(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ICaseImageCreationService caseImageCreationService,
            IPhoneService phoneService,
            IFileStorageService fileStorageService,
            IFeatureManager featureManager
            )
        {
            _contextFactory = contextFactory;
            this.caseImageCreationService = caseImageCreationService;
            this.phoneService = phoneService;
            this.fileStorageService = fileStorageService;
            this.featureManager = featureManager;
        }

        public void AddLocationError(List<UploadError> errs, List<string> sums, int pinCode, string districtName)
        {
            errs.Add(new UploadError
            {
                UploadData = $"[Pincode: {pinCode} / District: {districtName}]",
                Error = "Location match not found or invalid for country"
            });
            sums.Add($"[Location={pinCode}/{districtName} not found]");
        }

        public async Task<(string Path, string Extension)> ProcessImage(UploadCase uc, byte[] zipData, List<UploadError> errs, List<string> sums, string imageName, string caseEntityName)
        {
            var extension = Path.GetExtension(imageName).ToLower();
            var imageData = await caseImageCreationService.GetImagesWithDataInSubfolder(zipData, uc.CaseId?.ToLower(), imageName);

            if (imageData == null)
            {
                errs.Add(new UploadError { UploadData = $"{caseEntityName} Image", Error = $"Missing {caseEntityName} Image" });
                sums.Add($"[{caseEntityName} image is missing]");
                return (string.Empty, extension);
            }

            // Returns a tuple: (string FileName, string RelativePath)
            var result = await fileStorageService.SaveAsync(imageData, extension, "Case", uc.CaseId);
            return (result.Item2, extension);
        }

        public async Task ValidatePhone(ApplicationUser user, string contactNumber, List<UploadError> errs, List<string> sums)
        {
            if (!await featureManager.IsEnabledAsync(FeatureFlags.VALIDATE_PHONE)) return;

            using var context = await _contextFactory.CreateDbContextAsync();
            var country = await context.Country.FirstOrDefaultAsync(c => c.CountryId == user.ClientCompany.CountryId);

            if (country == null || !phoneService.IsValidMobileNumber(contactNumber, country.ISDCode.ToString()))
            {
                errs.Add(new UploadError { UploadData = $"[Mobile: {contactNumber}]", Error = "Invalid mobile format" });
                sums.Add($"[Mobile={contactNumber} is invalid]");
            }
        }
    }
}