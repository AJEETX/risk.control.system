using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Services.Agentic;
using risk.control.system.Services.Common;
using risk.control.system.Services.Tool;

namespace risk.control.system.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class AgenticController(IGoogleService googleService, IFileStorageService fileStorageService, IAgenticService agenticService) : ControllerBase
    {
        private readonly IGoogleService _googleService = googleService;
        private readonly IFileStorageService _fileStorageService = fileStorageService;
        private readonly IAgenticService _agenticService = agenticService;

        //Ocr Endpoint
        [HttpPost("Ocr")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        public async Task<IActionResult> OcrDocument(IFormFile image)
        {
            try
            {
                var (file, path) = await _fileStorageService.SaveAsync(image, "tool");
                var ocrTextData = await _googleService.DetectTextAsync(path);
                if (string.IsNullOrWhiteSpace(ocrTextData))
                {
                    return Ok(new
                    {
                        Success = false,
                        Message = "No text found in the image.",
                        Data = ocrTextData
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "OCR completed successfully.",
                    Data = ocrTextData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = ex.Message });
            }
        }
        //Face Existence Endpoint
        [HttpPost("FaceExists")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        public async Task<IActionResult> FaceExists(IFormFile image)
        {
            try
            {
                var faceExists = await _agenticService.FaceExistsAsync(image);
                if (faceExists.Item1)
                {
                    return Ok(new
                    {
                        Success = false,
                        Message = faceExists.Item2,
                    });
                }
                return Ok(new
                {
                    Success = true,
                    Message = faceExists.Item2,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, Message = ex.Message });
            }
        }
    }
}