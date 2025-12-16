using System.Data;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

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
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
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
                logger.LogError(ex, "Error getting companies for user {UserEmail}", userEmail);
                return null;
            }
        }

        [HttpGet("CompanyUsers")]
        public async Task<IActionResult> CompanyUsers(long id)
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
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
                logger.LogError(ex, "Error getting company users for user {UserEmail}", userEmail);
                return null;
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
                logger.LogError(ex, "Error getting company users for user {UserEmail}", userEmail);
                return null;
            }
        }
        [HttpGet("GetEmpanelledVendors")]
        public async Task<IActionResult> GetEmpanelledVendors()
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var companyUser = await _context.ClientCompanyApplicationUser
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
                logger.LogError(ex, "Error getting empanedlled agencies for user {UserEmail}", userEmail);
                return null;
            }
        }
        [HttpGet("GetEmpanelledAgency")]
        public async Task<IActionResult> GetEmpanelledAgency(long caseId)
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var companyUser = await _context.ClientCompanyApplicationUser
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
                logger.LogError(ex, "Error getting empanedlled agencies for user {UserEmail}", userEmail);
                return null;
            }
        }


        [HttpGet("GetAvailableVendors")]
        public async Task<IActionResult> GetAvailableVendors()
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
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
                logger.LogError(ex, "Error getting available agencies for user {UserEmail}", userEmail);
                return null;
            }
        }

        [HttpGet("AllServices")]
        public async Task<IActionResult> AllServices(long id)
        {
            var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userClaim) || string.IsNullOrEmpty(userEmail))
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
                logger.LogError(ex, "Error getting agency services for user {UserEmail}", userEmail);
                return null;
            }
        }

        [HttpGet("SearchCountry")]
        public IActionResult SearchCountry(string term = "")
        {
            var allCountries = _context.Country.ToList();

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

        [HttpGet("SearchState")]
        public IActionResult SearchState(long countryId, string term = "")
        {
            if (string.IsNullOrEmpty(term?.Trim()))
                return Ok(_context.State.Where(x => x.CountryId == countryId)?
                    .OrderBy(x => x.Name)
                 .Take(10)
                 .Select(x => new { StateId = x.StateId, StateName = x.Name })?.ToList());

            var states = _context.State.Where(x => x.CountryId == countryId && x.Name.ToLower().Contains(term.ToLower()))
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

        [HttpGet("SearchDistrict")]
        public IActionResult SearchDistrict(long stateId, long countryId, string term = "")
        {
            // If the search term is empty or null, fetch the first 10 districts
            var districts = string.IsNullOrEmpty(term?.Trim())
                ? _context.District
                    .Where(x => x.CountryId == countryId && x.StateId == stateId)
                    .OrderBy(x => x.Name)
                    .Take(10)
                    .Select(x => new
                    {
                        DistrictId = x.DistrictId,
                        DistrictName = $"{x.Name}"
                    })
                    .ToList()
                : _context.District
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

        [HttpGet("SearchPincode")]
        public IActionResult SearchPincode(long districtId, long stateId, long countryId, string term = "")
        {
            // Check if the term is empty or null
            if (string.IsNullOrEmpty(term?.Trim()))
            {
                // If no search term, return all pincodes for the given district, state, and country
                var allpincodes = _context.PinCode
                    .Where(x => x.DistrictId == districtId && x.StateId == stateId && x.CountryId == countryId)
                    .OrderBy(x => x.Name)
                    .Take(10)
                    .Select(x => new { PincodeId = x.PinCodeId, PincodeName = $"{x.Name} - {x.Code}" })
                    .ToList();
                return Ok(allpincodes);
            }

            // Sanitize the term by trimming spaces
            var sanitizedTerm = term.Trim();

            // Split the term by hyphen, handle both parts (name and pincode)
            var termParts = sanitizedTerm.Split('-').Select(part => part.Trim()).ToArray();

            // Name filter: The part before the hyphen (if exists)
            var nameFilter = termParts.Length > 0 ? termParts[0] : string.Empty;

            // Pincode filter: The part after the hyphen (if exists)
            var pincodeFilter = termParts.Length > 1 ? termParts[1] : string.Empty;

            // Search pincodes that match either name or pincode
            var pincodesQuery = _context.PinCode
                .Where(x => x.DistrictId == districtId && x.StateId == stateId && x.CountryId == countryId);

            // Apply name filter (case-insensitive)
            if (!string.IsNullOrEmpty(nameFilter))
            {
                pincodesQuery = pincodesQuery.Where(x => x.Name.ToLower().Contains(nameFilter.ToLower()));
            }

            // Apply pincode filter (case-insensitive)
            if (!string.IsNullOrEmpty(pincodeFilter))
            {
                pincodesQuery = pincodesQuery.Where(x => x.Code.ToLower().Contains(pincodeFilter.ToLower()));
            }

            // Get the filtered and sorted results
            var filteredPincodes = pincodesQuery
                .OrderBy(x => x.Name)
                .Take(10)
                .Select(x => new { PincodeId = x.PinCodeId, PincodeName = $"{x.Name} - {x.Code}" })
                .ToList();

            // Return the filtered pincodes
            return Ok(filteredPincodes);
        }

        [HttpGet("SearchPincodez")]
        public IActionResult SearchPincodez(long countryId, string term = "")
        {
            // Check if the term is empty or null
            if (string.IsNullOrEmpty(term?.Trim()))
            {
                // If no search term, return all pincodes for the given district, state, and country
                var allpincodes = _context.PinCode
                    .Where(x => x.CountryId == countryId)
                    .OrderBy(x => x.Name)
                    .Take(10)
                    .Select(x => new { PincodeId = x.PinCodeId, PincodeName = $"{x.Name} - {x.Code}" })
                    .ToList();
                return Ok(allpincodes);
            }

            // Sanitize the term by trimming spaces
            var sanitizedTerm = term.Trim();

            // Split the term by hyphen, handle both parts (name and pincode)
            var termParts = sanitizedTerm.Split('-').Select(part => part.Trim()).ToArray();

            // Name filter: The part before the hyphen (if exists)
            var nameFilter = termParts.Length > 0 ? termParts[0] : string.Empty;

            // Pincode filter: The part after the hyphen (if exists)
            var pincodeFilter = termParts.Length > 1 ? termParts[1] : string.Empty;

            // Search pincodes that match either name or pincode
            var pincodesQuery = _context.PinCode
                .Where(x => x.CountryId == countryId);

            // Apply name filter (case-insensitive)
            if (!string.IsNullOrEmpty(nameFilter))
            {
                pincodesQuery = pincodesQuery.Where(x => x.Name.ToLower().Contains(nameFilter.ToLower()));
            }

            // Apply pincode filter (case-insensitive)
            if (!string.IsNullOrEmpty(pincodeFilter))
            {
                pincodesQuery = pincodesQuery.Where(x => x.Code.ToLower().Contains(pincodeFilter.ToLower()));
            }

            // Get the filtered and sorted results
            var filteredPincodes = pincodesQuery
                .OrderBy(x => x.Name)
                .Take(10)
                .Select(x => new { PincodeId = x.PinCodeId, PincodeName = $"{x.Name} - {x.Code}" })
                .ToList();

            // Return the filtered pincodes
            return Ok(filteredPincodes);
        }

        [HttpGet("GetCountryName")]
        public IActionResult GetCountryName(long id)
        {
            var country = _context.Country.Where(x => x.CountryId == id).OrderBy(x => x.Name).Take(10) // Filter based on user input
                .Select(x => new { Id = x.CountryId, Name = $"{x.Name}" }).FirstOrDefault(); // Format for jQuery UI Autocomplete

            return Ok(country);
        }

        [HttpGet("GetStateName")]
        public IActionResult GetStateName(long id, long countryId)
        {
            var state = _context.State.Where(x => x.StateId == id && x.CountryId == countryId).OrderBy(x => x.Name).Take(10) // Filter based on user input
                .Select(x => new { StateId = x.StateId, StateName = $"{x.Name}" }).FirstOrDefault(); // Format for jQuery UI Autocomplete

            return Ok(state);
        }

        [HttpGet("GetStateNameForCountry")]
        public IActionResult GetStateNameForCountry(long countryId, long? id = null)
        {
            var states = _context.State
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

        [HttpGet("GetDistrictName")]
        public IActionResult GetDistrictName(long id, long stateId, long countryId)
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
            var pincode = _context.District.Where(x => x.DistrictId == id && x.StateId == stateId && x.CountryId == countryId).OrderBy(x => x.Name).Take(10) // Filter based on user input
                .Select(x => new { DistrictId = x.DistrictId, DistrictName = $"{x.Name}" }).FirstOrDefault(); // Format for jQuery UI Autocomplete

            return Ok(pincode);
        }

        [HttpGet("GetDistrictNameForAgency")]
        public IActionResult GetDistrictName(long id, long stateId, long countryId, long lob, long serviceId, long vendorId)
        {
            var districts = _context.District.Where(x => x.StateId == stateId && x.CountryId == countryId).OrderBy(x => x.Name)//.Take(10) // Filter based on user input
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

        [HttpGet("GetPincodeName")]
        public IActionResult GetPincodeName(long id, long districtId, long stateId, long countryId)
        {
            var pincode = _context.PinCode.Where(x => x.PinCodeId == id && x.DistrictId == districtId && x.StateId == stateId && x.CountryId == countryId).OrderBy(x => x.Name).Take(10) // Filter based on user input
                .Select(x => new { PincodeId = x.PinCodeId, PincodeName = $"{x.Name} - {x.Code}" }).FirstOrDefault(); // Format for jQuery UI Autocomplete

            return Ok(pincode);
        }

        [HttpGet("GetPincodeNamez")]
        public IActionResult GetPincodeNamez(long id, long countryId)
        {
            var pincode = _context.PinCode
                .Include(p => p.District)
                .Include(p => p.State)
                .Include(p => p.Country)
                .Where(x => x.PinCodeId == id && x.CountryId == countryId).OrderBy(x => x.Name).Take(10) // Filter based on user input
                .Select(x => new { PincodeId = x.PinCodeId, PincodeName = $"{x.Name} - {x.Code}", District = x.District.Name, State = x.State.Name }).FirstOrDefault(); // Format for jQuery UI Autocomplete

            return Ok(pincode);
        }

        [HttpGet("GetStateAndDistrictByPincode")]
        public IActionResult GetStateAndDistrictByPincode(long pincodeId, long countryId)
        {
            var pincode = _context.PinCode
                .Include(p => p.District)
                .Include(p => p.State)
                .Include(p => p.Country)
                .FirstOrDefault(x => x.PinCodeId == pincodeId && x.CountryId == countryId); // Format for jQuery UI Autocomplete

            var response = new
            {
                DistrictId = pincode.DistrictId,
                DistrictName = pincode.District.Name,
                StateId = pincode.StateId,
                StateName = pincode.State.Name
            };
            return Ok(response);
        }

        [HttpGet("GetPincode")]
        public IActionResult GetPincode(long id, long countryId)
        {
            var pincode = _context.PinCode.FirstOrDefault(x => x.PinCodeId == id && x.CountryId == countryId); // Format for jQuery UI Autocomplete

            var response = new
            {
                DistrictId = pincode.DistrictId,
                StateId = pincode.StateId,
                PincodeName = $"{pincode.Name} - {pincode.Code}",
                PincodeId = pincode.PinCodeId
            };
            return Ok(response);
        }

        [HttpGet("GetPincodeSuggestions")]
        public IActionResult GetPincodeSuggestions(long countryId, string term = "")
        {
            // Check if the term is empty or null
            if (string.IsNullOrEmpty(term?.Trim()))
            {
                // If no search term, return all pincodes for the given district, state, and country
                var allpincodes = _context.PinCode
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

            var pincodesQuery = _context.PinCode.Where(x => x.CountryId == countryId);

            if (!string.IsNullOrWhiteSpace(nameFilter))
            {
                // Search pincodes that match either name or pincode
                pincodesQuery = _context.PinCode
                    .Where(x => x.CountryId == countryId &&
                    (x.Name.ToLower().Contains(nameFilter.ToLower()) ||
                    x.Code.ToLower().Contains(nameFilter.ToLower()))
                    );
            }
            else
            {
                // Search pincodes that match either name or pincode
                pincodesQuery = _context.PinCode
                    .Where(x => x.CountryId == countryId &&
                    x.Code.ToLower().Contains(pincodeFilter.ToLower())
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
        [HttpGet("GetCountrySuggestions")]
        public IActionResult GetCountrySuggestions(string term = "")
        {
            var allCountries = _context.Country.ToList();

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
        [AllowAnonymous]
        [HttpGet("GetCountryIsdCode")]
        public IActionResult GetCountryIsdCode(string term = "")
        {
            var allCountries = _context.Country.ToList();

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

        [HttpGet("ValidatePhone")]
        public async Task<IActionResult> ValidatePhone(string phone, int countryCode)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return Ok(new { valid = false, message = "Mobile number is required." });
            if (await featureManager.IsEnabledAsync(FeatureFlags.VALIDATE_PHONE))
            {
                var country = await _context.Country.FirstOrDefaultAsync(c => c.ISDCode == countryCode);

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
        [HttpGet("IsValidMobileNumber")]
        public async Task<IActionResult> IsValidMobileNumber(string phone, int countryCode)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return Ok(new { valid = false, message = "Mobile number is required." });
            var country = await _context.Country.FirstOrDefaultAsync(c => c.ISDCode == countryCode);

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

        [HttpGet("bsb")]
        public IActionResult GetBSBDetails(string code)
        {
            var bsbDetail = _context.BsbInfo.FirstOrDefault(b => b.BSB == code);
            return Ok(bsbDetail);
        }
    }
}