using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services;

namespace risk.control.system.Controllers.Api.PortalAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    [ApiController]
    public class PortalCompanyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PortalCompanyController> logger;
        private readonly IUserService userService;
        private readonly IVendorService vendorService;
        private readonly ICompanyService companyService;

        public PortalCompanyController(ApplicationDbContext context,
            ILogger<PortalCompanyController> logger,
            IUserService userService,
            IVendorService vendorService,
            ICompanyService companyService
            )
        {
            _context = context;
            this.logger = logger;
            this.userService = userService;
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
    }
}