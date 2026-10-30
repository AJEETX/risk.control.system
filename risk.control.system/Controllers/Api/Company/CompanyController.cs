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
    public class CompanyController(ILogger<CompanyController> logger,
            ICompanyUserApiService companyUserApiService,
            IAgencyApiService agencyService
            ) : ControllerBase
    {
        private readonly ILogger<CompanyController> _logger = logger;
        private readonly ICompanyUserApiService _companyUserApiService = companyUserApiService;
        private readonly IAgencyApiService _agencyService = agencyService;

        [HttpGet("Users")]
        public async Task<IActionResult> Users()
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var result = await _companyUserApiService.GetCompanyUsers(userEmail);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company users for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetAllEmpanelledAgencies")]
        public async Task<IActionResult> GetAllEmpanelledAgencies()
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var vendors = await _agencyService.GetAllEmpanelledAgenciesAsync(userEmail);

                return Ok(vendors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting empanedlled agencies for user {UserEmail}", userEmail ?? "Anonymous");
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
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var result = await _agencyService.GetEmpanelledAgency(userEmail, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting empanedlled agencies for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetEmpanelledAgencyNew/{id}")]
        public async Task<IActionResult> GetEmpanelledAgencyNew(long id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid case ID.");
            }
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var result = await _agencyService.GetEmpanelledAgencyNew(userEmail, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting empanedlled agencies for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("AvailableAgency")]
        public async Task<IActionResult> AvailableAgency()
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var result = await _agencyService.GetAvailableAgencies(userEmail);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available agencies for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("AllServices/{id}")]
        public async Task<IActionResult> AllServices(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var result = await _agencyService.GetAgencyService(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agency services for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}