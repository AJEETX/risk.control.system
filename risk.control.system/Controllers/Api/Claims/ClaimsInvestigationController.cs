using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

namespace risk.control.system.Controllers.Api.Claims
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClaimsInvestigationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClaimsInvestigationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetActive")]
        public async Task<IActionResult> GetActive()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.ClientCompany)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CostCentre)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.InvestigationServiceType)
                .Include(c => c.LineOfBusiness)
                .Include(c => c.PinCode)
                .Include(c => c.State);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var clientCompany = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            if (clientCompany == null)
            {
            }
            else
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == clientCompany.ClientCompanyId);
            }
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var assignedToAssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var submittededToSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var submittededToAssesssorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            if (userRole.Value.Contains(AppRoles.Creator.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId));
                var allclaimsSubmitted = await applicationDbContext.ToListAsync();
                var response = allclaimsSubmitted
                    .Select(a => new
                    {
                        Id = a.ClaimsInvestigationId,
                        SelectedToAssign = false,
                        Document = a.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.DocumentImage)) : "/img/no-image.png",
                        Customer = a.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ProfilePicture)) : "/img/no-image.png",
                        Name = a.CustomerName,
                        Policy = a.LineOfBusiness.Name,
                        Status = a.InvestigationCaseStatus.Name,
                        ServiceType = a.ClaimType.GetEnumDisplayName(),
                        Location = a.CaseLocations.Count == 0 ?
                        "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
                        string.Join("", a.CaseLocations.Select(c => "<span class='badge badge-light'>" + c.InvestigationCaseSubStatus.Name + "-" + c.PinCode.Code + "</span> ")),
                        Created = a.Created.ToString("dd-MM-yyyy"),
                        timePending = DateTime.Now.Subtract(a.Created).Days == 0 ? "< 1" : DateTime.Now.Subtract(a.Created).Days.ToString()
                    })
                    .ToList();

                return Ok(response);
            }
            else if (userRole.Value.Contains(AppRoles.Assigner.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a =>
                openStatusesIds.Contains(a.InvestigationCaseStatusId) && a.InvestigationCaseSubStatusId == assignedToAssignerStatus.InvestigationCaseSubStatusId
                || a.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId
                || a.InvestigationCaseSubStatusId == submittededToSupervisorStatus.InvestigationCaseSubStatusId
                || a.InvestigationCaseSubStatusId == submittededToAssesssorStatus.InvestigationCaseSubStatusId
                || a.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId);
            }
            else if (userRole.Value.Contains(AppRoles.Assessor.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a => a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => c.VendorId != null));

                var claimsSubmitted = new List<ClaimsInvestigation>();

                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => c.InvestigationCaseSubStatusId == submittededToAssesssorStatus.InvestigationCaseSubStatusId)?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsSubmitted.Add(item);
                    }
                }
                var response = claimsSubmitted
                    .Select(a => new
                    {
                        Id = a.ClaimsInvestigationId,
                        SelectedToAssign = false,
                        Document = a.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.DocumentImage)) : "/img/no-image.png",
                        Customer = a.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ProfilePicture)) : "/img/no-image.png",
                        Name = a.CustomerName,
                        Policy = a.LineOfBusiness.Name,
                        Status = a.InvestigationCaseStatus.Name,
                        ServiceType = a.ClaimType.GetEnumDisplayName(),
                        Location = a.CaseLocations.Count == 0 ?
                        "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
                        string.Join("", a.CaseLocations.Select(c => "<span class='badge badge-light'>" + c.InvestigationCaseSubStatus.Name + "-" + c.PinCode.Code + "</span> ")),
                        Created = a.Created.ToString("dd-MM-yyyy"),
                        timePending = DateTime.Now.Subtract(a.Created).Days == 0 ? "< 1" : DateTime.Now.Subtract(a.Created).Days.ToString()
                    })
                    .ToList();

                return Ok(response);
            }
            else if (userRole.Value.Contains(AppRoles.AgencyAdmin.ToString()) || userRole.Value.Contains(AppRoles.Supervisor.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a =>
                openStatusesIds.Contains(a.InvestigationCaseStatusId) && a.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId);
            }
            else if (!userRole.Value.Contains(AppRoles.PortalAdmin.ToString()) && !userRole.Value.Contains(AppRoles.CompanyAdmin.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId));
                var claimsSubmitted = await applicationDbContext.ToListAsync();
                var response = claimsSubmitted
                    .Select(a => new
                    {
                        Id = a.ClaimsInvestigationId,
                        SelectedToAssign = false,
                        Document = a.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.DocumentImage)) : "/img/no-image.png",
                        Customer = a.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ProfilePicture)) : "/img/no-image.png",
                        Name = a.CustomerName,
                        Policy = a.LineOfBusiness.Name,
                        Status = a.InvestigationCaseStatus.Name,
                        ServiceType = a.ClaimType.GetEnumDisplayName(),
                        Location = a.CaseLocations.Count == 0 ?
                        "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
                        string.Join("", a.CaseLocations.Select(c => "<span class='badge badge-light'>" + c.InvestigationCaseSubStatus.Name + "-" + c.PinCode.Code + "</span> ")),
                        Created = a.Created.ToString("dd-MM-yyyy"),
                        timePending = DateTime.Now.Subtract(a.Created).Days == 0 ? "< 1" : DateTime.Now.Subtract(a.Created).Days.ToString()
                    })
                    .ToList();

                return Ok(response);
            }
            var allClaims = await applicationDbContext.ToListAsync();

            var result = allClaims
                    .Select(a => new
                    {
                        Id = a.ClaimsInvestigationId,
                        SelectedToAssign = false,
                        Document = a.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.DocumentImage)) : "/img/no-image.png",
                        Customer = a.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ProfilePicture)) : "/img/no-image.png",
                        Name = a.CustomerName,
                        Policy = a.LineOfBusiness.Name,
                        Status = a.InvestigationCaseStatus.Name,
                        ServiceType = a.ClaimType.GetEnumDisplayName(),
                        Location = a.CaseLocations.Count == 0 ?
                        "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
                        string.Join("", a.CaseLocations.Select(c => "<span class='badge badge-light'>" + c.InvestigationCaseSubStatus.Name + "-" + c.PinCode.Code + "</span> ")),
                        Created = a.Created.ToString("dd-MM-yyyy"),
                        timePending = DateTime.Now.Subtract(a.Created).Days == 0 ? "< 1" : DateTime.Now.Subtract(a.Created).Days.ToString()
                    })
                    .ToList();

            return Ok(result);
        }

        [HttpGet("GetAssign")]
        public async Task<IActionResult> GetAssign()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.ClientCompany)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CostCentre)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.InvestigationServiceType)
            .Include(c => c.LineOfBusiness)
            .Include(c => c.PinCode)
            .Include(c => c.State);

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (companyUser == null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);
            }
            else if (companyUser != null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);
            }

            // SHOWING DIFFERRENT PAGES AS PER ROLES
            if (userRole.Value.Contains(AppRoles.PortalAdmin.ToString()) || userRole.Value.Contains(AppRoles.CompanyAdmin.ToString()) || userRole.Value.Contains(AppRoles.Creator.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => c.VendorId == null && c.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId));

                var claimsAssigned = new List<ClaimsInvestigation>();
                foreach (var item in applicationDbContext)
                {
                    if (item.IsReady2Assign)
                    {
                        item.CaseLocations = item.CaseLocations.Where(c => string.IsNullOrWhiteSpace(c.VendorId)
                        && c.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId)?.ToList();
                        if (item.CaseLocations.Any())
                        {
                            claimsAssigned.Add(item);
                        }
                    }
                }
                var response = claimsAssigned
                    .Select(a => new
                    {
                        Id = a.ClaimsInvestigationId,
                        SelectedToAssign = false,
                        Document = a.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.DocumentImage)) : "/img/no-image.png",
                        Customer = a.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ProfilePicture)) : "/img/no-image.png",
                        Name = a.CustomerName,
                        Policy = a.LineOfBusiness.Name,
                        Status = a.InvestigationCaseStatus.Name,
                        ServiceType = a.ClaimType.GetEnumDisplayName(),
                        Location = a.CaseLocations.Count == 0 ?
                        "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
                        string.Join("", a.CaseLocations.Select(c => "<span class='badge badge-light'>" + c.InvestigationCaseSubStatus.Name + "-" + c.PinCode.Code + "</span> ")),
                        Created = a.Created.ToString("dd-MM-yyyy"),
                        timePending = DateTime.Now.Subtract(a.Created).Days == 0 ? "< 1" : DateTime.Now.Subtract(a.Created).Days.ToString()
                    })
                    .ToList();

                return Ok(response);
            }
            return Ok(null);
        }

        [HttpGet("GetIncomplete")]
        public async Task<IActionResult> GetIncomplete()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.ClientCompany)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CostCentre)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.InvestigationServiceType)
                .Include(c => c.LineOfBusiness)
                .Include(c => c.PinCode)
                .Include(c => c.State);

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (companyUser == null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);
            }
            else if (companyUser != null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);
            }
            else if (companyUser != null && vendorUser != null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId && i.VendorId == vendorUser.VendorId);
            }

            // SHOWING DIFFERRENT PAGES AS PER ROLES
            if (userRole.Value.Contains(AppRoles.PortalAdmin.ToString()) || userRole.Value.Contains(AppRoles.CompanyAdmin.ToString()) || userRole.Value.Contains(AppRoles.Creator.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId && !a.IsReady2Assign);

                var response = await applicationDbContext
                    .Select(a => new
                    {
                        Id = a.ClaimsInvestigationId,
                        Document = a.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.DocumentImage)) : "/img/no-image.png",
                        Customer = a.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ProfilePicture)) : "/img/no-image.png",
                        Name = a.CustomerName,
                        Policy = a.LineOfBusiness.Name,
                        Status = a.InvestigationCaseStatus.Name,
                        ServiceType = a.ClaimType.GetEnumDisplayName(),
                        Location = a.CaseLocations.Count == 0 ?
                        "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
                        string.Join("", a.CaseLocations.Select(c => "<span class='badge badge-light'>" + c.InvestigationCaseSubStatus.Name + "-" + c.PinCode.Code + "</span> ")),
                        Created = a.Created.ToString("dd-MM-yyyy"),
                        timePending = DateTime.Now.Subtract(a.Created).Days == 0 ? "< 1" : DateTime.Now.Subtract(a.Created).Days.ToString()
                    })
                    .ToListAsync();

                return Ok(response);
            }

            return Ok(await applicationDbContext.ToListAsync());
        }

        [HttpGet("GetAssigner")]
        public async Task<IActionResult> GetAssigner()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
             .Include(c => c.ClientCompany)
             .Include(c => c.CaseEnabler)
             .Include(c => c.CaseLocations)
             .ThenInclude(c => c.PinCode)
             .Include(c => c.CaseLocations)
             .ThenInclude(c => c.InvestigationCaseSubStatus)
             .Include(c => c.CostCentre)
             .Include(c => c.Country)
             .Include(c => c.District)
             .Include(c => c.InvestigationCaseStatus)
             .Include(c => c.InvestigationCaseSubStatus)
             .Include(c => c.InvestigationServiceType)
             .Include(c => c.LineOfBusiness)
             .Include(c => c.PinCode)
             .Include(c => c.State);

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (companyUser == null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);
            }
            else if (companyUser != null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);
            }

            // SHOWING DIFFERRENT PAGES AS PER ROLES
            applicationDbContext = applicationDbContext.Where(a => a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => c.VendorId == null));

            var claimsAssigned = new List<ClaimsInvestigation>();

            foreach (var item in applicationDbContext)
            {
                item.CaseLocations = item.CaseLocations.Where(c => string.IsNullOrWhiteSpace(c.VendorId)
                    && c.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId)?.ToList();
                if (item.CaseLocations.Any())
                {
                    claimsAssigned.Add(item);
                }
            }
            var response = claimsAssigned
                    .Select(a => new
                    {
                        Id = a.ClaimsInvestigationId,
                        Document = a.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.DocumentImage)) : "/img/no-image.png",
                        Customer = a.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ProfilePicture)) : "/img/no-image.png",
                        Name = a.CustomerName,
                        Policy = a.LineOfBusiness.Name,
                        Status = a.InvestigationCaseStatus.Name,
                        ServiceType = a.ClaimType.GetEnumDisplayName(),
                        Location = a.CaseLocations.Count == 0 ?
                        "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
                        string.Join("", a.CaseLocations.Select(c => "<span class='badge badge-light'>" + c.InvestigationCaseSubStatus.Name + "-" + c.PinCode.Code + "</span> ")),
                        Created = a.Created.ToString("dd-MM-yyyy"),
                        timePending = DateTime.Now.Subtract(a.Created).Days == 0 ? "< 1" : DateTime.Now.Subtract(a.Created).Days.ToString()
                    })
                    ?.ToList();

            return Ok(response);
        }

        [HttpGet("GetAssessor")]
        public async Task<IActionResult> GetAssessor()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.ClientCompany)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CostCentre)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.InvestigationServiceType)
                .Include(c => c.LineOfBusiness)
                .Include(c => c.PinCode)
                .Include(c => c.State);

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (companyUser == null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);
            }
            else if (companyUser != null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);
            }

            var claimsAssigned = new List<ClaimsInvestigation>();
            if (userRole.Value.Contains(AppRoles.PortalAdmin.ToString()) || userRole.Value.Contains(AppRoles.CompanyAdmin.ToString()) || userRole.Value.Contains(AppRoles.Creator.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => c.VendorId == null && c.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId));

                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => string.IsNullOrWhiteSpace(c.VendorId)
                    && c.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId)?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsAssigned.Add(item);
                    }
                }
            }
            else if (userRole.Value.Contains(AppRoles.Assigner.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => c.VendorId == null));

                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => string.IsNullOrWhiteSpace(c.VendorId)
                        && c.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId)?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsAssigned.Add(item);
                    }
                }
            }
            else if (userRole.Value.Contains(AppRoles.Assessor.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => c.VendorId != null));

                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => c.InvestigationCaseSubStatusId == submittedToAssessorStatus.InvestigationCaseSubStatusId)?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsAssigned.Add(item);
                    }
                }
            }

            var response = claimsAssigned
            .Select(a => new
            {
                Id = a.ClaimsInvestigationId,
                Document = a.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.DocumentImage)) : "/img/no-image.png",
                Customer = a.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ProfilePicture)) : "/img/no-image.png",
                Name = a.CustomerName,
                Policy = a.LineOfBusiness.Name,
                Status = a.InvestigationCaseStatus.Name,
                ServiceType = a.ClaimType.GetEnumDisplayName(),
                Location = a.CaseLocations.Count == 0 ?
                "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
                string.Join("", a.CaseLocations.Select(c => "<span class='badge badge-light'>" + c.InvestigationCaseSubStatus.Name + "-" + c.PinCode.Code + "</span> ")),
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = DateTime.Now.Subtract(a.Created).Days == 0 ? "< 1" : DateTime.Now.Subtract(a.Created).Days.ToString()
            })
            ?.ToList();

            return Ok(response);
        }

        [HttpGet("GetApproved")]
        public async Task<IActionResult> GetApproved()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
             .Include(c => c.ClientCompany)
             .Include(c => c.CaseEnabler)
             .Include(c => c.CaseLocations)
            .ThenInclude(c => c.PinCode)
             .Include(c => c.CaseLocations).
             ThenInclude(c => c.InvestigationCaseSubStatus)
             .Include(c => c.CostCentre)
             .Include(c => c.Country)
             .Include(c => c.District)
             .Include(c => c.InvestigationCaseStatus)
             .Include(c => c.InvestigationCaseSubStatus)
             .Include(c => c.InvestigationServiceType)
             .Include(c => c.LineOfBusiness)
             .Include(c => c.PinCode)
             .Include(c => c.State);

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var assessorApprovedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (companyUser == null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);
            }
            else if (companyUser != null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);
            }
            var claimsSubmitted = new List<ClaimsInvestigation>();

            if (userRole.Value.Contains(AppRoles.Assessor.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => c.VendorId != null));

                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => c.InvestigationCaseSubStatusId == assessorApprovedStatus.InvestigationCaseSubStatusId)?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsSubmitted.Add(item);
                    }
                }
            }
            var response = claimsSubmitted
            .Select(a => new
            {
                Id = a.ClaimsInvestigationId,
                Document = a.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.DocumentImage)) : "/img/no-image.png",
                Customer = a.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ProfilePicture)) : "/img/no-image.png",
                Name = a.CustomerName,
                Policy = a.LineOfBusiness.Name,
                Status = a.InvestigationCaseStatus.Name,
                ServiceType = a.ClaimType.GetEnumDisplayName(),
                Location = a.CaseLocations.Count == 0 ?
                "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
                string.Join("", a.CaseLocations.Select(c => "<span class='badge badge-light'>" + c.InvestigationCaseSubStatus.Name + "-" + c.PinCode.Code + "</span> ")),
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = DateTime.Now.Subtract(a.Created).Days == 0 ? "< 1" : DateTime.Now.Subtract(a.Created).Days.ToString()
            })
            ?.ToList();

            return Ok(response);
        }

        [HttpGet("GetReview")]
        public async Task<IActionResult> GetReview()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                 .Include(c => c.ClientCompany)
                 .Include(c => c.CaseEnabler)
                 .Include(c => c.CaseLocations)
                 .ThenInclude(c => c.PinCode)
                 .Include(c => c.CaseLocations)
                 .ThenInclude(c => c.ClaimReport)
                 .Include(c => c.CaseLocations).
                 ThenInclude(c => c.InvestigationCaseSubStatus)
             .Include(c => c.CostCentre)
             .Include(c => c.Country)
             .Include(c => c.District)
             .Include(c => c.InvestigationCaseStatus)
             .Include(c => c.InvestigationCaseSubStatus)
             .Include(c => c.InvestigationServiceType)
             .Include(c => c.LineOfBusiness)
             .Include(c => c.PinCode)
             .Include(c => c.State);

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var reassignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (companyUser == null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);
            }
            else if (companyUser != null && vendorUser == null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);
            }
            var claimsSubmitted = new List<ClaimsInvestigation>();

            if (userRole.Value.Contains(AppRoles.Assessor.ToString()) || userRole.Value.Contains(AppRoles.Assigner.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.CaseLocations.Count > 0 && a.CaseLocations.Any(c => c.VendorId != null));

                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => c.InvestigationCaseSubStatusId == reassignedStatus.InvestigationCaseSubStatusId)?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsSubmitted.Add(item);
                    }
                }
            }

            var response = claimsSubmitted
        .Select(a => new
        {
            Id = a.ClaimsInvestigationId,
            Document = a.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.DocumentImage)) : "/img/no-image.png",
            Customer = a.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ProfilePicture)) : "/img/no-image.png",
            Name = a.CustomerName,
            Policy = a.LineOfBusiness.Name,
            Status = a.InvestigationCaseStatus.Name,
            ServiceType = a.ClaimType.GetEnumDisplayName(),
            Location = a.CaseLocations.Count == 0 ?
            "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
            string.Join("", a.CaseLocations.Select(c => "<span class='badge badge-light'>" + c.InvestigationCaseSubStatus.Name + "-" + c.PinCode.Code + "</span> ")),
            Created = a.Created.ToString("dd-MM-yyyy"),
            timePending = DateTime.Now.Subtract(a.Created).Days == 0 ? "< 1" : DateTime.Now.Subtract(a.Created).Days.ToString()
        })
        ?.ToList();

            return Ok(response);
        }

        [HttpGet("GetToInvestigate")]
        public async Task<IActionResult> GetToInvestigate()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
               .Include(c => c.ClientCompany)
               .Include(c => c.CaseEnabler)
               .Include(c => c.CostCentre)
               .Include(c => c.CaseLocations)
               .ThenInclude(c => c.InvestigationCaseSubStatus)
               .Include(c => c.CaseLocations)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.Country)
               .Include(c => c.District)
               .Include(c => c.InvestigationCaseStatus)
               .Include(c => c.InvestigationCaseSubStatus)
               .Include(c => c.InvestigationServiceType)
               .Include(c => c.LineOfBusiness)
               .Include(c => c.PinCode)
               .Include(c => c.State);

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var assignedToAssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);

            if (userRole.Value.Contains(AppRoles.Creator.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId));
            }
            else if (userRole.Value.Contains(AppRoles.Assigner.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a =>
                openStatusesIds.Contains(a.InvestigationCaseStatusId) && a.InvestigationCaseSubStatusId == assignedToAssignerStatus.InvestigationCaseSubStatusId
                || a.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId);
            }
            else if (userRole.Value.Contains(AppRoles.AgencyAdmin.ToString()) || userRole.Value.Contains(AppRoles.Supervisor.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a =>
                openStatusesIds.Contains(a.InvestigationCaseStatusId) && a.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId);
            }
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var clientCompany = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            if (clientCompany == null)
            {
            }
            else
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == clientCompany.ClientCompanyId);
            }
            var claimsSubmitted = await applicationDbContext.ToListAsync();
            var response = claimsSubmitted
                .Select(a => new
                {
                    Id = a.ClaimsInvestigationId,
                    Document = a.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.DocumentImage)) : "/img/no-image.png",
                    Customer = a.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ProfilePicture)) : "/img/no-image.png",
                    Name = a.CustomerName,
                    Policy = a.LineOfBusiness.Name,
                    Status = a.InvestigationCaseStatus.Name,
                    ServiceType = a.ClaimType.GetEnumDisplayName(),
                    Location = a.CaseLocations.Count == 0 ?
                "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
                string.Join("", a.CaseLocations.Select(c => "<span class='badge badge-light'>" + c.InvestigationCaseSubStatus.Name + "-" + c.PinCode.Code + "</span> ")),
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = DateTime.Now.Subtract(a.Created).Days == 0 ? "< 1" : DateTime.Now.Subtract(a.Created).Days.ToString()
                })
                ?.ToList();

            await Task.Delay(1000);
            return Ok(response);
        }
    }
}