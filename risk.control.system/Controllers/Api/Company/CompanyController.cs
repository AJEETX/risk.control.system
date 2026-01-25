using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;
using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Controllers.Api.Company
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompanyController> logger;
        private readonly IUserService userService;
        private readonly IFeatureManager featureManager;
        private readonly IPhoneService phoneService;
        private readonly IVendorService vendorService;
        private readonly ICompanyService companyService;

        public CompanyController(ApplicationDbContext context,
            ILogger<CompanyController> logger,
            IUserService userService,
            IFeatureManager featureManager,
            IPhoneService phoneService,
            IVendorService vendorService,
            ICompanyService companyService
            )
        {
            _context = context;
            this.logger = logger;
            this.userService = userService;
            this.featureManager = featureManager;
            this.phoneService = phoneService;
            this.vendorService = vendorService;
            this.companyService = companyService;
        }

        [HttpGet("AllCompanies")]
        public async Task<IActionResult> AllCompanies()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var result = await companyService.GetCompanies();
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting companies for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("CompanyUsers")]
        public async Task<IActionResult> CompanyUsers(long id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid company ID.");
            }
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var result = await userService.GetCompanyUsers(userEmail, id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting company users for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
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
                var result = await userService.GetCompanyUsers(userEmail);

                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting company users for user {UserEmail}", userEmail ?? "Anonymous");
                return null;
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
                var companyUser = await _context.ApplicationUser
                    .FirstOrDefaultAsync(c => c.Email == userEmail);
                if (companyUser == null)
                {
                    return NotFound("Company user not found.");
                }
                var vendors = await vendorService.GetEmpanelledVendorsAsync(companyUser);

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
                var companyUser = await _context.ApplicationUser
                    .FirstOrDefaultAsync(c => c.Email == userEmail);
                if (companyUser == null)
                {
                    return NotFound("Company user not found.");
                }

                var result = await vendorService.GetEmpanelledAgency(companyUser, caseId);
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
                var result = await vendorService.GetAvailableVendors(userEmail);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting available agencies for user {UserEmail}", userEmail ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("AllServices")]
        public async Task<IActionResult> AllServices(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var result = await vendorService.GetAgencyService(id);
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