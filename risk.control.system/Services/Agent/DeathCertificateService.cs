using System.Text.RegularExpressions;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Agent
{
    public interface IDeathCertificateService
    {
        Task<byte[]> Process(byte[] idImage, IReadOnlyList<TextBlock> imageReadOnly, ClientCompany company, DocumentIdReport doc, int pageNumber = 1);
        //byte[] MaskAdharIfFound(byte[] idImage, IReadOnlyList<TextBlock> imageReadOnly, string adharNumber);
    }
    internal class DeathCertificateService(IGoogleMaskHelper googleHelper, IProcessImageService processImageService, IHttpClientService httpClientService, IWebHostEnvironment env, ILogger<DeathCertificateService> logger) : IDeathCertificateService
    {
        private static Regex regex = new Regex(@"\b\d{4}\s?\d{4}\s?\d{4}\b");
        private readonly string docyTypeDeathCertificateName = DocumentIdReportType.DEATH_CERTIFICATE.GetEnumDisplayName();
        private readonly IGoogleMaskHelper _googleHelper = googleHelper;
        private readonly IProcessImageService _processImageService = processImageService;
        private readonly IHttpClientService _httpClientService = httpClientService;
        private readonly IWebHostEnvironment _env = env;
        private readonly ILogger<DeathCertificateService> _logger = logger;
        public async Task<byte[]> Process(byte[] idImage, IReadOnlyList<TextBlock> imageReadOnly, ClientCompany company, DocumentIdReport doc, int pageNumber = 1)
        {
            var documentFileForPage = doc.DocumentFiles.FirstOrDefault(d => d.PageNumber == pageNumber);
            var filePath = Path.Combine(_env.ContentRootPath, documentFileForPage?.FilePath ?? string.Empty);
            try
            {
                string extractedText = imageReadOnly.FirstOrDefault()?.Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    doc.ImageValid = false;
                    idImage = await SaveCompressedInValidImage(filePath, idImage);
                    documentFileForPage!.OcrText = "No text detected in the image";
                    return idImage;
                }
                var extractedTextToLower = extractedText.ToLowerInvariant();
                if (pageNumber == 1)
                {
                    bool hasKeywords = extractedTextToLower.Contains("death") && (extractedTextToLower.Contains("registration") || extractedTextToLower.Contains("registrar")) && extractedTextToLower.Contains("date");
                    doc.ImageValid = hasKeywords;
                }
                {
                    if (doc.ImageValid == true)
                    {
                        documentFileForPage!.OcrText = extractedText;
                    }
                    else
                    {
                        documentFileForPage!.OcrText = "Invalid DEATH CERTIFICATE detected";
                    }
                    idImage = await SaveCompressedValidImage(filePath, idImage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during DEATH CERTIFICATE document processing.");
                idImage = await SaveCompressedInValidImage(filePath, idImage);
                doc.LongLatTime = DateTime.UtcNow;
                documentFileForPage!.OcrText = "Error occurred during DEATH CERTIFICATE document processing.";
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
