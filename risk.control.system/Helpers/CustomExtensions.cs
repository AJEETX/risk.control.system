using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace risk.control.system.Helpers
{
    public static class CustomExtensions
    {
        public static CultureInfo GetCultureByCountry(string countryCode)
        {
            // Dictionary of country codes to valid culture names
            var countryToCulture = new Dictionary<string, string>
            {
                { "US", "en-US" }, // United States
                { "AU", "en-AU" }, // United States
                { "IN", "hi-IN" }, // India (Hindi)
                { "FR", "fr-FR" }, // France
                { "JP", "ja-JP" }, // Japan
                { "DE", "de-DE" }, // Germany
                { "GB", "en-GB" }, // United Kingdom
                { "CN", "zh-CN" }, // China
                { "AE", "ar-AE" }  // United Arab Emirates
            };

            // Return CultureInfo based on country code, or default to "en-US"
            return new CultureInfo(countryToCulture.ContainsKey(countryCode) ? countryToCulture[countryCode] : "en-US");
        }

        private static readonly Dictionary<byte[], string> ImageSignatures = new()
        {
            { new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "png" },
            { new byte[] { 0xFF, 0xD8, 0xFF }, "jpg" },
            { new byte[] { 0x47, 0x49, 0x46 }, "gif" },
            { new byte[] { 0x42, 0x4D }, "bmp" },
            { new byte[] { 0x52, 0x49, 0x46, 0x46 }, "webp" }
        };

        public static string? GetImageExtension(this byte[]? imageBytes)
        {
            if (imageBytes == null) return null;

            foreach (var sig in ImageSignatures)
            {
                if (imageBytes.Take(sig.Key.Length).SequenceEqual(sig.Key))
                    return sig.Value;
            }
            return null;
        }

        public static string GetEnumDisplayName(this Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString())!;

            DisplayAttribute[] attributes = (DisplayAttribute[])fi.GetCustomAttributes(typeof(DisplayAttribute), false);

            if (attributes != null && attributes.Length > 0)
                return attributes[0].Name!;
            else
                return value.ToString();
        }

        public static bool ValidateEmail(this string email)
        {
            string emailPattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";
            Regex regex = new Regex(emailPattern);
            bool isValid = regex.IsMatch(email);
            return isValid;
        }
    }
}