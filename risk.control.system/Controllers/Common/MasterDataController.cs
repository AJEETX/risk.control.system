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
        public async Task<IActionResult> SearchCountry(string term = "")
        {
            try
            {
                var allCountries = await _context.Country.AsNoTracking().ToListAsync();

                if (string.IsNullOrEmpty(term))
                {
                    var result = allCountries
                        .OrderBy(x => x.Name)
                     .Take(10)
                     .Select(c => new
                     {
                         Id = c.CountryId,
                         Name = c.Name,
                         Label = c.Name
                     })?
                        .ToList();
                    return Ok(result);
                }

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
        public async Task<IActionResult> SearchState(long countryId, string term = "")
        {
            try
            {
                var searchTerm = term?.Trim();
                var query = _context.State
                    .AsNoTracking()
                    .Where(x => x.CountryId == countryId);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(x => EF.Functions.Like(x.Name, $"%{searchTerm}%"));
                }
                ;
                var states = await query
                    .OrderBy(x => x.Name)
                    .Take(10)
                    .Select(x => new
                    {
                        StateId = x.StateId,
                        StateName = x.Name
                    })
                    .ToListAsync();
                return Ok(states);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting states");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("SearchDistrictTerm")]
        public async Task<IActionResult> SearchDistrictTerm(long stateId, long countryId, string term = "")
        {
            try
            {
                var searchTerm = term?.Trim();
                var query = _context.District
                    .AsNoTracking()
                    .Where(x => x.CountryId == countryId && x.StateId == stateId);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(x => EF.Functions.Like(x.Name, $"%{searchTerm}%"));
                }

                var districts = await query
                    .OrderBy(x => x.Name)
                    .Take(10)
                    .Select(x => new
                    {
                        DistrictId = x.DistrictId,
                        DistrictName = x.Name
                    })
                    .ToListAsync();

                var result = new List<object>
                {
                };
                result.AddRange(districts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting districts");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("SearchDistrict")]
        public async Task<IActionResult> SearchDistrict(long stateId, long countryId, string term = "")
        {
            try
            {
                var searchTerm = term?.Trim();
                var query = _context.District
                    .AsNoTracking()
                    .Where(x => x.CountryId == countryId && x.StateId == stateId);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(x => EF.Functions.Like(x.Name, $"%{searchTerm}%"));
                }

                var districts = await query.OrderBy(x => x.Name).Take(10).Select(x => new { DistrictId = x.DistrictId, DistrictName = x.Name }).ToListAsync();
                var result = new List<object>
                {
                    new
                    {
                        DistrictId = -1, // Special value for "ALL DISTRICTS"
                        DistrictName = Applicationsettings.ALL_DISTRICT
                    }
                };
                result.AddRange(districts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting districts");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetCountryName")]
        public async Task<IActionResult> GetCountryName(long id)
        {
            try
            {
                var country = await _context.Country.AsNoTracking().Where(x => x.CountryId == id).OrderBy(x => x.Name).Take(10) // Filter based on user input
                    .Select(x => new { Id = x.CountryId, Name = $"{x.Name}" }).FirstOrDefaultAsync(); // Format for jQuery UI Autocomplete

                return Ok(country);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting countries");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetStateName")]
        public async Task<IActionResult> GetStateName(long id, long countryId)
        {
            try
            {
                var state = await _context.State.AsNoTracking().Where(x => x.StateId == id && x.CountryId == countryId).OrderBy(x => x.Name).Take(10) // Filter based on user input
                    .Select(x => new { StateId = x.StateId, StateName = $"{x.Name}" }).FirstOrDefaultAsync(); // Format for jQuery UI Autocomplete

                return Ok(state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting states");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetStateNameForCountry")]
        public async Task<IActionResult> GetStateNameForCountry(long countryId, long? id = null)
        {
            try
            {
                var states = await _context.State.AsNoTracking()
                    .Where(x => x.CountryId == countryId)
                    .OrderBy(x => x.Name)
                    .Select(x => new { StateId = x.StateId, StateName = x.Name })
                    .ToListAsync();

                if (id.HasValue)
                {
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
        public async Task<IActionResult> GetDistrictName(long id, long stateId, long countryId)
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
                var pincode = await _context.District.AsNoTracking().Where(x => x.DistrictId == id && x.StateId == stateId && x.CountryId == countryId).OrderBy(x => x.Name).Take(10) // Filter based on user input
                    .Select(x => new { DistrictId = x.DistrictId, DistrictName = $"{x.Name}" }).FirstOrDefaultAsync(); // Format for jQuery UI Autocomplete

                return Ok(pincode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting districts");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetDistrictNameForAgency")]
        public async Task<IActionResult> GetDistrictNameForAgency(long id, long stateId, long countryId, long lob, long serviceId, long vendorId)
        {
            try
            {
                var districts = await _context.District.AsNoTracking().Where(x => x.StateId == stateId && x.CountryId == countryId).OrderBy(x => x.Name)//.Take(10) // Filter based on user input
                                .Select(x => new { DistrictId = x.DistrictId, DistrictName = $"{x.Name}" }).ToListAsync(); // Format for jQuery UI Autocomplete

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
        public async Task<IActionResult> GetPincode(long id, long countryId)
        {
            try
            {
                var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var pincode = await _context.PinCode.AsNoTracking().FirstOrDefaultAsync(x => x.PinCodeId == id && x.CountryId == countryId); // Format for jQuery UI Autocomplete

                var response = new
                {
                    DistrictId = pincode!.DistrictId,
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
        public async Task<IActionResult> GetPincodeSuggestions(long countryId, string term = "")
        {
            try
            {
                if (string.IsNullOrEmpty(term?.Trim()))
                {
                    var allpincodes = await _context.PinCode.AsNoTracking().Include(x => x.State).Include(x => x.District).Where(x => x.CountryId == countryId).OrderBy(x => x.Name).Take(10)
                        .Select(x => new { PincodeId = x.PinCodeId, Pincode = x.Code, Name = x.Name, StateId = x.StateId, StateName = x.State!.Name, DistricId = x.DistrictId, DistrictName = x.District!.Name }).ToListAsync();
                    return Ok(allpincodes);
                }
                var sanitizedTerm = term.Trim();
                var termParts = sanitizedTerm.Split('-').Select(part => part.Trim()).ToArray();
                var nameFilter = termParts.Length > 0 ? termParts[0] : string.Empty;
                var pincodeFilter = termParts.Length > 1 ? termParts[1] : string.Empty;
                var pincodesQuery = _context.PinCode.AsNoTracking().Where(x => x.CountryId == countryId);
                if (!string.IsNullOrWhiteSpace(nameFilter))
                {
                    var filter = $"%{nameFilter}%";
                    pincodesQuery = pincodesQuery.Where(x => EF.Functions.Like(x.Name, filter) || EF.Functions.Like(x.Code.ToString(), filter));
                }
                else
                {
                    pincodesQuery = _context.PinCode.AsNoTracking().Where(x => x.CountryId == countryId && x.Code.ToString().Contains(pincodeFilter.ToLower()));
                }
                var filteredPincodes = pincodesQuery.Include(x => x.State).Include(x => x.District).Take(10).OrderBy(x => x.Name)
                        .Select(x => new { PincodeId = x.PinCodeId, Pincode = x.Code, Name = x.Name, StateId = x.StateId, StateName = x.State!.Name, DistrictId = x.DistrictId, DistrictName = x.District!.Name })?.ToList();
                return Ok(filteredPincodes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting pincodes");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetCountrySuggestions")]
        public async Task<IActionResult> GetCountrySuggestions(string term = "")
        {
            try
            {
                var allCountries = await _context.Country.AsNoTracking().ToListAsync();

                if (string.IsNullOrEmpty(term))
                    return Ok(allCountries.OrderBy(x => x.Name).Take(10).Select(c => new { Id = c.CountryId, Name = c.Name, Label = c.Name })?.ToList());

                var countries = allCountries.Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name).Take(10).Select(c => new { Id = c.CountryId, Name = c.Name, Label = c.Name })?.ToList();
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