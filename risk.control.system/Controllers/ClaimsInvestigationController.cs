using System.Data;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;
using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

namespace risk.control.system.Controllers
{
    public class ClaimsInvestigationController : Controller
    {
        private readonly JsonSerializerOptions options = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };
        private readonly ApplicationDbContext _context;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly IMailboxService mailboxService;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IToastNotification toastNotification;

        public ClaimsInvestigationController(ApplicationDbContext context,
            IClaimsInvestigationService claimsInvestigationService,
            IMailboxService mailboxService,
            UserManager<ClientCompanyApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IToastNotification toastNotification)
        {
            _context = context;
            this.claimsInvestigationService = claimsInvestigationService;
            this.mailboxService = mailboxService;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.toastNotification = toastNotification;
        }

        // GET: ClaimsInvestigation
        public async Task<IActionResult> Index()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.ClientCompany)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CostCentre)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.InvestigationServiceType)
                .Include(c => c.LineOfBusiness)
                .Include(c => c.PinCode)
                .Include(c => c.State);
 
            ViewBag.HasClientCompany = true;
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i => 
                i.Name ==CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name== CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (userRole.Value.Contains(AppRoles.PortalAdmin.ToString()) || userRole.Value.Contains(AppRoles.ClientAdmin.ToString()))
            {
                return View(await applicationDbContext.ToListAsync());
            }
            if (userRole.Value.Contains(AppRoles.ClientCreator.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId);
            }
            else if (userRole.Value.Contains(AppRoles.ClientAssigner.ToString()))
            {
                applicationDbContext = applicationDbContext.Where(a => a.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId);
            }

            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var clientCompany = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            if (clientCompany == null)
            {
                ViewBag.HasClientCompany = false;
            }
            else
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == clientCompany.ClientCompanyId);
            }
            return View(await applicationDbContext.ToListAsync());
        }

        public async Task<IActionResult> CaseLocation(string id)
        {
            if (id == null)
            {
                toastNotification.AddErrorToastMessage("vendor not found!");
                return NotFound();
            }

            var applicationDbContext = _context.ClaimsInvestigation
               .Include(c => c.CaseLocations)
               .ThenInclude(c=>c.PincodeServices)
               .Include(c=>c.CaseLocations)
               .ThenInclude(c=>c.State)
               .Include(c => c.CaseLocations)
               .ThenInclude(c => c.District)
               .Include(c => c.CaseLocations)
               .ThenInclude(c => c.Country)
               .Include(c => c.BeneficiaryRelation)
               .Include(c => c.ClientCompany)
               .Include(c => c.CaseEnabler)
               .Include(c => c.CostCentre)
               .Include(c => c.Country)
               .Include(c => c.District)
               .Include(c => c.InvestigationCaseStatus)
               .Include(c => c.InvestigationCaseSubStatus)
               .Include(c => c.InvestigationServiceType)
               .Include(c => c.LineOfBusiness)
               .Include(c => c.PinCode)
               .Include(c => c.State)
                .FirstOrDefault(a => a.ClaimsInvestigationId == id);

            return View(applicationDbContext);
        }
        public async Task<IActionResult> Open()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.ClientCompany)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CostCentre)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.InvestigationServiceType)
                .Include(c => c.LineOfBusiness)
                .Include(c => c.PinCode)
                .Include(c => c.State);
            ViewBag.HasClientCompany = true;

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (userRole.Value.Contains(AppRoles.ClientCreator.ToString()) || userRole.Value.Contains(AppRoles.ClientAssigner.ToString()))
            {
                var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId));
            }
            else if (!userRole.Value.Contains(AppRoles.PortalAdmin.ToString()) && !userRole.Value.Contains(AppRoles.ClientAdmin.ToString()))
            {
                return View(new List<ClaimsInvestigation> { });
            }
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var clientCompany = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            if (clientCompany == null)
            {
                ViewBag.HasClientCompany = false;
            }
            else
            {
                applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == clientCompany.ClientCompanyId);
            }
            return View(await applicationDbContext.ToListAsync());
        }
        [HttpPost]
        public async Task<IActionResult> Assign(List<string> claims)
        {
            var initiatedStatus = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains(CONSTANTS.CASE_STATUS.INITIATED));
            if (initiatedStatus == null)
            {

                return RedirectToAction(nameof(Create));
            }
            
            await claimsInvestigationService.Assign(HttpContext.User.Identity.Name,claims);       
            
            await mailboxService.NotifyClaimAssignment(HttpContext.User.Identity.Name, claims);

            return RedirectToAction(nameof(Index));
        }
        // GET: ClaimsInvestigation/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.ClientCompany)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CostCentre)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationServiceType)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.LineOfBusiness)
                .Include(c => c.PinCode)
                .Include(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }

            return View(claimsInvestigation);
        }

        // GET: ClaimsInvestigation/Create
        public async Task<IActionResult> Create()
        {
            var userEmailToSend = string.Empty;
            var model = new ClaimsInvestigation { LineOfBusinessId = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == "claims").LineOfBusinessId };

            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            if (clientCompanyUser == null)
            {
                model.HasClientCompany = false;
                userEmailToSend = _context.ApplicationUser.FirstOrDefault(u => u.IsSuperAdmin).Email;
            }
            else
            {
                var assignerRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.ClientAssigner.ToString()));

                var assignerUsers = _context.ClientCompanyApplicationUser.Where(u => u.ClientCompanyId == clientCompanyUser.ClientCompanyId);

                foreach (var assignedUser in assignerUsers)
                {
                    var isTrue = await userManager.IsInRoleAsync(assignedUser, assignerRole.Name);
                    if (isTrue)
                    {
                        userEmailToSend = assignedUser.Email;
                        break;
                    }
                }


                model.ClientCompanyId = clientCompanyUser.ClientCompanyId;
            }


            //mailboxService.InsertMessage(new ContactMessage
            //{
            //    ApplicationUserId = clientCompanyUser != null ? clientCompanyUser.Id : _context.ApplicationUser.First(u => u.isSuperAdmin).Id,
            //    ReceipientEmail = userEmailToSend,
            //    Created = DateTime.UtcNow,
            //    Message = "start",
            //    Subject = "New case created: case Id = " + userEmailToSend,
            //    SenderEmail = clientCompanyUser != null ? clientCompanyUser.FirstName : _context.ApplicationUser.First(u => u.isSuperAdmin).FirstName,
            //    Priority = ContactMessagePriority.NORMAL,
            //    SendDate = DateTime.UtcNow,
            //    Updated = DateTime.UtcNow,
            //    Read = false,
            //    UpdatedBy = userEmail.Value
            //});



            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name");
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name");
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i => i.LineOfBusinessId == model.LineOfBusinessId), "InvestigationServiceTypeId", "Name", model.InvestigationServiceTypeId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name");
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name");
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name");
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
            return View(model);
        }

        // POST: ClaimsInvestigation/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClaimsInvestigation claimsInvestigation)
        {
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains("INITIATED"));
            if (status == null)
            {
                return View(claimsInvestigation);
            }
            var userEmail = HttpContext.User.Identity.Name;

            if (claimsInvestigation is not null)
            {
                await claimsInvestigationService.Create(userEmail, claimsInvestigation, Request.Form?.Files?.FirstOrDefault());

                await mailboxService.NotifyClaimCreation(userEmail, claimsInvestigation);
         
                toastNotification.AddSuccessToastMessage("case(s) created successfully!");

                return RedirectToAction(nameof(Index));
            }
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.InvestigationServiceTypeId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", claimsInvestigation.BeneficiaryRelationId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.CostCentreId);
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", claimsInvestigation.DistrictId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.LineOfBusinessId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", claimsInvestigation.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", claimsInvestigation.StateId);
            return View(claimsInvestigation);
        }

        // GET: ClaimsInvestigation/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation.FindAsync(id);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.InvestigationServiceTypeId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", claimsInvestigation.BeneficiaryRelationId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.CostCentreId);
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", claimsInvestigation.DistrictId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.LineOfBusinessId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", claimsInvestigation.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", claimsInvestigation.StateId);
            return View(claimsInvestigation);
        }

        // POST: ClaimsInvestigation/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ClaimsInvestigation claimsInvestigation)
        {
            if (id != claimsInvestigation.ClaimsInvestigationId)
            {
                return NotFound();
            }

            if (claimsInvestigation is not null)
            {
                try
                {
                    var user = User?.Claims.FirstOrDefault(u => u.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                    claimsInvestigation.Updated = DateTime.UtcNow;
                    claimsInvestigation.UpdatedBy = user;
                    IFormFile? claimDocument = Request.Form?.Files?.FirstOrDefault();
                    if (claimDocument is not null)
                    {
                        claimsInvestigation.Document = claimDocument;
                        using var dataStream = new MemoryStream();
                        await claimsInvestigation.Document.CopyToAsync(dataStream);
                        claimsInvestigation.DocumentImage = dataStream.ToArray();
                    }
                    _context.Update(claimsInvestigation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClaimsInvestigationExists(claimsInvestigation.ClaimsInvestigationId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.ClientCompanyId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", claimsInvestigation.InvestigationServiceTypeId);
            ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation, "BeneficiaryRelationId", "Name", claimsInvestigation.BeneficiaryRelationId);
            ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler, "CaseEnablerId", "Name", claimsInvestigation.CaseEnablerId);
            ViewData["CostCentreId"] = new SelectList(_context.CostCentre, "CostCentreId", "Name", claimsInvestigation.CostCentreId);
            ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name", claimsInvestigation.CountryId);
            ViewData["DistrictId"] = new SelectList(_context.District, "DistrictId", "Name", claimsInvestigation.DistrictId);
            ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.LineOfBusinessId);
            ViewData["PinCodeId"] = new SelectList(_context.PinCode, "PinCodeId", "Name", claimsInvestigation.PinCodeId);
            ViewData["StateId"] = new SelectList(_context.State, "StateId", "Name", claimsInvestigation.StateId);
            return View(claimsInvestigation);
        }

        // GET: ClaimsInvestigation/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.ClientCompany)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.CaseEnabler)
                .Include(c => c.CostCentre)
                .Include(c => c.Country)
                .Include(c => c.District)
                .Include(c => c.InvestigationServiceType)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.LineOfBusiness)
                .Include(c => c.PinCode)
                .Include(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);
            if (claimsInvestigation == null)
            {
                return NotFound();
            }

            return View(claimsInvestigation);
        }

        // POST: ClaimsInvestigation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.ClaimsInvestigation == null)
            {
                return Problem("Entity set 'ApplicationDbContext.ClaimsInvestigation'  is null.");
            }
            var claimsInvestigation = await _context.ClaimsInvestigation.FindAsync(id);
            if (claimsInvestigation != null)
            {
                var user = User?.Claims.FirstOrDefault(u => u.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                claimsInvestigation.Updated = DateTime.UtcNow;
                claimsInvestigation.UpdatedBy = user;
                _context.ClaimsInvestigation.Remove(claimsInvestigation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClaimsInvestigationExists(string id)
        {
            return (_context.ClaimsInvestigation?.Any(e => e.ClaimsInvestigationId == id)).GetValueOrDefault();
        }
    }
}
