using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agent;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agentic
{
    public interface IAgenticService
    {
        Task<(bool, string)> FaceExistsAsync(IFormFile image);

        Task<bool> CaseAdjudicatedAsync(AdjudicationRequest request);
        //byte[] ConvertImageToSearchablePdfBytes(string inputImagePath);
    }
    internal class AgenticService(ApplicationDbContext dbContext, IAmazonApiService amazonApiService, ISmsService smsService) : IAgenticService
    {
        //private static string tessDataPath = @"./tessdata"; // Path to your tessdata folder
        private readonly IAmazonApiService _amazonApiService = amazonApiService;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly ISmsService _smsService = smsService;

        public async Task<bool> CaseAdjudicatedAsync(AdjudicationRequest request)
        {
            var caseTask = await _dbContext.Investigations.Include(c => c.PolicyDetail).FirstOrDefaultAsync(i => !i.Deleted && i.AiEnabled && i.PolicyDetail!.ContractNumber == request.PolicyNumber.Trim());
            if (caseTask == null)
            {
                throw new InvalidOperationException("Investigation task not found.");
            }
            caseTask.AdjudicationCompleted = request.Set;
            _dbContext.Investigations.Update(caseTask);
            var result = await _dbContext.SaveChangesAsync() > 0;

            var assessor = await _dbContext.ApplicationUser.Include(a => a.Country).FirstOrDefaultAsync(u => u.Email == request.Email);
            if (assessor == null)
            {
                throw new InvalidOperationException("Assessor not found.");
            }
            var message = $"Dear {assessor.Email}\n";
            message += $"Your case with policy number {caseTask.PolicyDetail!.ContractNumber} has been adjudicated.\n";
            message += "Please process the case.\n";
            message += "Thanks \n";
            await _smsService.SendSmsAsync(assessor.Country!.Code, assessor.Country!.ISDCode.ToString() + assessor.PhoneNumber, message);

            return result;
        }

        public async Task<(bool, string)> FaceExistsAsync(IFormFile image)
        {
            var imageCollection = EnvHelper.Get(CONSTANTS.FaceImageCollection);

            await _amazonApiService.EnsureCollectionExistsAsync(imageCollection!);

            var memoryStream = new MemoryStream();

            await image.CopyToAsync(memoryStream);

            var searchRequest = new SearchFacesByImageRequest
            {
                CollectionId = imageCollection!,
                Image = new Image { Bytes = memoryStream },
                MaxFaces = 1,
                FaceMatchThreshold = 90F
            };

            var response = await _amazonApiService.SearchFacesByImageAsync(searchRequest);
            if (response.FaceMatches.Count > 0)
            {
                return (true, "Face match found.");
            }
            var indexRequest = new IndexFacesRequest
            {
                CollectionId = imageCollection!,
                Image = new Image { Bytes = memoryStream },
                ExternalImageId = Guid.NewGuid().ToString(),
                MaxFaces = 1,
                QualityFilter = QualityFilter.AUTO
            };
            var faceAddResponse = await _amazonApiService.IndexFacesAsync(indexRequest);
            var faceId = faceAddResponse.FaceRecords.FirstOrDefault()?.Face.FaceId;
            if (faceId != null)
            {
                return (false, "Face added to collection.");
            }
            return (true, "Error occurred while processing the image.");
        }

        //public byte[] ConvertImageToSearchablePdfBytes(string inputImagePath)
        //{
        //    string tempPathWithoutExtension = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        //    string expectedPhysicalPdfPath = tempPathWithoutExtension + ".pdf";

        //    try
        //    {
        //        using (var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default))
        //        {
        //            // 2. Create the PDF renderer targeting our temporary file path location
        //            using (var renderer = ResultRenderer.CreatePdfRenderer(tempPathWithoutExtension, tessDataPath, false))
        //            {
        //                using (renderer.BeginDocument("Extracted Document"))
        //                {
        //                    using (var img = Pix.LoadFromFile(inputImagePath))
        //                    {
        //                        using (var page = engine.Process(img))
        //                        {
        //                            bool success = renderer.AddPage(page);
        //                            if (!success)
        //                            {
        //                                throw new Exception("Tesseract failed to layout the OCR map to the renderer.");
        //                            }
        //                        }
        //                    }
        //                } // renderer.EndDocument() is explicitly executed here on disposal
        //            }
        //        }

        //        if (File.Exists(expectedPhysicalPdfPath))
        //        {
        //            byte[] pdfBytes = File.ReadAllBytes(expectedPhysicalPdfPath);
        //            return pdfBytes;
        //        }

        //        throw new FileNotFoundException("The OCR output PDF file was not generated by Tesseract.");
        //    }
        //    finally
        //    {
        //        if (File.Exists(expectedPhysicalPdfPath))
        //        {
        //            try
        //            {
        //                File.Delete(expectedPhysicalPdfPath);
        //            }
        //            catch
        //            {
        //                // Log or suppress cleanup error so it doesn't break the return flow
        //            }
        //        }
        //    }
        //}
    }
}
