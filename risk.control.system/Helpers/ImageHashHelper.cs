using System.Security.Cryptography;

namespace risk.control.system.Helpers
{
    public static class ImageHashHelper
    {
        public static string GenerateImageHash(byte[] imageBytes)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(imageBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}
