using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Rendering;

namespace risk.control.system.Helpers
{
    public static class Extensions
    {
        private static readonly Dictionary<string, string> FileSignatures = new Dictionary<string, string>
    {
        { "89504E47", ".png" },      // PNG
        { "FFD8FFE0", ".jpg" },      // JPG (JPEG)
        { "FFD8FFE1", ".jpg" },      // JPG (JPEG with EXIF)
        { "FFD8FFE2", ".jpg" },      // JPG (JPEG with ICC)
        { "47494638", ".gif" },      // GIF
        { "25504446", ".pdf" },      // PDF
        { "504B0304", ".zip" },      // ZIP
        { "494433",   ".mp3" },      // MP3
        { "52494646", ".wav" },      // WAV
        { "4D546864", ".mid" },      // MIDI
        { "52494646", ".avi" },      // AVI
        { "00000018", ".mp4" },      // MP4
        { "66747970", ".mp4" },      // MP4 (ftyp)
        { "1A45DFA3", ".mkv" },      // MKV
        { "00000014", ".mov" },      // MOV
        { "464C56",   ".flv" },      // FLV
        { "3026B275", ".wmv" },      // WMV
    };

        public static string GetFileExtension(this byte[] fileBytes)
        {
            if (fileBytes == null || fileBytes.Length < 4)
                return "unknown";

            // Convert the first few bytes to a hexadecimal string
            string fileHeader = BitConverter.ToString(fileBytes.Take(8).ToArray()).Replace("-", "").ToUpper();

            // Match the header against the known signatures
            foreach (var signature in FileSignatures)
            {
                if (fileHeader.StartsWith(signature.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return signature.Value;
                }
            }

            return "unknown"; // Return "unknown" if no match is found
        }

        public static string GetImageExtension(this byte[] imageBytes)
        {
            // Check the magic numbers for common image formats
            if (imageBytes.Length > 4)
            {
                if (imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && imageBytes[2] == 0x4E && imageBytes[3] == 0x47) // PNG
                    return "png";
                if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8 && imageBytes[2] == 0xFF) // JPEG
                    return "jpg";
                if (imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46) // GIF
                    return "gif";
                if (imageBytes[0] == 0x42 && imageBytes[1] == 0x4D) // BMP
                    return "bmp";
                if (imageBytes[0] == 0x52 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46 && imageBytes[3] == 0x46) // WebP (RIFF header)
                    return "webp";
            }

            return null; // Unknown format
        }
        public static string GetEnumDisplayName(this Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DisplayAttribute[] attributes = (DisplayAttribute[])fi.GetCustomAttributes(typeof(DisplayAttribute), false);

            if (attributes != null && attributes.Length > 0)
                return attributes[0].Name;
            else
                return value.ToString();
        }

        public static bool ValidateEmail(this string email)
        {
            // Regular expression pattern for email validation
            string emailPattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";

            // Create a Regex object with the email pattern
            Regex regex = new Regex(emailPattern);

            // Perform the email validation
            bool isValid = regex.IsMatch(email);

            // Return the validation result
            return isValid;
        }

        public static bool IsBase64String(this string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out int bytesParsed);
        }
    }
    public static class HtmlHelperExtensions
    {
        public static IEnumerable<SelectListItem> GetEnumSelectListWithDefaultValue<TEnum>(this IHtmlHelper htmlHelper, TEnum defaultValue)
            where TEnum : struct
        {
            var selectList = htmlHelper.GetEnumSelectList<TEnum>().ToList();
            selectList.Single(x => x.Value == $"{(int)(object)defaultValue}").Selected = true;
            return selectList;
        }
    }
}