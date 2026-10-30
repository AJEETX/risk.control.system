using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;
using risk.control.system.Services;

using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

namespace risk.control.system.Controllers.Api.Company
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
    public class AssessorController(IAssessorService assesorService, ILogger<AssessorController> logger) : ControllerBase
    {
        private readonly IAssessorService _assesorService = assesorService;
        private readonly ILogger<AssessorController> _logger = logger;

        [HttpGet("GetInvestigationReports")]
        public async Task<IActionResult> GetInvestigationReports(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var response = await _assesorService.GetInvestigationReports(userEmail, draw, start, length, search, caseType, orderColumn, orderDir);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting investigations for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetReviewCases")]
        public async Task<IActionResult> GetReviewCases(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var response = await _assesorService.GetReviewCases(userEmail, draw, start, length, search, caseType, orderColumn, orderDir);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting review for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetApprovededCases")]
        public async Task<IActionResult> GetApprovededCases(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var response = await _assesorService.GetApprovededCases(userEmail, draw, start, length, search, caseType, orderColumn, orderDir);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting approved cases for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetRejectedCases")]
        public async Task<IActionResult> GetRejectedCases(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var response = await _assesorService.GetRejectedCases(userEmail, draw, start, length, search, caseType, orderColumn, orderDir);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting rejected cases for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}