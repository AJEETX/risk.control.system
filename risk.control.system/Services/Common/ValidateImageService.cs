using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Common
{
    public interface IValidateImageService
    {
        void ValidateImage(IFormFile? image, Dictionary<string, string> errors);
        Task ValidateFaceImage(IFormFile? image, Dictionary<string, string> errors);
        Task<bool> ValidateProfileImage(IFormFile file, ServiceResult result);
    }

    public class ValidateImageService(IUserFaceImageCheckService userFaceCheckService) : IValidateImageService
    {
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedExt = new() { ".jpg", ".jpeg", ".png" };
        private static readonly HashSet<string> AllowedMime = new() { "image/jpeg", "image/png" };
        private readonly IUserFaceImageCheckService _userFaceCheckService = userFaceCheckService;

        public void ValidateImage(IFormFile? image, Dictionary<string, string> errors)
        {
            if (image == null || image.Length == 0)
            {
                errors["Document"] = "No Document image Found";
                return;
            }

            if (image.Length > MAX_FILE_SIZE)
                errors[image.Name] = "Image size exceeds 5MB";

            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedExt.Contains(ext))
                errors[image.Name] = "Invalid image type";

            if (!AllowedMime.Contains(image.ContentType))
                errors[image.Name] = "Invalid image content type";

            if (!ImageSignatureValidator.HasValidSignature(image))
                errors[image.Name] = "Invalid or corrupted image";
        }
        public async Task ValidateFaceImage(IFormFile? image, Dictionary<string, string> errors)
        {
            if (image == null || image.Length == 0)
            {
                errors["ProfileImage"] = "No image Found";
                return;
            }

            if (image.Length > MAX_FILE_SIZE)
                errors[image.Name] = "Image size exceeds 5MB";

            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedExt.Contains(ext))
                errors[image.Name] = "Invalid image type";

            if (!AllowedMime.Contains(image.ContentType))
                errors[image.Name] = "Invalid image content type";

            if (!ImageSignatureValidator.HasValidSignature(image))
                errors[image.Name] = "Invalid or corrupted image";
            var faceCheckResult = await _userFaceCheckService.HasExactlyOneFace(image);
            if (!faceCheckResult.IsValid)
            {
                errors[image.Name] = faceCheckResult.Message!;
            }
        }
        public async Task<bool> ValidateProfileImage(IFormFile file, ServiceResult result)
        {
            if (file == null || file.Length == 0)
            {
                result.Errors[nameof(ApplicationUser.ProfileImage)] = "Profile image is required.";
                return false;
            }

            if (file.Length > MAX_FILE_SIZE)
            {
                result.Errors[nameof(ApplicationUser.ProfileImage)] = "Profile image exceeds 5MB.";
                return false;
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExt.Contains(ext))
            {
                result.Errors[nameof(ApplicationUser.ProfileImage)] = "Invalid file type.";
                return false;
            }

            if (!AllowedMime.Contains(file.ContentType))
            {
                result.Errors[nameof(ApplicationUser.ProfileImage)] = "Invalid image content type.";
                return false;
            }

            if (!ImageSignatureValidator.HasValidSignature(file))
            {
                result.Errors[nameof(ApplicationUser.ProfileImage)] = "Invalid or corrupted image.";
                return false;
            }
            var faceCheckResult = await _userFaceCheckService.HasExactlyOneFace(file);
            if (!faceCheckResult.IsValid)
            {
                result.Errors[nameof(ApplicationUser.ProfileImage)] = faceCheckResult.Message!;
                return false;
            }
            return true;
        }
    }
}