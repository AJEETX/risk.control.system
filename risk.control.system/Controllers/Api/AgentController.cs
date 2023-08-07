using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly IMailboxService mailboxService;

        public AgentController(ApplicationDbContext context, IClaimsInvestigationService claimsInvestigationService, IMailboxService mailboxService)
        {
            this._context = context;
            this.claimsInvestigationService = claimsInvestigationService;
            this.mailboxService = mailboxService;
        }

        [AllowAnonymous]
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("This is a test endpoint");
        }

        [AllowAnonymous]
        [HttpGet("agent")]
        public async Task<IActionResult> Index(string email = "agent@agency1.com")
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.ClaimReport)
                .Include(c => c.ClientCompany)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CaseLocations)
                .ThenInclude(c => c.Vendor)
                .Include(c => c.Vendor)
                .Include(c => c.CostCentre)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.InvestigationServiceType)
            .Include(c => c.LineOfBusiness)
            .Include(c => c.PinCode)
            .Include(c => c.State);

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == email);

            if (vendorUser != null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.CaseLocations.Any(c => c.VendorId == vendorUser.VendorId));
                var claimsAssigned = new List<ClaimsInvestigation>();

                foreach (var item in applicationDbContext)
                {
                    item.CaseLocations = item.CaseLocations.Where(c => c.VendorId == vendorUser.VendorId
                        && c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId
                        && c.AssignedAgentUserEmail == email)?.ToList();
                    if (item.CaseLocations.Any())
                    {
                        claimsAssigned.Add(item);
                    }
                }
                var claim2Agent = claimsAssigned
                    .Select(c =>
                new
                {
                    claimId = c.ClaimsInvestigationId,
                    CustomerName = c.CustomerName,
                    CustomerEmail = email,
                    PolicyNumber = c.ContractNumber,
                    c.Gender,
                    c.Addressline,
                    c.PinCode.Code,
                    Country = c.Country.Name,
                    State = c.State.Name,
                    District = c.District.Name,
                    c.Description,
                    Locations = c.CaseLocations.Select(l => new { l.CaseLocationId, l.BeneficiaryName, l.Addressline, l.Addressline2, l.PinCode.Code, District = l.District.Name, State = l.State.Name, })
                });
                return Ok(claim2Agent);
            }
            return Unauthorized();
        }

        [AllowAnonymous]
        [HttpGet("get")]
        public async Task<IActionResult> Get(string email, string claimId)
        {
            var claimsInvestigation = _context.ClaimsInvestigation
                .Include(c => c.LineOfBusiness)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId
                && c.CurrentUserEmail == email);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var claimCase = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.ClaimReport)
                .Include(c => c.District)
                .Include(c => c.State)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimId
                && c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId
                    );
            return Ok(new ClaimsInvestigationVendorsModel { CaseLocation = claimCase, ClaimsInvestigation = claimsInvestigation });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Post(string email, string remarks, string claimId, long locId)
        {
            await claimsInvestigationService.SubmitToVendorSupervisor(email, locId, claimId, remarks);

            await mailboxService.NotifyClaimReportSubmitToVendorSupervisor(email, claimId, locId);

            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("Vendors")]
        public async Task<IActionResult> Vendors()
        {
            var applicationDbContext = await _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes).ToListAsync();

            var data = applicationDbContext.Select(a => new VendorData
            {
                Image = a.DocumentImage,
                Name = a.Name,
                Code = a.Code,
                PhoneNumber = a.PhoneNumber,
                Email = a.Email,
                Addressline = a.Addressline,
                State = a.State.Name,
                Created = a.Created.ToString("dd/MM/yyyy")
            });

            var response = new VendorDataDataTable
            {
                data = data.ToList()
            };
            return Ok(response);
        }
    }

    public class VendorData
    {
        public byte[]? Image { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Addressline { get; set; }
        public string State { get; set; }
        public string Created { get; set; }
    }

    public class VendorDataDataTable
    {
        public List<VendorData> data { get; set; }
    }
}