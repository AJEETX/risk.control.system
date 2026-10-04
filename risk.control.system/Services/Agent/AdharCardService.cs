using System.Text.RegularExpressions;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Agent
{
    public interface IAdharCardService
    {
        Task<byte[]> Process(byte[] idImage, IReadOnlyList<TextBlock> imageReadOnly, ClientCompany company, DocumentIdReport doc);
        //byte[] MaskAdharIfFound(byte[] idImage, IReadOnlyList<TextBlock> imageReadOnly, string adharNumber);
    }
    internal class AdharCardService(IGoogleMaskHelper googleHelper, IProcessImageService processImageService, IHttpClientService httpClientService, IWebHostEnvironment env, ILogger<AdharCardService> logger) : IAdharCardService
    {
        private static Regex regex = new Regex(@"\b\d{4}\s?\d{4}\s?\d{4}\b");
        private readonly string docyTypeAdharName = DocumentIdReportType.ADHAAR.GetEnumDisplayName();
        private readonly IGoogleMaskHelper _googleHelper = googleHelper;
        private readonly IProcessImageService _processImageService = processImageService;
        private readonly IHttpClientService _httpClientService = httpClientService;
        private readonly IWebHostEnvironment _env = env;
        private readonly ILogger<AdharCardService> _logger = logger;
        public async Task<byte[]> Process(byte[] idImage, IReadOnlyList<TextBlock> imageReadOnly, ClientCompany company, DocumentIdReport doc)
        {
            var filePath = Path.Combine(_env.ContentRootPath, doc.FilePath!);
            try
            {
                string extractedText = imageReadOnly.FirstOrDefault()?.Text ?? string.Empty;
                Match match = regex.Match(extractedText);
                if (match.Success)
                {
                    // Clean up space characters to return raw 12 digits
                    var adharNumber = match.Value.Replace(" ", "").Trim();
                    idImage = MaskAdharIfFound(idImage, imageReadOnly, match.Value);
                    doc.ImageValid = await ValidateAdharNumber(adharNumber, company);
                    idImage = await SaveCompressedImage(filePath, idImage);
                    doc.LocationInfo = FormatLocationInfo(extractedText, match.Value, docyTypeAdharName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during ADHAR document processing.");
                idImage = await SaveCompressedImage(filePath, idImage);
                doc.LongLatTime = DateTime.UtcNow;
                doc.LocationInfo = "no data: ";
            }

            return idImage;
        }

        private string FormatLocationInfo(string extractedText, string adharNumber, string documentType)
        {
            if (!string.IsNullOrWhiteSpace(adharNumber) && documentType == docyTypeAdharName)
            {
                string maskedText = extractedText.Replace(adharNumber[..9], "XXXX XXXX ");
                return $"{documentType} data: \r\n {maskedText}";
            }

            return $"{documentType} data: \r\n {extractedText}";
        }
        public byte[] MaskAdharIfFound(byte[] idImage, IReadOnlyList<TextBlock> imageReadOnly, string adharNumber)
        {
            return _googleHelper.MaskAdharNumberTextInImage(idImage, imageReadOnly, adharNumber);
        }
        private async Task<bool> ValidateAdharNumber(string adharNumber, ClientCompany company)
        {
            if (company.VerifyPan)
            {
                return await _httpClientService.VerifyAdhar(adharNumber);
            }

            return true;
        }
        private async Task<byte[]> SaveCompressedImage(string filePath, byte[] imageBytes)
        {
            byte[] compressed = _processImageService.CompressImage(imageBytes);
            await File.WriteAllBytesAsync(filePath, compressed);
            return compressed;
        }
    }
}
