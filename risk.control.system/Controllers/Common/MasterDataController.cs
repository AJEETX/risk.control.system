using System.Globalization;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
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
    public class MasterDataController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IPhoneService phoneService;
        private readonly IFeatureManager featureManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ILogger<MasterDataController> logger;

        public MasterDataController(
            ApplicationDbContext context,
            IPhoneService phoneService,
            IFeatureManager featureManager,
            UserManager<ApplicationUser> userManager,
            ILogger<MasterDataController> logger)
        {
            this.context = context;
            this.phoneService = phoneService;
            this.featureManager = featureManager;
            this.userManager = userManager;
            this.logger = logger;
        }

        [HttpGet("CheckAgencyName")]
        public async Task<int?> CheckAgencyName(string input, string domain)
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(domain))
            {
                return null;
            }
            Domain domainData = (Domain)Enum.Parse(typeof(Domain), domain, true);

            var newDomain = input.Trim().ToLower(CultureInfo.InvariantCulture) + domainData.GetEnumDisplayName();

            var agenccompanyCount = await context.ClientCompany.AsNoTracking().CountAsync(u => u.Email.Trim().ToLower() == newDomain && !u.Deleted);
            var agencyCount = await context.Vendor.AsNoTracking().CountAsync(u => u.Email.Trim().ToLower() == newDomain);

            return agencyCount == 0 && agenccompanyCount == 0 ? 0 : 1;
        }

        [HttpGet("CheckUserEmail")]
        public async Task<int?> CheckUserEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var userCount = await userManager.Users.AsNoTracking().CountAsync(u => u.Email == input.ToLower());

            return userCount == 0 ? 0 : 1;
        }

        [HttpGet("GetInvestigationServicesByInsuranceType")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetInvestigationServicesByInsuranceType(string insuranceType)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                InsuranceType type;
                var services = new List<InvestigationServiceType>();
                if (!string.IsNullOrWhiteSpace(insuranceType) && Enum.TryParse(insuranceType, out type))
                {
                    services = await context.InvestigationServiceType.AsNoTracking().Where(s => s.InsuranceType == type).ToListAsync();
                }
                return Ok(services);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting investigation types for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetUserBySearch")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserBySearch(string search = "")
        {
            try
            {
                var vendorAgentIds = await context.Set<ApplicationUser>()
                .Where(v => v.Role == AppRoles.AGENT)
                .Select(v => v.Id)
                .ToListAsync();

                IQueryable<ApplicationUser> query = context.ApplicationUser.AsNoTracking()
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
                logger.LogError(ex, "Error occurred while getting users");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("SearchCountry")]
        public IActionResult SearchCountry(string term = "")
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var allCountries = context.Country.AsNoTracking().ToList();

                if (string.IsNullOrEmpty(term))
                    return Ok(allCountries
                        .OrderBy(x => x.Name)
                     .Take(10)
                     .Select(c => new
                     {
                         Id = c.CountryId,
                         Name = c.Name,
                         Label = c.Name
                     })?
                        .ToList());

                var countries = allCountries
                        .Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x => x.Name)
                     .Take(10)
                        .Select(c => new
                        {
                            Id = c.CountryId,
                            Name = c.Name,
                            Label = c.Name
                        })?
                        .ToList();
                return Ok(countries);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting countries for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("SearchState")]
        public IActionResult SearchState(long countryId, string term = "")
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                if (string.IsNullOrEmpty(term?.Trim()))
                    return Ok(context.State.AsNoTracking().Where(x => x.CountryId == countryId)?
                        .OrderBy(x => x.Name)
                     .Take(10)
                     .Select(x => new { StateId = x.StateId, StateName = x.Name })?.ToList());

                var states = context.State.AsNoTracking().Where(x => x.CountryId == countryId && x.Name.ToLower().Contains(term.ToLower()))
                        .OrderBy(x => x.Name)
                     .Take(10)
                        .Select(c => new
                        {
                            StateId = c.StateId,
                            StateName = c.Name
                        })?
                        .ToList();
                return Ok(states);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting states for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("SearchDistrict")]
        public IActionResult SearchDistrict(long stateId, long countryId, string term = "")
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var districts = string.IsNullOrEmpty(term?.Trim())
                ? context.District.AsNoTracking()
                    .Where(x => x.CountryId == countryId && x.StateId == stateId)
                    .OrderBy(x => x.Name)
                    .Take(10)
                    .Select(x => new
                    {
                        DistrictId = x.DistrictId,
                        DistrictName = $"{x.Name}"
                    })
                    .ToList()
                : context.District.AsNoTracking()
                    .Where(x => x.CountryId == countryId && x.StateId == stateId && x.Name.ToLower().Contains(term.ToLower()))
                    .OrderBy(x => x.Name)
                    .Take(10)
                    .Select(x => new
                    {
                        DistrictId = x.DistrictId,
                        DistrictName = $"{x.Name}"
                    })
                    .ToList();

                // Add the "ALL DISTRICTS" option to the response
                var result = new List<object>
            {
                new
                {
                    DistrictId = -1, // Special value for "ALL DISTRICTS"
                    DistrictName = Applicationsettings.ALL_DISTRICT
                }
            };

                // Append the queried districts to the result
                result.AddRange(districts);

                // Return the final response
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting districts for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetCountryName")]
        public IActionResult GetCountryName(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var country = context.Country.AsNoTracking().Where(x => x.CountryId == id).OrderBy(x => x.Name).Take(10) // Filter based on user input
                    .Select(x => new { Id = x.CountryId, Name = $"{x.Name}" }).FirstOrDefault(); // Format for jQuery UI Autocomplete

                return Ok(country);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting countries for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetStateName")]
        public IActionResult GetStateName(long id, long countryId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var state = context.State.AsNoTracking().Where(x => x.StateId == id && x.CountryId == countryId).OrderBy(x => x.Name).Take(10) // Filter based on user input
                    .Select(x => new { StateId = x.StateId, StateName = $"{x.Name}" }).FirstOrDefault(); // Format for jQuery UI Autocomplete

                return Ok(state);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting states for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetStateNameForCountry")]
        public IActionResult GetStateNameForCountry(long countryId, long? id = null)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var states = context.State.AsNoTracking()
                    .Where(x => x.CountryId == countryId)
                    .OrderBy(x => x.Name)
                    .Select(x => new { StateId = x.StateId, StateName = x.Name })
                    .ToList();

                if (id.HasValue)
                {
                    // Return the state with the specific id if needed for pre-filling
                    var state = states.FirstOrDefault(x => x.StateId == id);
                    return Ok(state);
                }

                return Ok(states); // Return all states if no id is specified
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting states for country for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetDistrictName")]
        public IActionResult GetDistrictName(long id, long stateId, long countryId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                if (id == -1)
                {
                    var result = new
                    {
                        DistrictId = -1, // Special value for "ALL DISTRICTS"
                        DistrictName = Applicationsettings.ALL_DISTRICT
                    };
                    return Ok(result);
                }
                var pincode = context.District.AsNoTracking().Where(x => x.DistrictId == id && x.StateId == stateId && x.CountryId == countryId).OrderBy(x => x.Name).Take(10) // Filter based on user input
                    .Select(x => new { DistrictId = x.DistrictId, DistrictName = $"{x.Name}" }).FirstOrDefault(); // Format for jQuery UI Autocomplete

                return Ok(pincode);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting districts for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetDistrictNameForAgency")]
        public IActionResult GetDistrictNameForAgency(long id, long stateId, long countryId, long lob, long serviceId, long vendorId)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var districts = context.District.AsNoTracking().Where(x => x.StateId == stateId && x.CountryId == countryId).OrderBy(x => x.Name)//.Take(10) // Filter based on user input
                                .Select(x => new { DistrictId = x.DistrictId, DistrictName = $"{x.Name}" }).ToList(); // Format for jQuery UI Autocomplete

                var result = new List<object>
            {
                new {
                    DistrictId = -1,
                    DistrictName = Applicationsettings.ALL_DISTRICT
                }
            };

                result.AddRange(districts);

                return Ok(districts);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting districts for agency for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetPincode")]
        public IActionResult GetPincode(long id, long countryId)
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var pincode = context.PinCode.AsNoTracking().FirstOrDefault(x => x.PinCodeId == id && x.CountryId == countryId); // Format for jQuery UI Autocomplete

                var response = new
                {
                    DistrictId = pincode.DistrictId,
                    StateId = pincode.StateId,
                    PincodeName = $"{pincode.Name} - {pincode.Code}",
                    PincodeId = pincode.PinCodeId
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting investigations for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetPincodeSuggestions")]
        public IActionResult GetPincodeSuggestions(long countryId, string term = "")
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                // Check if the term is empty or null
                if (string.IsNullOrEmpty(term?.Trim()))
                {
                    // If no search term, return all pincodes for the given district, state, and country
                    var allpincodes = context.PinCode.AsNoTracking()
                        .Include(x => x.State)
                        .Include(x => x.District)
                        .Where(x => x.CountryId == countryId)
                        .OrderBy(x => x.Name)
                     .Take(10)
                        .Select(x => new
                        {
                            PincodeId = x.PinCodeId,
                            Pincode = x.Code,
                            Name = x.Name,
                            StateId = x.StateId,
                            StateName = x.State.Name,
                            DistricId = x.DistrictId,
                            DistrictName = x.District.Name
                        })?
                        .ToList();
                    return Ok(allpincodes);
                }

                // Sanitize the term by trimming spaces
                // Sanitize the term by trimming spaces
                var sanitizedTerm = term.Trim();

                // Split the term by hyphen, handle both parts (name and pincode)
                var termParts = sanitizedTerm.Split('-').Select(part => part.Trim()).ToArray();

                // Name filter: The part before the hyphen (if exists)
                var nameFilter = termParts.Length > 0 ? termParts[0] : string.Empty;

                // Pincode filter: The part after the hyphen (if exists)
                var pincodeFilter = termParts.Length > 1 ? termParts[1] : string.Empty;

                var pincodesQuery = context.PinCode.AsNoTracking().Where(x => x.CountryId == countryId);

                if (!string.IsNullOrWhiteSpace(nameFilter))
                {
                    // Search pincodes that match either name or pincode
                    pincodesQuery = context.PinCode.AsNoTracking()
                        .Where(x => x.CountryId == countryId &&
                        (x.Name.ToLower().Contains(nameFilter.ToLower()) ||
                        x.Code.ToString().Contains(nameFilter.ToLower()))
                        );
                }
                else
                {
                    // Search pincodes that match either name or pincode
                    pincodesQuery = context.PinCode.AsNoTracking()
                        .Where(x => x.CountryId == countryId &&
                        x.Code.ToString().Contains(pincodeFilter.ToLower())
                        );
                }

                // Get the filtered and sorted results
                var filteredPincodes = pincodesQuery
                    .Include(x => x.State)
                    .Include(x => x.District)
                     .Take(10)
                    .OrderBy(x => x.Name)
                        .Select(x => new
                        {
                            PincodeId = x.PinCodeId,
                            Pincode = x.Code,
                            Name = x.Name,
                            StateId = x.StateId,
                            StateName = x.State.Name,
                            DistrictId = x.DistrictId,
                            DistrictName = x.District.Name
                        })?
                    .ToList();

                // Return the filtered pincodes
                return Ok(filteredPincodes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting pincodes for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetCountrySuggestions")]
        public IActionResult GetCountrySuggestions(string term = "")
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var allCountries = context.Country.AsNoTracking().ToList();

                if (string.IsNullOrEmpty(term))
                    return Ok(allCountries
                        .OrderBy(x => x.Name)
                     .Take(10)
                     .Select(c => new
                     {
                         Id = c.CountryId,
                         Name = c.Name,
                         Label = c.Name
                     })?
                        .ToList());

                var countries = allCountries
                        .Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x => x.Name)
                     .Take(10)
                        .Select(c => new
                        {
                            Id = c.CountryId,
                            Name = c.Name,
                            Label = c.Name
                        })?
                        .ToList();
                return Ok(countries);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting country for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [AllowAnonymous]
        [HttpGet("GetCountryIsdCode")]
        public IActionResult GetCountryIsdCode(string term = "")
        {
            try
            {
                var allCountries = context.Country.AsNoTracking().ToList();

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
                logger.LogError(ex, "Error occurred while getting isd code");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [AllowAnonymous]
        [HttpGet("GetIsdCode")]
        public IActionResult GetIsdCode(string term = "")
        {
            try
            {
                var allCountries = context.Country.AsNoTracking().ToList();

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
                logger.LogError(ex, "Error occurred while getting isd code");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("ValidatePhone")]
        public async Task<IActionResult> ValidatePhone(string phone, int countryCode)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                if (string.IsNullOrWhiteSpace(phone))
                    return Ok(new { valid = false, message = "Mobile number is required." });
                if (await featureManager.IsEnabledAsync(FeatureFlags.VALIDATE_PHONE))
                {
                    var country = await context.Country.AsNoTracking().FirstOrDefaultAsync(c => c.ISDCode == countryCode);

                    var phoneInfo = await phoneService.ValidateAsync(country.ISDCode.ToString() + phone);

                    if (phoneInfo == null || !phoneInfo.IsValidNumber || phoneInfo.CountryCode != country.ISDCode.ToString() || phoneInfo.PhoneNumberRegion.ToLower() != country.Code.ToLower() || phoneInfo.NumberType.ToLower() != "mobile")
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
                return Ok(new
                {
                    valid = true,
                    message = "Valid mobile number"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while validating mobile for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("IsValidMobileNumber")]
        public async Task<IActionResult> IsValidMobileNumber(string phone, int countryCode)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                if (string.IsNullOrWhiteSpace(phone))
                    return Ok(new { valid = false, message = "Mobile number is required." });
                var country = await context.Country.AsNoTracking().FirstOrDefaultAsync(c => c.ISDCode == countryCode);

                var isMobile = phoneService.IsValidMobileNumber(phone, country.ISDCode.ToString());

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
                logger.LogError(ex, "Error occurred while checking mobile for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("bsb")]
        public IActionResult GetBSBDetails(string code)
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var bsbDetail = context.BsbInfo.AsNoTracking().FirstOrDefault(b => b.BSB == code);
                return Ok(bsbDetail);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting bsb details for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}