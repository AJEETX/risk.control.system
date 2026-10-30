using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;
using risk.control.system.Services.Api;
using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

namespace risk.control.system.Controllers.Api.Agency
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/agency/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class VendorInvestigationController(ILogger<VendorInvestigationController> logger,
            IAgencyInvestigationService agencyInvestigationService) : ControllerBase
    {
        private readonly ILogger<VendorInvestigationController> _logger = logger;
        private readonly IAgencyInvestigationService _agencyInvestigationService = agencyInvestigationService;

        [HttpGet("GetNewCases")]
        public async Task<IActionResult> GetNewCases(int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var response = await _agencyInvestigationService.GetNewCases(userEmail, draw, start, length, search, orderColumn, orderDir);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting new cases for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetOpenCases")]
        public async Task<IActionResult> GetOpenCases(int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var response = await _agencyInvestigationService.GetOpenCases(userEmail, draw, start, length, search, orderColumn, orderDir);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting open cases for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetAgentReports")]
        public async Task<IActionResult> GetAgentReports(int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var response = await _agencyInvestigationService.GetAgentReports(userEmail, draw, start, length, search, orderColumn, orderDir);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting reports for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetCompleted")]
        public async Task<IActionResult> GetCompleted(int draw, int start, int length, string search = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var response = await _agencyInvestigationService.GetCompletedCases(userEmail, userClaim!, draw, start, length, search, orderColumn, orderDir);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting completed cases for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}