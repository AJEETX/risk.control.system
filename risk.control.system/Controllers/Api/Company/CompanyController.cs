using System.Data;
using System.Globalization;

using Google.Api;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
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
        private static CultureInfo hindi = new CultureInfo("hi-IN");
        private static NumberFormatInfo hindiNFO = (NumberFormatInfo)hindi.NumberFormat.Clone();
        private readonly string noUserImagefilePath = string.Empty;
        private readonly ApplicationDbContext _context;
        private readonly IUserService userService;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;

        public CompanyController(ApplicationDbContext context, IUserService userService, UserManager<ClientCompanyApplicationUser> userManager)
        {
            this.userManager = userManager;
            _context = context;
            this.userService = userService;
            noUserImagefilePath = "/img/no-user.png";
        }

        [HttpGet("AllCompanies")]
        public async Task<IActionResult> AllCompanies()
        {
            var companies = _context.ClientCompany.
                Where(v => !v.Deleted)
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State).OrderBy(o => o.Name);

            var result =
                companies.Select(u =>
                new
                {
                    Id = u.ClientCompanyId,
                    Document = string.IsNullOrWhiteSpace(u.DocumentUrl) ? Applicationsettings.NO_IMAGE : u.DocumentUrl,
                    Domain = $"<a href='/ClientCompany/Details?Id={u.ClientCompanyId}'>" + u.Email + "</a>",
                    Name = u.Name,
                    //Code = u.Code,
                    Phone = "(+" + u.Country.ISDCode + ") " + u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Code,
                    Country = u.Country.Code,
                    Flag = "/flags/" + u.Country.Code.ToLower() + ".png",
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    Active = u.Status.GetEnumDisplayName(),
                    UpdatedBy = u.UpdatedBy,
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated
                })?.ToArray();
            companies.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync();
            return Ok(result);
        }

        [HttpGet("CompanyUsers")]
        public async Task<IActionResult> CompanyUsers(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var result = await userService.GetCompanyUsers(userEmail, id);

            return Ok(result);
        }

        [HttpGet("AllUsers")]
        public async Task<IActionResult> AllUsers()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var result = await userService.GetCompanyUsers(userEmail);

            return Ok(result);
        }

        [HttpGet("GetEmpanelledVendors")]
        public async Task<IActionResult> GetEmpanelledVendors()
        {
            var userEmail = HttpContext.User?.Identity?.Name; 
            var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submitted2SuperStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);
            var enquiryRequestStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);
            var claimsCases = _context.ClaimsInvestigation
                .Where(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                (c.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                c.InvestigationCaseSubStatusId == enquiryRequestStatus.InvestigationCaseSubStatusId ||
                c.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId))
                ?.ToList();
            var company = _context.ClientCompany
                .Include(c => c.CompanyApplicationUser)
                .Include(c => c.EmpanelledVendors).ThenInclude(c => c.State)
                .Include(c => c.EmpanelledVendors).ThenInclude(c => c.District)
                .Include(c => c.EmpanelledVendors).ThenInclude(c => c.Country)
                .Include(c => c.EmpanelledVendors).ThenInclude(c => c.PinCode)
                .Include(c => c.EmpanelledVendors).ThenInclude(c => c.ratings)
                .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var result = company.EmpanelledVendors?.Where(v => !v.Deleted && v.Status == VendorStatus.ACTIVE)
                .OrderBy(u => u.Name).Select(u => new
                {
                    Id = u.VendorId,
                    Document = string.IsNullOrWhiteSpace(u.DocumentUrl) ? Applicationsettings.NO_IMAGE : u.DocumentUrl,
                    Domain = companyUser.Role == AppRoles.COMPANY_ADMIN ? "<a href=/Company/AgencyDetail?id=" + u.VendorId + ">" + u.Email + "</a>" : u.Email,
                    Name = u.Name,
                    Code = u.Code,
                    Phone = "(+" + u.Country.ISDCode + ") " + u.PhoneNumber,
                    Address = $"{u.Addressline}",
                    District = u.District.Name,
                    State = u.State.Code,
                    Country = u.Country.Code,
                    Flag = "/flags/" + u.Country.Code.ToLower() + ".png",
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    UpdateBy = u.UpdatedBy,
                    CaseCount = claimsCases.Count(c => c.VendorId == u.VendorId),
                    RateCount = u.RateCount,
                    RateTotal = u.RateTotal,
                    RawAddress = u.Addressline + "," + u.District.Name + ", " + u.State.Code + ", " + u.Country.Code,
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated
                })?.ToArray();

            company.EmpanelledVendors?.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync();
            return Ok(result);
        }

        [HttpGet("GetAvailableVendors")]
        public async Task<IActionResult> GetAvailableVendors()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var company = _context.ClientCompany
                .Include(c => c.EmpanelledVendors)
                .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var availableVendors = _context.Vendor
                .Where(v => !company.EmpanelledVendors.Contains(v) && !v.Deleted && v.CountryId == company.CountryId)
                .Include(v => v.VendorApplicationUser)
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes)
                .OrderBy(u => u.Name)
                .AsQueryable();

            var result =
                availableVendors?.Select(u =>
                new
                {
                    Id = u.VendorId,
                    Document = string.IsNullOrWhiteSpace(u.DocumentUrl) ? Applicationsettings.NO_IMAGE : u.DocumentUrl,
                    Domain = "<a href=/Vendors/Details?id=" + u.VendorId + ">" + u.Email + "</a>",
                    Name = u.Name,
                    Code = u.Code,
                    Phone = "(+" + u.Country.ISDCode + ") " + u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Name,
                    Country = u.Country.Code,
                    Flag = "/flags/" + u.Country.Code.ToLower() + ".png",
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    UpdateBy = u.UpdatedBy,
                    CanOnboard = u.Status == VendorStatus.ACTIVE &&
                        u.VendorInvestigationServiceTypes != null &&
                        u.VendorApplicationUser != null &&
                        u.VendorApplicationUser.Count > 0 &&
                        u.VendorInvestigationServiceTypes.Count > 0,
                    VendorName = u.Email,
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated,
                    Deletable = u.CreatedUser == userEmail
                })?.ToArray();
            availableVendors?.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync();
            return Ok(result);
        }

        [HttpGet("AllServices")]
        public async Task<IActionResult> AllServices(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var vendor = _context.Vendor
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(i => i.LineOfBusiness)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                 .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.State)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.Country)
                .Include(i => i.District)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(i => i.InvestigationServiceType)
                .Include(i => i.State)
                .Include(i => i.VendorInvestigationServiceTypes)
                .FirstOrDefault(a => a.VendorId == id);

            var services = vendor.VendorInvestigationServiceTypes?
               .OrderBy(s => s.InvestigationServiceType.Name);
            var serviceResponse = new List<AgencyServiceResponse>();
            foreach (var service in services)
            {
                var IsAllDistrict = (service.DistrictId == null);
                string pincodes = $"{ALL_PINCODE}";
                string rawPincodes = $"{ALL_PINCODE}";
                //if (!IsAllDistrict)
                //{
                //    var allPinCodesForDistrict = await _context.PinCode.CountAsync(p => p.DistrictId == service.DistrictId);
                //    if (allPinCodesForDistrict == service.PincodeServices.Count)
                //    {
                //        pincodes = ALL_PINCODE;
                //        rawPincodes = ALL_PINCODE;
                //    }
                //    else
                //    {
                //        pincodes = string.Join(", ", service.PincodeServices.Select(c => c.Pincode).Distinct());
                //        rawPincodes = string.Join(", ", service.PincodeServices.Select(c => c.Name).Distinct());
                //    }
                //}

                serviceResponse.Add(new AgencyServiceResponse
                {
                    VendorId = service.VendorId,
                    Id = service.VendorInvestigationServiceTypeId,
                    CaseType = service.LineOfBusiness.Name,
                    ServiceType = service.InvestigationServiceType.Name,
                    District = IsAllDistrict ? ALL_DISTRICT : service.District.Name,
                    State = service.State.Code,
                    Country = service.Country.Code,
                    Flag = "/flags/" + service.Country.Code.ToLower() + ".png",
                    Pincodes = pincodes,
                    RawPincodes = rawPincodes,
                    Rate = string.Format(Extensions.GetCultureByCountry(service.Country.Code.ToUpper()), "{0:c}", service.Price),
                    UpdatedBy = service.UpdatedBy,
                    Updated = service.Updated.HasValue ? service.Updated.Value.ToString("dd-MM-yyyy") : service.Created.ToString("dd-MM-yyyy"),
                    IsUpdated = service.IsUpdated,
                    LastModified = service.Updated
                });
            }

            vendor.VendorInvestigationServiceTypes?.ToList().ForEach(i => i.IsUpdated = false);
            await _context.SaveChangesAsync();
            return Ok(serviceResponse);
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

        [HttpGet("SearchRemainingDistrict")]
        public IActionResult SearchRemainingDistrict(long stateId, long countryId, long vendorId, long lobId, long serviceId, string term = "")
        {
            var existingServices = _context.VendorInvestigationServiceType.AsNoTracking().
                        Where(v =>
                            v.VendorId == vendorId &&
                            v.LineOfBusinessId == lobId &&
                            v.InvestigationServiceTypeId == serviceId &&
                            v.StateId == stateId);
            if(existingServices is null)
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
            if (existingServices is not null && existingServices.Any(e=>e.DistrictId == null))
            {
                return Ok();
            }
            if (existingServices is not null && existingServices.All(e => e.DistrictId != null))
            {
                var existingDistricts = existingServices.Select(e => e.DistrictId).ToList();
                // If the search term is empty or null, fetch the first 10 districts
                var districts = string.IsNullOrEmpty(term?.Trim())
                    ? _context.District
                        .Where(x => x.CountryId == countryId && x.StateId == stateId && !existingDistricts.Contains(x.DistrictId))
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
            return Ok();
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
    }
}