using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

namespace risk.control.system.Controllers.Api.Company
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = MANAGER.DISPLAY_NAME)]
    public class ManagerController : ControllerBase
    {
        private readonly ILogger<ManagerController> logger;
        private readonly IManagerService managerService;

        public ManagerController(ILogger<ManagerController> logger,
            IManagerService managerService)
        {
            this.logger = logger;
            this.managerService = managerService;
        }

        [HttpGet("GetActiveCases")]
        public async Task<IActionResult> GetActiveCases(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await managerService.GetActiveCases(userEmail, draw, start, length, search, caseType, orderColumn, orderDir);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting active cases for user {UserEmail}", userEmail);
                return null;
            }
        }
        [HttpGet("GetApprovedCases")]
        public async Task<IActionResult> GetApprovedCases()
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await managerService.GetApprovedCases(userEmail);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting approved cases for user {UserEmail}", userEmail);
                return null;
            }
        }

        [HttpGet("GetRejectedCases")]
        public async Task<IActionResult> GetRejectedCases()
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await managerService.GetRejectedCases(userEmail);
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting rejected cases for user {UserEmail}", userEmail);
                return null;
            }
        }
    }
}