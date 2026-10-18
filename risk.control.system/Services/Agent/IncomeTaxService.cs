using System.Text.RegularExpressions;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Agent
{
    public interface IIncomeTaxService
    {
        Task<byte[]> Process(byte[] idImage, IReadOnlyList<TextBlock> imageReadOnly, ClientCompany company, DocumentIdReport doc);
        //byte[] MaskAdharIfFound(byte[] idImage, IReadOnlyList<TextBlock> imageReadOnly, string adharNumber);
    }
    internal class IncomeTaxService(IGoogleMaskHelper googleHelper, IProcessImageService processImageService, IHttpClientService httpClientService, IWebHostEnvironment env, ILogger<IncomeTaxService> logger) : IIncomeTaxService
    {
        private static Regex regex = new Regex(@"\b\d{4}\s?\d{4}\s?\d{4}\b");
        private readonly string docyTypeIncomeTaxName = DocumentIdReportType.ITR.GetEnumDisplayName();
        private readonly IGoogleMaskHelper _googleHelper = googleHelper;
        private readonly IProcessImageService _processImageService = processImageService;
        private readonly IHttpClientService _httpClientService = httpClientService;
        private readonly IWebHostEnvironment _env = env;
        private readonly ILogger<IncomeTaxService> _logger = logger;
        public async Task<byte[]> Process(byte[] idImage, IReadOnlyList<TextBlock> imageReadOnly, ClientCompany company, DocumentIdReport doc)
        {
            var filePath = Path.Combine(_env.ContentRootPath, doc.FilePath!);
            try
            {
                string extractedText = imageReadOnly.FirstOrDefault()?.Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    doc.ImageValid = false;
                    idImage = await SaveCompressedInValidImage(filePath, idImage);
                    doc.LocationInfo = "No text detected in the image";
                    return idImage;
                }
                var extractedTextToLower = extractedText.ToLowerInvariant();
                bool hasKeywords = extractedTextToLower.Contains("income") && (extractedTextToLower.Contains("tax") || extractedTextToLower.Contains("return")) && extractedTextToLower.Contains("date");
                if (hasKeywords)
                {
                    // Clean up space characters to return raw 12 digits
                    doc.ImageValid = true;
                    idImage = await SaveCompressedValidImage(filePath, idImage);
                    doc.LocationInfo = extractedText;
                }
                else
                {
                    doc.ImageValid = false;
                    idImage = await SaveCompressedInValidImage(filePath, idImage);
                    doc.LocationInfo = "Invalid ITR detected";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during INCOME TAX document processing.");
                idImage = await SaveCompressedInValidImage(filePath, idImage);
                doc.LongLatTime = DateTime.UtcNow;
                doc.LocationInfo = "no data: ";
            }

            return idImage;
        }

        private async Task<byte[]> SaveCompressedValidImage(string filePath, byte[] imageBytes)
        {
            var compressedWithWatermark = _processImageService.CompressAndWaterMarkImage(imageBytes, 80, "VALID", 0f);
            await File.WriteAllBytesAsync(filePath, compressedWithWatermark);
            byte[] compressed = _processImageService.CompressImage(imageBytes, 80, 0f);
            return compressed;
        }
        private async Task<byte[]> SaveCompressedInValidImage(string filePath, byte[] imageBytes)
        {
            var compressedWithWatermark = _processImageService.CompressAndWaterMarkImage(imageBytes, 80, "INVALID", 5f);
            await File.WriteAllBytesAsync(filePath, compressedWithWatermark);
            byte[] compressed = _processImageService.CompressImage(imageBytes, 80, 5f);
            return compressed;
        }
    }
}
