using risk.control.system.Helpers;

namespace risk.control.system.Services.Common
{
    public interface IValidateImageService
    {
        void ValidateImage(IFormFile image, Dictionary<string, string> errors);
    }

    public class ValidateImageService : IValidateImageService
    {
        private const long MaxFileSize = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedExt = new() { ".jpg", ".jpeg", ".png" };
        private static readonly HashSet<string> AllowedMime = new() { "image/jpeg", "image/png" };

        public void ValidateImage(IFormFile image, Dictionary<string, string> errors)
        {
            if (image == null || image.Length == 0)
            {
                errors[image.Name] = "Invalid image";
                return;
            }

            if (image.Length > MaxFileSize)
                errors[image.Name] = "Image size exceeds 5MB";

            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedExt.Contains(ext))
                errors[image.Name] = "Invalid image type";

            if (!AllowedMime.Contains(image.ContentType))
                errors[image.Name] = "Invalid image content type";

            if (!ImageSignatureValidator.HasValidSignature(image))
                errors[image.Name] = "Invalid or corrupted image";
        }
    }
}