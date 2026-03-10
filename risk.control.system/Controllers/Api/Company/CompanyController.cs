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

        [HttpGet("Users")]
        public async Task<IActionResult> Users()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

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

        [HttpGet("GetAllEmpanelledAgencies")]
        public async Task<IActionResult> GetAllEmpanelledAgencies()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

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

        [HttpGet("GetEmpanelledAgency/{id}")]
        public async Task<IActionResult> GetEmpanelledAgency(long id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid case ID.");
            }
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                var result = await agencyService.GetEmpanelledAgency(userEmail, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting empanedlled agencies for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("AvailableAgency")]
        public async Task<IActionResult> AvailableAgency()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

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