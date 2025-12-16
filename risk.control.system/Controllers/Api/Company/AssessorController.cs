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
        public async Task<IActionResult> GetInvestigations()
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await assesorService.GetInvestigations(userEmail);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting investigations for user {UserEmail}", userEmail);
                return null;
            }

        }

        [HttpGet("GetReviewCases")]
        public async Task<IActionResult> GetReviewCases()
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await assesorService.GetReview(userEmail);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting review for user {UserEmail}", userEmail);
                return null;
            }

        }

        [HttpGet("GetApprovededCases")]
        public async Task<IActionResult> GetApprovededCases()
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var response = await assesorService.GetApprovededCases(userEmail);
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
                var response = await assesorService.GetRejectedCases(userEmail);

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