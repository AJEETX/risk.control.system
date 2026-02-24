using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Services.Api;
using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

namespace risk.control.system.Controllers.Api.Company
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class InvestigationController : ControllerBase
    {
        private readonly IInvestigationService service;
        private readonly ILogger<InvestigationController> logger;

        public InvestigationController(IInvestigationService service, ILogger<InvestigationController> logger)
        {
            this.service = service;
            this.logger = logger;
        }

        [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
        [HttpGet("GetAuto")]
        public async Task<IActionResult> GetAuto(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                var response = await service.GetAuto(userEmail, draw, start, length, search, caseType, orderColumn, orderDir);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting draft cases for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
        [HttpGet("GetActive")]
        public async Task<IActionResult> GetActive(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                var response = await service.GetActive(userEmail, draw, start, length, search, caseType, orderColumn, orderDir);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting active cases for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetFilesData/{uploadId?}")]
        public async Task<IActionResult> GetFilesData(int draw, int start, int length, int orderColumn, string orderDir, int uploadId = 0, string searchTerm = null)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                var isManager = HttpContext.User.IsInRole(MANAGER.DISPLAY_NAME);
                var result = await service.GetFilesData(userEmail, isManager, draw, start, length, orderColumn, orderDir, uploadId, searchTerm);
                return Ok(new
                {
                    draw = result.Draw,
                    recordsTotal = result.RecordsTotal,
                    recordsFiltered = result.RecordsFiltered,
                    maxAssignReadyAllowed = result.MaxAssignReadyAllowed,
                    data = result.Data,
                    IsManager = isManager
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting uploaded cases for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetFileById/{uploadId}")]
        public async Task<IActionResult> GetFileById(int uploadId)
        {
            if (uploadId <= 0)
            {
                return BadRequest("Invalid uploadId parameter.");
            }
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                var isManager = HttpContext.User.IsInRole(MANAGER.DISPLAY_NAME);
                var result = await service.GetFileById(userEmail, isManager, uploadId);

                return Ok(new
                {
                    data = result.Item1,
                    maxAssignReadyAllowed = result.Item2
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting uploaded case by {UploadId} for user {UserEmail}", uploadId, userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}