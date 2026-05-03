using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Common
{
    public interface IValidateImageService
    {
        void ValidateImage(IFormFile? image, Dictionary<string, string> errors);
        Task ValidateFaceImage(IFormFile? image, Dictionary<string, string> errors);
        Task<bool> ValidateCompanyUserImage(IFormFile file, ServiceResult result);
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
            var singleFaceResult = await _userFaceCheckService.HasExactlyOneFace(image);
            if (!singleFaceResult.IsValid)
            {
                errors[image.Name] = singleFaceResult.Message!;
            }
            else
            {
                var matchedFace = await _userFaceCheckService.CheckFaceImageExistAsync(image!);
                if (matchedFace)
                {
                    errors[image.Name] = "Profile image matches with existing users. Please use a different image.";
                }
            }
        }
        public async Task<bool> ValidateCompanyUserImage(IFormFile file, ServiceResult result)
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
            var singleFaceResult = await _userFaceCheckService.HasExactlyOneFace(file);
            if (!singleFaceResult.IsValid)
            {
                result.Errors[nameof(ApplicationUser.ProfileImage)] = singleFaceResult.Message!;
                return false;
            }

            var matchedFace = await _userFaceCheckService.CheckFaceImageExistAsync(file!);
            if (matchedFace)
            {
                result.Errors[nameof(ApplicationUser.ProfileImage)] = "Profile image matches with existing users. Please use a different image.";
                return false;
            }

            return true;
        }
    }
}