using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;


namespace risk.control.system.Controllers.Api.Agency
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/agency/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class VendorInvestigationController : ControllerBase
    {
        private readonly ILogger<VendorInvestigationController> logger;
        private readonly IVendorInvestigationService vendorInvestigationService;

        public VendorInvestigationController(ILogger<VendorInvestigationController> logger,
            IVendorInvestigationService vendorInvestigationService)
        {
            this.logger = logger;
            this.vendorInvestigationService = vendorInvestigationService;
        }

        [HttpGet("GetNewCases")]
        public async Task<IActionResult> GetNewCases()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await vendorInvestigationService.GetNewCases(userEmail);
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting new cases for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        [HttpGet("GetOpenCases")]
        public async Task<IActionResult> GetOpenCases()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await vendorInvestigationService.GetOpenCases(userEmail);
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting open cases for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetReport")]
        public async Task<IActionResult> GetReport()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await vendorInvestigationService.GetReport(userEmail);
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting reports for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        [HttpGet("GetCompleted")]
        public async Task<IActionResult> GetCompleted()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail) || string.IsNullOrWhiteSpace(userClaim))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await vendorInvestigationService.GetCompleted(userEmail, userClaim);
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting completed cases for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}