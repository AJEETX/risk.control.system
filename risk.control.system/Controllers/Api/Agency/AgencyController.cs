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
    public class AgencyController : ControllerBase
    {
        private readonly ILogger<AgencyController> logger;
        private readonly IAgencyUserApiService agencyUserApiService;
        private readonly IAgencyService agencyService;

        public AgencyController(
            ILogger<AgencyController> logger,
            IAgencyUserApiService agencyUserApiService,
            IAgencyService agencyService)
        {
            this.logger = logger;
            this.agencyUserApiService = agencyUserApiService;
            this.agencyService = agencyService;
        }

        [HttpGet("AllAgencies")]
        public async Task<IActionResult> AllAgencies()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await agencyService.AllAgencies();

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting agencies for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("AllServices")]
        public async Task<IActionResult> AllServices()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await agencyService.AllServices(userEmail);
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting agency service for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetCompanyAgencyUser")]
        public async Task<IActionResult> GetCompanyAgencyUser(long id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid agency id.");
            }
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var result = await agencyUserApiService.GetCompanyAgencyUsers(userEmail, id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting agency users for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var agentWithLoad = await agencyUserApiService.GetAgencyUsers(userEmail);
                return Ok(agentWithLoad);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting agency users for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetAgentWithCases")]
        public async Task<IActionResult> GetAgentWithCases(long id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid agent id.");
            }
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var agentList = await agencyService.GetAgentWithCases(userEmail, id);
                return Ok(agentList);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting agency users for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}