using System.Text.RegularExpressions;

using Google.Cloud.Vision.V1;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Agent
{
    public interface IPanCardService
    {
        Task<DocumentIdReport> Process(byte[] IdImage, IReadOnlyList<TextBlock> imageReadOnly, ClientCompany company, DocumentIdReport doc, string onlyExtension);
        Task<DocumentIdReport> Process(byte[] IdImage, IReadOnlyList<EntityAnnotation> imageReadOnly, ClientCompany company, DocumentIdReport doc, string onlyExtension);
    }

    internal class PanCardService : IPanCardService
    {
        private static string panNumber2Find = "Permanent Account Number";
        private static string newPanNumber2Find = "Permanent Account Number Card";
        private static readonly Regex panRegex = new Regex(@"[A-Z]{5}\d{4}[A-Z]{1}");
        private readonly IGoogleMaskHelper googleHelper;
        private readonly IProcessImageService processImageService;
        private readonly IHttpClientService httpClientService;
        private readonly IWebHostEnvironment env;
        private readonly ILogger<PanCardService> logger;
        private string panNumber = string.Empty;
        private string docyTypePan = string.Empty;
        private byte[]? ocrImaged = null;
        public PanCardService(IGoogleMaskHelper googleHelper, IProcessImageService processImageService, IHttpClientService httpClientService, IWebHostEnvironment env, ILogger<PanCardService> logger)
        {
            this.googleHelper = googleHelper;
            this.processImageService = processImageService;
            this.httpClientService = httpClientService;
            this.env = env;
            this.logger = logger;
        }

        public async Task<DocumentIdReport> Process(byte[] IdImage, IReadOnlyList<TextBlock> imageReadOnly, ClientCompany company, DocumentIdReport doc, string onlyExtension)
        {
            var filePath = Path.Combine(env.ContentRootPath, doc.FilePath!);
            var allPanText = imageReadOnly.FirstOrDefault()!.Text;
            var panTextPre = allPanText.IndexOf(panNumber2Find);
            if (panTextPre > 0)
            {
                panTextPre = allPanText.IndexOf(newPanNumber2Find);
                if (panTextPre > 0)
                {
                    panNumber = allPanText.Substring(panTextPre + newPanNumber2Find.Length + 1, 10);
                    docyTypePan = allPanText.IndexOf(newPanNumber2Find) > 0 && allPanText.Length > allPanText.IndexOf(newPanNumber2Find) ? "PAN" : "UNKNOWN";
                    ocrImaged = googleHelper.MaskPanTextInImage(IdImage, imageReadOnly, newPanNumber2Find);
                }
                else
                {
                    panTextPre = allPanText.IndexOf(panNumber2Find);
                    panNumber = allPanText.Substring(panTextPre + panNumber2Find.Length + 1, 10);
                    docyTypePan = allPanText.IndexOf(panNumber2Find) > 0 && allPanText.Length > allPanText.IndexOf(panNumber2Find) ? "PAN" : "UNKNOWN";
                    ocrImaged = googleHelper.MaskPanTextInImage(IdImage, imageReadOnly, panNumber2Find);
                }
            }
            var maskedImage = new FaceImageDetail { DocType = docyTypePan, DocumentId = panNumber, MaskedImage = ocrImaged != null ? Convert.ToBase64String(ocrImaged) : string.Empty, OcrData = allPanText };
            try
            {
                if (company.VerifyPan)
                {
                    var panResponse = await httpClientService.VerifyPanNew(maskedImage.DocumentId, company.PanIdfyUrl, company.PanAPIData, company.PanAPIHost);
                    if (panResponse != null && panResponse.valid)
                    {
                        var panMatch = panRegex.Match(maskedImage.DocumentId);
                        doc.ImageValid = panMatch.Success && panResponse.valid ? true : false;
                    }
                }
                else
                {
                    var panMatch = panRegex.Match(maskedImage.DocumentId);
                    doc.ImageValid = panMatch.Success ? true : false;
                }
                await File.WriteAllBytesAsync(filePath, processImageService.CompressImage(Convert.FromBase64String(maskedImage.MaskedImage)));
                doc.LocationInfo = maskedImage.DocType + " data: ";
                if (!string.IsNullOrWhiteSpace(maskedImage.OcrData))
                {
                    doc.LocationInfo = maskedImage.DocType + " data:. \r\n " + "" + maskedImage.OcrData.Replace(maskedImage.DocumentId, "xxxxxxxxxx");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                await File.WriteAllBytesAsync(filePath, processImageService.CompressImage(Convert.FromBase64String(maskedImage.MaskedImage)));
                doc.LongLatTime = DateTime.UtcNow;
                doc.LocationInfo = "no data: ";
            }
            return doc;
        }
        public async Task<DocumentIdReport> Process(byte[] IdImage, IReadOnlyList<EntityAnnotation> imageReadOnly, ClientCompany company, DocumentIdReport doc, string onlyExtension)
        {
            var filePath = Path.Combine(env.ContentRootPath, doc.FilePath!);
            var allPanText = imageReadOnly.FirstOrDefault()!.Description;
            var panTextPre = allPanText.IndexOf(panNumber2Find);
            if (panTextPre > 0)
            {
                panTextPre = allPanText.IndexOf(newPanNumber2Find);
                if (panTextPre > 0)
                {
                    panNumber = allPanText.Substring(panTextPre + newPanNumber2Find.Length + 1, 10);
                    docyTypePan = allPanText.IndexOf(newPanNumber2Find) > 0 && allPanText.Length > allPanText.IndexOf(newPanNumber2Find) ? "PAN" : "UNKNOWN";
                    ocrImaged = googleHelper.MaskPanTextInImage(IdImage, imageReadOnly, newPanNumber2Find);
                }
                else
                {
                    panTextPre = allPanText.IndexOf(panNumber2Find);
                    panNumber = allPanText.Substring(panTextPre + panNumber2Find.Length + 1, 10);
                    docyTypePan = allPanText.IndexOf(panNumber2Find) > 0 && allPanText.Length > allPanText.IndexOf(panNumber2Find) ? "PAN" : "UNKNOWN";
                    ocrImaged = googleHelper.MaskPanTextInImage(IdImage, imageReadOnly, panNumber2Find);
                }
            }
            var maskedImage = new FaceImageDetail { DocType = docyTypePan, DocumentId = panNumber, MaskedImage = ocrImaged != null ? Convert.ToBase64String(ocrImaged) : string.Empty, OcrData = allPanText };
            try
            {
                if (company.VerifyPan)
                {
                    var panResponse = await httpClientService.VerifyPanNew(maskedImage.DocumentId, company.PanIdfyUrl, company.PanAPIData, company.PanAPIHost);
                    if (panResponse != null && panResponse.valid)
                    {
                        var panMatch = panRegex.Match(maskedImage.DocumentId);
                        doc.ImageValid = panMatch.Success && panResponse.valid ? true : false;
                    }
                }
                else
                {
                    var panMatch = panRegex.Match(maskedImage.DocumentId);
                    doc.ImageValid = panMatch.Success ? true : false;
                }
                await File.WriteAllBytesAsync(filePath, processImageService.CompressImage(Convert.FromBase64String(maskedImage.MaskedImage)));
                doc.LocationInfo = maskedImage.DocType + " data: ";
                if (!string.IsNullOrWhiteSpace(maskedImage.OcrData))
                {
                    doc.LocationInfo = maskedImage.DocType + " data:. \r\n " + "" + maskedImage.OcrData.Replace(maskedImage.DocumentId, "xxxxxxxxxx");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                await File.WriteAllBytesAsync(filePath, processImageService.CompressImage(Convert.FromBase64String(maskedImage.MaskedImage)));
                doc.LongLatTime = DateTime.UtcNow;
                doc.LocationInfo = "no data: ";
            }
            return doc;
        }
    }
}