using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Common
{
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DashboardGraphController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardGraphController> _logger;

        public DashboardGraphController(IDashboardService dashboardService,
            ILogger<DashboardGraphController> logger
            )
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        public async Task<JsonResult> GetAgentClaim()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (string.IsNullOrEmpty(userRole))
                {
                    _logger.LogWarning("User {UserEmail} has no role claim.", userEmail);
                    return new JsonResult(null);
                }
                _logger.LogInformation("Fetching agent claim data for user {UserEmail} with role {UserRole}", userEmail, userRole);

                Dictionary<string, int> monthlyExpense = null;

                if (userRole.Contains(MANAGER.DISPLAY_NAME) ||
                    userRole.Contains(ASSESSOR.DISPLAY_NAME) ||
                    userRole.Contains(COMPANY_ADMIN.DISPLAY_NAME) ||
                    userRole.Contains(CREATOR.DISPLAY_NAME))
                {
                    _logger.LogInformation("User {UserEmail} is Manager/Assessor/CompanyAdmin/Creator. Calculating agency claim status.", userEmail);
                    monthlyExpense = await _dashboardService.CalculateAgencyClaimStatus(userEmail);
                }
                else if (userRole.Contains(AGENCY_ADMIN.DISPLAY_NAME) || userRole.Contains(SUPERVISOR.DISPLAY_NAME))
                {
                    _logger.LogInformation("User {UserEmail} is AgencyAdmin/Supervisor. Calculating agent case status.", userEmail);
                    monthlyExpense = await _dashboardService.CalculateAgentCaseStatus(userEmail);
                }
                else
                {
                    _logger.LogWarning("User {UserEmail} with role {UserRole} is not authorized to fetch agent claim data.", userEmail, userRole);
                }

                return new JsonResult(monthlyExpense);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching agent claim data for user {UserEmail}.", userEmail);
                return new JsonResult(null);
            }
        }

        public async Task<JsonResult> GetAgentUnderwriting()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (string.IsNullOrEmpty(userRole))
                {
                    _logger.LogWarning("User {UserEmail} has no role claim.", userEmail);
                    return new JsonResult(null);
                }
                if (userRole.Contains(MANAGER.DISPLAY_NAME)
                                                || userRole.Contains(ASSESSOR.DISPLAY_NAME)
                                                || userRole.Contains(COMPANY_ADMIN.DISPLAY_NAME)
                                                || userRole.Contains(CREATOR.DISPLAY_NAME)
                                                )
                {
                    Dictionary<string, int> monthlyExpense = await _dashboardService.CalculateAgencyUnderwritingStatus(userEmail);
                    return new JsonResult(monthlyExpense);
                }
                else if (userRole.Contains(AGENCY_ADMIN.DISPLAY_NAME) || userRole.Contains(SUPERVISOR.DISPLAY_NAME))
                {
                    Dictionary<string, int> monthlyExpense = await _dashboardService.CalculateAgentCaseStatus(userEmail);
                    return new JsonResult(monthlyExpense);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching agent underwriting data for user {UserEmail}.", userEmail);
                return new JsonResult(null);
            }
            return new JsonResult(null);
        }

        public async Task<JsonResult> GetMonthlyClaim()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var monthlyExpense = await _dashboardService.CalculateMonthlyCaseStatus(userEmail);
                return new JsonResult(monthlyExpense);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching monthly claim data for user {UserEmail}.", userEmail);
                return new JsonResult(null);
            }
        }

        public async Task<JsonResult> GetWeeklyClaim()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var monthlyExpense = await _dashboardService.CalculateWeeklyCaseStatus(userEmail);
                return new JsonResult(monthlyExpense);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching weekly claim data for user {UserEmail}.", userEmail);
                return new JsonResult(null);
            }
        }

        public async Task<JsonResult> GetWeeklyPieClaim()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var monthlyExpense = await _dashboardService.CalculateWeeklyCaseStatusPieClaims(userEmail);
                return new JsonResult(monthlyExpense);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching weekly Claim Pie data for user {UserEmail}.", userEmail);
                return new JsonResult(null);
            }
        }

        public async Task<JsonResult> GetWeeklyPieUnderwriting()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var monthlyExpense = await _dashboardService.CalculateWeeklyCaseStatusPieUnderwritings(userEmail);
                return new JsonResult(monthlyExpense);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching weekly Underwriting Pie data for user {UserEmail}.", userEmail);
                return new JsonResult(null);
            }
        }

        public async Task<JsonResult> GetClaimChart()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var monthlyExpense = await _dashboardService.CalculateCaseChart(userEmail);
                return new JsonResult(monthlyExpense);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching Claim Chart data for user {UserEmail}.", userEmail);
                return new JsonResult(null);
            }
        }

        public async Task<JsonResult> GetClaimWeeklyTat()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var monthlyExpense = await _dashboardService.CalculateTimespan(userEmail);
                return new JsonResult(monthlyExpense);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching Claim Weely Tat data for user {UserEmail}.", userEmail);
                return new JsonResult(null);
            }
        }
    }
}