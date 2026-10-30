using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;
using risk.control.system.Services.Api;

namespace risk.control.system.Controllers.Api.Agency
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class AgencyController(
            ILogger<AgencyController> logger,
            IAgencyUserApiService agencyUserApiService,
            IAgencyApiService agencyService) : ControllerBase
    {
        private readonly ILogger<AgencyController> _logger = logger;
        private readonly IAgencyUserApiService _agencyUserApiService = agencyUserApiService;
        private readonly IAgencyApiService _agencyService = agencyService;

        [HttpGet("AllAgencies")]
        public async Task<IActionResult> AllAgencies()
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var response = await _agencyService.AllAgencies();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting agencies for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("AllServices")]
        public async Task<IActionResult> AllServices()
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var response = await _agencyService.AllServices(userEmail);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting agency service for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetCompanyAgencyUser/{id}")]
        public async Task<IActionResult> GetCompanyAgencyUser(long id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid agency id.");
            }
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var result = await _agencyUserApiService.GetCompanyAgencyUsers(userEmail, id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting agency users for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers()
        {
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var agentWithLoad = await _agencyUserApiService.GetAgencyUsers(userEmail);
                return Ok(agentWithLoad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting agency users for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetAgentWithCases/{id}")]
        public async Task<IActionResult> GetAgentWithCases(long id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid agent id.");
            }
            var userEmail = HttpContext.User?.Identity?.Name!;

            try
            {
                var agentList = await _agencyService.GetAgentWithCases(userEmail, id);
                return Ok(agentList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting agency users for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}