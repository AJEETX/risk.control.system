using System.Text.RegularExpressions;
using Microsoft.FeatureManagement;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agent
{
    public interface IAdharCardService
    {
        Task<byte[]> Process(byte[] idImage, byte[] registeredImage, IReadOnlyList<TextBlock> imageReadOnly, ClientCompany company, DocumentIdReport doc);
    }
    internal class AdharCardService(
        IGoogleMaskHelper googleHelper,
        IProcessImageService processImageService,
         IAmazonApiService amazonApiService,
        IHttpClientService httpClientService,
        IWebHostEnvironment env,
        IFeatureManager featureManager,
        IAwsFaceImageCheckService awsFaceImageCheckService,
        ILogger<AdharCardService> logger) : IAdharCardService
    {
        private static Regex regex = new Regex(@"\b\d{4}\s?\d{4}\s?\d{4}\b");
        private readonly string docyTypeAdharName = DocumentIdReportType.ADHAAR.GetEnumDisplayName();
        private readonly IGoogleMaskHelper _googleHelper = googleHelper;
        private readonly IProcessImageService _processImageService = processImageService;
        private readonly IAmazonApiService _amazonApiService = amazonApiService;
        private readonly IAwsFaceImageCheckService _awsFaceImageCheckService = awsFaceImageCheckService;
        private readonly IHttpClientService _httpClientService = httpClientService;
        private readonly IWebHostEnvironment _env = env;
        private readonly IFeatureManager _featureManager = featureManager;
        private readonly ILogger<AdharCardService> _logger = logger;
        public async Task<byte[]> Process(byte[] idImage, byte[] registeredImage, IReadOnlyList<TextBlock> imageReadOnly, ClientCompany company, DocumentIdReport doc)
        {
            var filePath = Path.Combine(_env.ContentRootPath, doc.FilePath!);
            try
            {
                var checkDocumentface = await _featureManager.IsEnabledAsync(FeatureFlags.DOCUMENT_FACE_MATCH_CHECK);
                if (checkDocumentface)
                {
                    var hasSingleFaceImageTask = _awsFaceImageCheckService.HasExactlyOneFace(idImage);
                    var faceCheckResultTask = _awsFaceImageCheckService.HasExactlyOneFace(idImage);
                    var faceMatchedTask = _awsFaceImageCheckService.FaceMatch(registeredImage, idImage);

                    await Task.WhenAll(hasSingleFaceImageTask, faceCheckResultTask, faceMatchedTask);
                    var hasSingleFaceImage = await hasSingleFaceImageTask;
                    if (!hasSingleFaceImage.IsValid)
                    {
                        doc.ImageValid = false;
                        doc.LocationInfo = "Image does not contain exactly one face";
                        idImage = await SaveCompressedInValidImage(filePath, idImage);
                        return idImage;
                    }
                    var faceCheckResult = await faceCheckResultTask;
                    if (!faceCheckResult.IsValid)
                    {
                        doc.ImageValid = false;
                        doc.LocationInfo = "Document image does not contain exactly one face";
                        idImage = await SaveCompressedInValidImage(filePath, idImage);
                        return idImage;
                    }
                    var faceMatched = await faceMatchedTask;
                    if (!faceMatched)
                    {
                        doc.ImageValid = false;
                        doc.LocationInfo = "Face match failed between registered image and document image";
                        idImage = await SaveCompressedInValidImage(filePath, idImage);
                        return idImage;
                    }
                }
                string extractedText = imageReadOnly.FirstOrDefault()?.Text ?? string.Empty;
                Match match = regex.Match(extractedText);
                if (match.Success)
                {
                    // Clean up space characters to return raw 12 digits
                    var adharNumber = match.Value.Replace(" ", "").Trim();
                    idImage = MaskAdharIfFound(idImage, imageReadOnly, match.Value);
                    doc.ImageValid = await ValidateAdharNumber(adharNumber, company);
                    idImage = await SaveCompressedValidImage(filePath, idImage);
                    doc.LocationInfo = FormatLocationInfo(extractedText, match.Value, docyTypeAdharName);
                }
                else
                {
                    doc.ImageValid = false;
                    idImage = await SaveCompressedInValidImage(filePath, idImage);
                    doc.LocationInfo = "No ADHAR number detected";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during ADHAR document processing.");
                idImage = await SaveCompressedInValidImage(filePath, idImage);
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
        private byte[] MaskAdharIfFound(byte[] idImage, IReadOnlyList<TextBlock> imageReadOnly, string adharNumber)
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
