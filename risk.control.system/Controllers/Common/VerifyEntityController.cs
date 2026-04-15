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
            Domain domainData = Enum.Parse<Domain>(domain, true);
            var newDomain = input.Trim().ToLower(CultureInfo.InvariantCulture) + domainData.GetEnumDisplayName();
            var agencyCompanyCount = await _context.ClientCompany.AsNoTracking().CountAsync(u => u.Email.Trim().ToLower() == newDomain && !u.Deleted);
            var agencyCount = await _context.Vendor.AsNoTracking().CountAsync(u => u.Email.Trim().ToLower() == newDomain);
            return agencyCount == 0 && agencyCompanyCount == 0 ? 0 : 1;
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
                _logger.LogError(ex, "Error occurred while getting investigation types");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetUserBySearch")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserBySearch([FromQuery] string search = "")
        {
            try
            {
                var query = _context.ApplicationUser.AsNoTracking().Where(u => !u.Deleted);

                query = query.Where(u => u.Role != AppRoles.AGENT && u.Role != AppRoles.PORTAL_ADMIN && u.Role != AppRoles.GUEST && u.Role != null);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(u => u.Email != null && u.Email.ToLower().Contains(term));
                }

                var userEmails = await query
                    .OrderBy(u => u.Email)
                    .Take(10)
                    .Select(u => u.Email)
                    .ToListAsync();

                return Ok(userEmails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user emails for search: {SearchTerm}", search);
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal error occurred.");
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
            try
            {
                if (string.IsNullOrWhiteSpace(phone))
                    return Ok(new { valid = false, message = "Mobile number is required." });
                var country = await _context.Country.AsNoTracking().FirstOrDefaultAsync(c => c.ISDCode == countryCode);

                var isMobile = _phoneService.IsValidMobileNumber(phone, country!.ISDCode.ToString());

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
                _logger.LogError(ex, "Error occurred while checking mobile");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("get-bsb")]
        public IActionResult GetBSBDetails(string code)
        {
            try
            {
                var bsbDetail = _context.BsbInfo.AsNoTracking().FirstOrDefault(b => b.BSB == code);
                return Ok(bsbDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting bsb details");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}