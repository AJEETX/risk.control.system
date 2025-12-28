using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Tools
{
    [Authorize(Roles = GUEST.DISPLAY_NAME)]
    public class OcrController : Controller
    {
        private readonly IGoogleService googleService;
        private readonly IFileStorageService fileStorageService;

        public OcrController(IGoogleService googleService, IFileStorageService fileStorageService)
        {
            this.googleService = googleService;
            this.fileStorageService = fileStorageService;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> OcrDocument(DocumentOcrData data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var (file, path) = await fileStorageService.SaveAsync(data.DocumentImage, "tool");
            var ocrData = await googleService.DetectTextAsync(path);
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
