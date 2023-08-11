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
    public class ClaimsVendorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClaimsVendorController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetOpen")]
        public async Task<IActionResult> GetOpen()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                            .Include(c => c.ClientCompany)
                            .Include(c => c.CaseEnabler)
                            .Include(c => c.CaseLocations)
                            .ThenInclude(c => c.InvestigationCaseSubStatus)
                            .Include(c => c.CaseLocations)
                            .ThenInclude(c => c.PinCode)
                            .Include(c => c.CaseLocations)
                            .ThenInclude(c => c.Vendor)
                            .Include(c => c.CostCentre)
                            .Include(c => c.Country)
                            .Include(c => c.District)
                            .Include(c => c.InvestigationCaseStatus)
                            .Include(c => c.InvestigationCaseSubStatus)
                            .Include(c => c.InvestigationServiceType)
                            .Include(c => c.LineOfBusiness)
                            .Include(c => c.PinCode)
                            .Include(c => c.State);

            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (vendorUser != null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));
            }

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var openSubstatusesForSupervisor = _context.InvestigationCaseSubStatus.Where(i =>
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR) ||
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT) ||
            i.Name.Contains(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR)
            ).Select(s => s.InvestigationCaseSubStatusId).ToList();

            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submittedToVendorSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            if (userRole.Value.Contains(AppRoles.AgencyAdmin.ToString()) || userRole.Value.Contains(AppRoles.Supervisor.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                if (userRole.Value.Contains(AppRoles.Supervisor.ToString()))
                {
                    applicationDbContext = applicationDbContext.Where(a => openSubstatusesForSupervisor.Contains(a.InvestigationCaseSubStatusId));
                }

                var claimsAllocated = new List<ClaimsInvestigation>();

                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => !string.IsNullOrWhiteSpace(c.VendorId)
                        && c.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId
                        || c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId
                        || c.InvestigationCaseSubStatusId == submittedToVendorSupervisorStatus.InvestigationCaseSubStatusId)?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsAllocated.Add(item);
                    }
                }
                var response = claimsAllocated
                   .Select(a => new
                   {
                       Id = a.ClaimsInvestigationId,
                       Company = a.ClientCompany.Name,
                       SelectedToAssign = false,
                       Document = a.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.DocumentImage)) : "/img/no-image.png",
                       Customer = a.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ProfilePicture)) : "/img/no-image.png",
                       Name = a.CustomerName,
                       Policy = a.LineOfBusiness.Name,
                       Status = a.InvestigationCaseStatus.Name,
                       ServiceType = a.ClaimType.GetEnumDisplayName(),
                       Location = a.CaseLocations.Count == 0 ?
                       "<span class=\"badge badge-danger\"><img class=\"form-Image\" src=\"/img/timer.gif\" /> </span>" :
                       string.Join("", a.CaseLocations.Select(c => "<span class='badge badge-light'>" + c.InvestigationCaseSubStatus.Name + "-" + c.PinCode.Code + "</span> ")),
                       Created = a.Created.ToString("dd-MM-yyyy"),
                       timePending = DateTime.Now.Subtract(a.Created).Days == 0 ? "< 1" : DateTime.Now.Subtract(a.Created).Days.ToString()
                   })
                   .ToList();

                return Ok(response);
            }
            return BadRequest();
        }
    }
}