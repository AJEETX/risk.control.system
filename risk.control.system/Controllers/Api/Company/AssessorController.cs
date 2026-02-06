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
    public class AssessorController : ControllerBase
    {
        private readonly IAssessorService assesorService;
        private readonly ILogger<AssessorController> logger;

        public AssessorController(IAssessorService assesorService, ILogger<AssessorController> logger)
        {
            this.assesorService = assesorService;
            this.logger = logger;
        }

        [HttpGet("GetInvestigations")]
        public async Task<IActionResult> GetInvestigations(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await assesorService.GetInvestigationReports(userEmail, draw, start, length, search, caseType, orderColumn, orderDir);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting investigations for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetReviewCases")]
        public async Task<IActionResult> GetReviewCases(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await assesorService.GetReviews(userEmail, draw, start, length, search, caseType, orderColumn, orderDir);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting review for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetApprovededCases")]
        public async Task<IActionResult> GetApprovededCases(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await assesorService.GetApprovededCases(userEmail, draw, start, length, search, caseType, orderColumn, orderDir);
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting approved cases for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetRejectedCases")]
        public async Task<IActionResult> GetRejectedCases(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await assesorService.GetRejectedCases(userEmail, draw, start, length, search, caseType, orderColumn, orderDir);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting rejected cases for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}