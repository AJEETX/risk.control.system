using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Controllers.Mobile
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ToolsController : ControllerBase
    {
        private readonly IMemoryCache cache;
        private readonly IFaceMatchService faceMatchService;
        private readonly IFileStorageService fileStorageService;
        private readonly IGoogleService googleApi;

        public ToolsController(
            IMemoryCache cache,
            IFaceMatchService faceMatchService,
            IFileStorageService fileStorageService,
            IGoogleService googleApi)
        {
            this.cache = cache;
            this.faceMatchService = faceMatchService;
            this.fileStorageService = fileStorageService;
            this.googleApi = googleApi;
        }

        //[AllowAnonymous]
        [HttpPost("face-match")]
        public async Task<IActionResult> FaceMatch(FaceMatchData data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var originalFace = await AgentVerificationHelper.GetBytesFromIFormFile(data.OriginalFaceImage);
            var secondayFace = await AgentVerificationHelper.GetBytesFromIFormFile(data.MatchFaceImage);
            var (file, path) = await fileStorageService.SaveAsync(data.OriginalFaceImage, "tool");
            var faceMatchData = await faceMatchService.GetFaceMatchAsync(originalFace, secondayFace, Path.GetExtension(file));

            return Ok(faceMatchData);
        }

        //[AllowAnonymous]
        [HttpPost("ocr")]
        public async Task<IActionResult> OcrDocument(DocumentOcrData data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var (file, path) = await fileStorageService.SaveAsync(data.DocumentImage, "tool");
            var ocrData = await googleApi.DetectTextAsync(path);
            if (ocrData == null || ocrData.Count == 0)
            {
                return BadRequest("Ocr failed");
            }
            var ocrDetail = ocrData.FirstOrDefault();
            var description = ocrDetail != null ? ocrDetail.Description : string.Empty;
            return Ok(description);
        }
    }
}
