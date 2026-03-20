using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Controllers.Common
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{CREATOR.DISPLAY_NAME},{ASSESSOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    public class MasterDataController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MasterDataController> _logger;

        public MasterDataController(
            ApplicationDbContext context,
            ILogger<MasterDataController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("SearchCountry")]
        public IActionResult SearchCountry(string term = "")
        {
            try
            {
                var allCountries = _context.Country.AsNoTracking().ToList();

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
                _logger.LogError(ex, "Error occurred while getting countries");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("SearchState")]
        public IActionResult SearchState(long countryId, string term = "")
        {
            try
            {
                if (string.IsNullOrEmpty(term?.Trim()))
                    return Ok(_context.State.AsNoTracking().Where(x => x.CountryId == countryId)?
                        .OrderBy(x => x.Name)
                     .Take(10)
                     .Select(x => new { StateId = x.StateId, StateName = x.Name })?.ToList());

                var states = _context.State.AsNoTracking().Where(x => x.CountryId == countryId && x.Name.ToLower().Contains(term.ToLower()))
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
                _logger.LogError(ex, "Error occurred while getting states");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("SearchDistrictTerm")]
        public IActionResult SearchDistrictTerm(long stateId, long countryId, string term = "")
        {
            try
            {
                var districts = string.IsNullOrEmpty(term?.Trim())
                ? _context.District.AsNoTracking()
                    .Where(x => x.CountryId == countryId && x.StateId == stateId)
                    .OrderBy(x => x.Name)
                    .Take(10)
                    .Select(x => new
                    {
                        DistrictId = x.DistrictId,
                        DistrictName = $"{x.Name}"
                    })
                    .ToList()
                : _context.District.AsNoTracking()
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
                };

                // Append the queried districts to the result
                result.AddRange(districts);

                // Return the final response
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting districts");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("SearchDistrict")]
        public IActionResult SearchDistrict(long stateId, long countryId, string term = "")
        {
            try
            {
                var districts = string.IsNullOrEmpty(term?.Trim())
                ? _context.District.AsNoTracking()
                    .Where(x => x.CountryId == countryId && x.StateId == stateId)
                    .OrderBy(x => x.Name)
                    .Take(10)
                    .Select(x => new
                    {
                        DistrictId = x.DistrictId,
                        DistrictName = $"{x.Name}"
                    })
                    .ToList()
                : _context.District.AsNoTracking()
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
                _logger.LogError(ex, "Error occurred while getting districts");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetCountryName")]
        public IActionResult GetCountryName(long id)
        {
            try
            {
                var country = _context.Country.AsNoTracking().Where(x => x.CountryId == id).OrderBy(x => x.Name).Take(10) // Filter based on user input
                    .Select(x => new { Id = x.CountryId, Name = $"{x.Name}" }).FirstOrDefault(); // Format for jQuery UI Autocomplete

                return Ok(country);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting countries");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetStateName")]
        public IActionResult GetStateName(long id, long countryId)
        {
            try
            {
                var state = _context.State.AsNoTracking().Where(x => x.StateId == id && x.CountryId == countryId).OrderBy(x => x.Name).Take(10) // Filter based on user input
                    .Select(x => new { StateId = x.StateId, StateName = $"{x.Name}" }).FirstOrDefault(); // Format for jQuery UI Autocomplete

                return Ok(state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting states");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetStateNameForCountry")]
        public IActionResult GetStateNameForCountry(long countryId, long? id = null)
        {
            try
            {
                var states = _context.State.AsNoTracking()
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
                _logger.LogError(ex, "Error occurred while getting states for country");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetDistrictName")]
        public IActionResult GetDistrictName(long id, long stateId, long countryId)
        {
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
                var pincode = _context.District.AsNoTracking().Where(x => x.DistrictId == id && x.StateId == stateId && x.CountryId == countryId).OrderBy(x => x.Name).Take(10) // Filter based on user input
                    .Select(x => new { DistrictId = x.DistrictId, DistrictName = $"{x.Name}" }).FirstOrDefault(); // Format for jQuery UI Autocomplete

                return Ok(pincode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting districts");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetDistrictNameForAgency")]
        public IActionResult GetDistrictNameForAgency(long id, long stateId, long countryId, long lob, long serviceId, long vendorId)
        {
            try
            {
                var districts = _context.District.AsNoTracking().Where(x => x.StateId == stateId && x.CountryId == countryId).OrderBy(x => x.Name)//.Take(10) // Filter based on user input
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
                _logger.LogError(ex, "Error occurred while getting districts for agency");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetPincode")]
        public IActionResult GetPincode(long id, long countryId)
        {
            try
            {
                var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var pincode = _context.PinCode.AsNoTracking().FirstOrDefault(x => x.PinCodeId == id && x.CountryId == countryId); // Format for jQuery UI Autocomplete

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
                _logger.LogError(ex, "Error occurred while getting investigations");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetPincodeSuggestions")]
        public IActionResult GetPincodeSuggestions(long countryId, string term = "")
        {
            try
            {
                // Check if the term is empty or null
                if (string.IsNullOrEmpty(term?.Trim()))
                {
                    // If no search term, return all pincodes for the given district, state, and country
                    var allpincodes = _context.PinCode.AsNoTracking()
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

                var pincodesQuery = _context.PinCode.AsNoTracking().Where(x => x.CountryId == countryId);

                if (!string.IsNullOrWhiteSpace(nameFilter))
                {
                    // Search pincodes that match either name or pincode
                    pincodesQuery = _context.PinCode.AsNoTracking()
                        .Where(x => x.CountryId == countryId &&
                        (x.Name.ToLower().Contains(nameFilter.ToLower()) ||
                        x.Code.ToString().Contains(nameFilter.ToLower()))
                        );
                }
                else
                {
                    // Search pincodes that match either name or pincode
                    pincodesQuery = _context.PinCode.AsNoTracking()
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
                _logger.LogError(ex, "Error occurred while getting pincodes");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetCountrySuggestions")]
        public IActionResult GetCountrySuggestions(string term = "")
        {
            try
            {
                var allCountries = _context.Country.AsNoTracking().ToList();

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
                _logger.LogError(ex, "Error occurred while getting country");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}