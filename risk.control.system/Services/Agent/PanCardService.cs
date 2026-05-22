using System.Text.RegularExpressions;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Agent
{
    public interface IPanCardService
    {
        Task<DocumentIdReport> Process(byte[] IdImage, IReadOnlyList<TextBlock> imageReadOnly, ClientCompany company, DocumentIdReport doc, string onlyExtension);
    }

    internal class PanCardService(IGoogleMaskHelper googleHelper, IProcessImageService processImageService, IHttpClientService httpClientService, IWebHostEnvironment env, ILogger<PanCardService> logger) : IPanCardService
    {
        private const string panNumber2Find = "Permanent Account Number";
        private const string newPanNumber2Find = "Permanent Account Number Card";
        private readonly string docyTypePanName = "PAN";
        private readonly string docyTypeUnknownName = "UNKNOWN";
        private static readonly Regex panRegex = new Regex(@"[A-Z]{5}\d{4}[A-Z]{1}");
        private readonly IGoogleMaskHelper _googleHelper = googleHelper;
        private readonly IProcessImageService _processImageService = processImageService;
        private readonly IHttpClientService _httpClientService = httpClientService;
        private readonly IWebHostEnvironment _env = env;
        private readonly ILogger<PanCardService> _logger = logger;
        private string panNumber = string.Empty;
        private string docyTypePan = string.Empty;

        public async Task<DocumentIdReport> Process(byte[] idImage, IReadOnlyList<TextBlock> imageReadOnly, ClientCompany company, DocumentIdReport doc, string onlyExtension)
        {
            var filePath = Path.Combine(_env.ContentRootPath, doc.FilePath!);
            string allPanText = imageReadOnly.FirstOrDefault()?.Text ?? string.Empty;

            try
            {
                var (panNumber, documentType) = ExtractPanAndType(allPanText);
                idImage = MaskPanIfFound(idImage, imageReadOnly, allPanText);

                doc.ImageValid = await ValidatePanNumber(panNumber, company);

                await SaveCompressedImage(filePath, idImage);

                doc.LocationInfo = FormatLocationInfo(allPanText, panNumber, documentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during PAN document processing.");

                await SaveCompressedImage(filePath, idImage);
                doc.LongLatTime = DateTime.UtcNow;
                doc.LocationInfo = "no data: ";
            }

            return doc;
        }

        private (string PanNumber, string DocumentType) ExtractPanAndType(string allPanText)
        {
            string panNumber = string.Empty;
            string docType = docyTypeUnknownName;

            if (allPanText.Contains(newPanNumber2Find))
            {
                int index = allPanText.IndexOf(newPanNumber2Find);
                panNumber = SafeSubstring(allPanText, index + newPanNumber2Find.Length + 1, 10);
                docType = docyTypePanName;
            }
            else if (allPanText.Contains(panNumber2Find))
            {
                int index = allPanText.IndexOf(panNumber2Find);
                panNumber = SafeSubstring(allPanText, index + panNumber2Find.Length + 1, 10);
                docType = docyTypePanName;
            }

            return (panNumber, docType);
        }

        private byte[] MaskPanIfFound(byte[] idImage, IReadOnlyList<TextBlock> imageReadOnly, string allPanText)
        {
            if (allPanText.Contains(newPanNumber2Find))
            {
                return _googleHelper.MaskPanTextInImage(idImage, imageReadOnly, newPanNumber2Find);
            }
            if (allPanText.Contains(panNumber2Find))
            {
                return _googleHelper.MaskPanTextInImage(idImage, imageReadOnly, panNumber2Find);
            }
            return idImage;
        }

        private async Task<bool> ValidatePanNumber(string panNumber, ClientCompany company)
        {
            bool isRegexValid = !string.IsNullOrEmpty(panNumber) && panRegex.Match(panNumber).Success;

            if (company.VerifyPan)
            {
                var panResponse = await _httpClientService.VerifyPanNew(panNumber, company.PanIdfyUrl, company.PanAPIData, company.PanAPIHost);

                return isRegexValid && panResponse != null && panResponse.valid;
            }

            return isRegexValid;
        }

        private string FormatLocationInfo(string allPanText, string panNumber, string documentType)
        {
            if (!string.IsNullOrWhiteSpace(panNumber) && documentType == docyTypePanName)
            {
                string maskedText = allPanText.Replace(panNumber, "XXXXXXXXXXX");
                return $"{documentType} data: \r\n {maskedText}";
            }

            return $"{documentType} data: \r\n {allPanText}";
        }

        private async Task SaveCompressedImage(string filePath, byte[] imageBytes)
        {
            byte[] compressed = _processImageService.CompressImage(imageBytes);
            await File.WriteAllBytesAsync(filePath, compressed);
        }

        private string SafeSubstring(string text, int startIndex, int length)
        {
            if (startIndex < 0 || startIndex >= text.Length) return string.Empty;
            if (startIndex + length > text.Length) return text.Substring(startIndex);
            return text.Substring(startIndex, length);
        }
    }
}