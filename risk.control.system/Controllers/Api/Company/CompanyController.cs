using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Services.Api;

namespace risk.control.system.Controllers.Api.Company
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ILogger<CompanyController> logger;
        private readonly ICompanyUserApiService companyUserApiService;
        private readonly IAgencyService agencyService;

        public CompanyController(ILogger<CompanyController> logger,
            ICompanyUserApiService companyUserApiService,
            IAgencyService agencyService
            )
        {
            this.logger = logger;
            this.companyUserApiService = companyUserApiService;
            this.agencyService = agencyService;
        }

        [HttpGet("AllUsers")]
        public async Task<IActionResult> AllUsers()
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var result = await companyUserApiService.GetCompanyUsers(userEmail);

                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting company users for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetEmpanelledVendors")]
        public async Task<IActionResult> GetEmpanelledVendors()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var vendors = await agencyService.GetAllEmpanelledAgenciesAsync(userEmail);

                return Ok(vendors);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting empanedlled agencies for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetEmpanelledAgency")]
        public async Task<IActionResult> GetEmpanelledAgency(long caseId)
        {
            if (caseId <= 0)
            {
                return BadRequest("Invalid case ID.");
            }
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var result = await agencyService.GetEmpanelledAgency(userEmail, caseId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting empanedlled agencies for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetAvailableVendors")]
        public async Task<IActionResult> GetAvailableVendors()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var result = await agencyService.GetAvailableAgencies(userEmail);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting available agencies for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("AllServices/{id}")]
        public async Task<IActionResult> AllServices(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var result = await agencyService.GetAgencyService(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting agency services for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}