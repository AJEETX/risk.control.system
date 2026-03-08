using System.Globalization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Common
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    public class VerifyEntityController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPhoneService _phoneService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<VerifyEntityController> _logger;

        public VerifyEntityController(
            ApplicationDbContext context,
            IPhoneService phoneService,
            UserManager<ApplicationUser> userManager,
            ILogger<VerifyEntityController> logger)
        {
            _context = context;
            _phoneService = phoneService;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("GetAgencyName")]
        public async Task<int?> GetAgencyName(string input, string domain)
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(domain))
            {
                return null;
            }
            Domain domainData = (Domain)Enum.Parse(typeof(Domain), domain, true);

            var newDomain = input.Trim().ToLower(CultureInfo.InvariantCulture) + domainData.GetEnumDisplayName();

            var agenccompanyCount = await _context.ClientCompany.AsNoTracking().CountAsync(u => u.Email.Trim().ToLower() == newDomain && !u.Deleted);
            var agencyCount = await _context.Vendor.AsNoTracking().CountAsync(u => u.Email.Trim().ToLower() == newDomain);

            return agencyCount == 0 && agenccompanyCount == 0 ? 0 : 1;
        }

        [HttpGet("GetUserEmail")]
        public async Task<int?> CheckUserEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var userCount = await _userManager.Users.AsNoTracking().CountAsync(u => u.Email == input.ToLower());

            return userCount == 0 ? 0 : 1;
        }

        [HttpGet("GetInvestigationServicesByInsuranceType")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetInvestigationServicesByInsuranceType(string insuranceType)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                InsuranceType type;
                var services = new List<InvestigationServiceType>();
                if (!string.IsNullOrWhiteSpace(insuranceType) && Enum.TryParse(insuranceType, out type))
                {
                    services = await _context.InvestigationServiceType.AsNoTracking().Where(s => s.InsuranceType == type).ToListAsync();
                }
                return Ok(services);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting investigation types for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetUserBySearch")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserBySearch(string search = "")
        {
            try
            {
                var vendorAgentIds = await _context.Set<ApplicationUser>()
                .Where(v => v.Role == AppRoles.AGENT)
                .Select(v => v.Id)
                .ToListAsync();

                IQueryable<ApplicationUser> query = _context.ApplicationUser.AsNoTracking()
                    .Where(a => !a.Deleted && a.Email != PORTAL_ADMIN.EMAIL &&
                                !vendorAgentIds.Contains(a.Id));

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string loweredSearch = search.Trim();
                    query = query.Where(a => a.Email.StartsWith(loweredSearch));
                }

                var userEmails = await query
                    .OrderBy(a => a.Email)
                    .Take(10)
                    .Select(a => a.Email)
                    .ToListAsync();

                return Ok(userEmails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting users");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [AllowAnonymous]
        [HttpGet("GetCountryIsdCode")]
        public IActionResult GetCountryIsdCode(string term = "")
        {
            try
            {
                var allCountries = _context.Country.AsNoTracking().ToList();

                if (string.IsNullOrEmpty(term))
                    return Ok(allCountries
                        .OrderBy(x => x.Name)
                     //.Take(10)
                     .Select(c => new
                     {
                         IsdCode = $"+{c.ISDCode.ToString()}",
                         Flag = "/flags/" + c.Code.ToLower() + ".png",
                         CountryId = $"{c.Code.ToString()}",
                         Label = $"+{c.ISDCode.ToString()} {c.Name}"
                     })?
                        .ToList());

                var countries = allCountries
                        .Where(c => c.Name.StartsWith(term, StringComparison.OrdinalIgnoreCase) || c.ISDCode.ToString().StartsWith(term, StringComparison.OrdinalIgnoreCase) || c.Code.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x => x.Name)
                        //.Take(10)
                        .Select(c => new
                        {
                            IsdCode = $"+{c.ISDCode.ToString()}",
                            Flag = "/flags/" + c.Code.ToLower() + ".png",
                            CountryId = $"{c.Code.ToString()}",
                            Label = $"+{c.ISDCode.ToString()} {c.Name}"
                        })?
                        .ToList();
                return Ok(countries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting isd code");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [AllowAnonymous]
        [HttpGet("GetIsdCode")]
        public IActionResult GetIsdCode(string term = "")
        {
            try
            {
                var allCountries = _context.Country.AsNoTracking().ToList();

                if (string.IsNullOrEmpty(term))
                    return Ok(allCountries
                        .OrderBy(x => x.Name)
                     //.Take(10)
                     .Select(c => new
                     {
                         IsdCode = $"+{c.ISDCode.ToString()}",
                         Flag = "/flags/" + c.Code.ToLower() + ".png",
                         CountryId = $"{c.Code.ToString()}",
                         Label = $"+{c.ISDCode.ToString()} ( {c.Name} )"
                     })?
                        .ToList());

                var countries = allCountries
                        .Where(c => c.Name.StartsWith(term, StringComparison.OrdinalIgnoreCase) || c.ISDCode.ToString().StartsWith(term, StringComparison.OrdinalIgnoreCase) || c.Code.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x => x.Name)
                        //.Take(10)
                        .Select(c => new
                        {
                            IsdCode = $"+{c.ISDCode.ToString()}",
                            Flag = "/flags/" + c.Code.ToLower() + ".png",
                            CountryId = $"{c.Code.ToString()}",
                            Label = $"+{c.ISDCode.ToString()} ( {c.Name} )"
                        })?
                        .ToList();
                return Ok(countries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting isd code");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("IsMobileNumber")]
        public async Task<IActionResult> IsMobileNumber(string phone, int countryCode)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                if (string.IsNullOrWhiteSpace(phone))
                    return Ok(new { valid = false, message = "Mobile number is required." });
                var country = await _context.Country.AsNoTracking().FirstOrDefaultAsync(c => c.ISDCode == countryCode);

                var isMobile = _phoneService.IsValidMobileNumber(phone, country.ISDCode.ToString());

                if (!isMobile)
                {
                    return Ok(new
                    {
                        valid = false,
                        message = "Invalid mobile number."
                    });
                }
                return Ok(new
                {
                    valid = true,
                    message = "Valid mobile number"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking mobile for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("get-bsb")]
        public IActionResult GetBSBDetails(string code)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            try
            {
                var bsbDetail = _context.BsbInfo.AsNoTracking().FirstOrDefault(b => b.BSB == code);
                return Ok(bsbDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting bsb details for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}