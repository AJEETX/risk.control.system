﻿using Google.Cloud.Vision.V1;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using System.Text.RegularExpressions;

namespace risk.control.system.Services
{
    public interface IPanCardService
    {
        Task<DocumentIdReport> Process(byte[] IdImage, IReadOnlyList<EntityAnnotation> imageReadOnly, ClientCompany company, DocumentIdReport doc, string onlyExtension);
    }
    public class PanCardService : IPanCardService
    {
        private static string panNumber2Find = "Permanent Account Number";
        private static Regex panRegex = new Regex(@"[A-Z]{5}\d{4}[A-Z]{1}");
        private readonly IGoogleMaskHelper googleHelper;
        private readonly IHttpClientService httpClientService;

        public PanCardService(IGoogleMaskHelper googleHelper, IHttpClientService httpClientService)
        {
            this.googleHelper = googleHelper;
            this.httpClientService = httpClientService;
        }
        public async Task<DocumentIdReport> Process(byte[] IdImage, IReadOnlyList<EntityAnnotation> imageReadOnly, ClientCompany company, DocumentIdReport doc, string onlyExtension)
        {
            var allPanText = imageReadOnly.FirstOrDefault().Description;
            var panTextPre = allPanText.IndexOf(panNumber2Find);
            var panNumber = allPanText.Substring(panTextPre + panNumber2Find.Length + 1, 10);


            var ocrImaged = googleHelper.MaskPanTextInImage(IdImage, imageReadOnly, panNumber2Find);
            var docyTypePan = allPanText.IndexOf(panNumber2Find) > 0 && allPanText.Length > allPanText.IndexOf(panNumber2Find) ? "PAN" : "UNKNOWN";
            var maskedImage = new FaceImageDetail
            {
                DocType = docyTypePan,
                DocumentId = panNumber,
                MaskedImage = Convert.ToBase64String(ocrImaged),
                OcrData = allPanText
            };
            try
            {
                #region// PAN VERIFICATION ::: //test PAN FNLPM8635N, BYSPP5796F
                if (company.VerifyPan)
                {
                    var panResponse = await httpClientService.VerifyPanNew(maskedImage.DocumentId, company.PanIdfyUrl, company.PanAPIKey, company.PanAPIHost);
                    if (panResponse != null && panResponse.valid)
                    {
                        var panMatch = panRegex.Match(maskedImage.DocumentId);
                        doc.IdImageValid = panMatch.Success && panResponse.valid ? true : false;
                    }
                }
                else
                {
                    var panMatch = panRegex.Match(maskedImage.DocumentId);
                    doc.IdImageValid = panMatch.Success ? true : false;
                }

                #endregion PAN IMAGE PROCESSING

                var image = Convert.FromBase64String(maskedImage.MaskedImage);
                var savedMaskedImage = CompressImage.ProcessCompress(image, onlyExtension);
                doc.IdImage = savedMaskedImage;
                doc.IdImageData = maskedImage.DocType + " data: ";

                if (!string.IsNullOrWhiteSpace(maskedImage.OcrData))
                {
                    doc.IdImageData = maskedImage.DocType + " data:. \r\n " +
                        "" + maskedImage.OcrData.Replace(maskedImage.DocumentId, "xxxxxxxxxx");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                var image = Convert.FromBase64String(maskedImage.MaskedImage);
                doc.IdImage = CompressImage.ProcessCompress(image, onlyExtension);
                doc.IdImageLongLatTime = DateTime.Now;
                doc.IdImageData = "no data: ";
            }
            return doc;
        }
    }
}
