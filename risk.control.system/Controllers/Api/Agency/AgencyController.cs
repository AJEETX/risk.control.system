using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Api.Agency
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class AgencyController : ControllerBase
    {
        private readonly ILogger<AgencyController> logger;
        private readonly IUserService userService;
        private readonly IVendorService vendorService;

        public AgencyController(
            ILogger<AgencyController> logger,
            IUserService userService,
            IVendorService vendorService)
        {
            this.logger = logger;
            this.userService = userService;
            this.vendorService = vendorService;
        }

        [HttpGet("AllAgencies")]
        public async Task<IActionResult> AllAgencies()
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await vendorService.AllAgencies();

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting agencies for user {UserEmail}", userEmail);
                return null;
            }
        }

        [HttpGet("AllServices")]
        public async Task<IActionResult> AllServices()
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await vendorService.AllServices(userEmail);
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting agency service for user {UserEmail}", userEmail);
                return null;
            }
        }

        [HttpGet("GetCompanyAgencyUser")]
        public async Task<IActionResult> GetCompanyAgencyUser(long id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid agency id.");
            }
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var result = await userService.GetCompanyAgencyUsers(userEmail, id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting agency users for user {UserEmail}", userEmail);
                return null;
            }
        }

        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers()
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var agentWithLoad = await userService.GetAgencyUsers(userEmail);
                return Ok(agentWithLoad);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting agency users for user {UserEmail}", userEmail);
                return null;
            }
        }


        [HttpGet("GetAgentWithCases")]
        public async Task<IActionResult> GetAgentWithCases(long id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid agent id.");
            }
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var agentList = await vendorService.GetAgentWithCases(userEmail, id);
                return Ok(agentList);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting agency users for user {UserEmail}", userEmail);
                return null;
            }
        }
    }
}