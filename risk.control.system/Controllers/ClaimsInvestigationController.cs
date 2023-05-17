using System.Data;
using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class ClaimsInvestigationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IToastNotification toastNotification;

        public ClaimsInvestigationController(ApplicationDbContext context,
            UserManager<ClientCompanyApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IToastNotification toastNotification)
        {
            _context = context;
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

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (userRole.Value.Contains(AppRoles.ClientCreator.ToString()))
            {
                var status = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains("CREATED"));
                applicationDbContext = applicationDbContext.Where(a => a.InvestigationCaseSubStatusId == status.InvestigationCaseSubStatusId);
            }
            else if (userRole.Value.Contains(AppRoles.ClientAssigner.ToString()))
            {
                var status = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains("ASSIGNED_TO_ASSIGNER"));
                applicationDbContext = applicationDbContext.Where(a => a.InvestigationCaseSubStatusId == status.InvestigationCaseSubStatusId);
            }
            else if (!userRole.Value.Contains(AppRoles.PortalAdmin.ToString()) && !userRole.Value.Contains(AppRoles.ClientAdmin.ToString()))
            {
                return View(new List<ClaimsInvestigation> { });
            }
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var clientCompany = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            ViewBag.HasClientCompany = true;
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

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (userRole.Value.Contains(AppRoles.ClientCreator.ToString()) || userRole.Value.Contains(AppRoles.ClientAssigner.ToString()))
            {
                var status = _context.InvestigationCaseStatus.FirstOrDefault(i => !i.Name.Contains("FINISHED"));
                applicationDbContext = applicationDbContext.Where(a => a.InvestigationCaseStatusId == status.InvestigationCaseStatusId);
            }
            else if (!userRole.Value.Contains(AppRoles.PortalAdmin.ToString()) && !userRole.Value.Contains(AppRoles.ClientAdmin.ToString()))
            {
                return View(new List<ClaimsInvestigation> { });
            }
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var clientCompany = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            ViewBag.HasClientCompany = true;
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
            var status = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains("IN-PROGRESS"));
            if (status == null)
            {

                return RedirectToAction(nameof(Create));
            }
            if (claims is not null && claims.Count > 0)
            {
                var casesAssigned = _context.ClaimsInvestigation.Where(v => claims.Contains(v.ClaimsInvestigationCaseId));
                var user = User?.Claims.FirstOrDefault(u => u.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                foreach (var claimsInvestigation in casesAssigned)
                {
                    claimsInvestigation.Updated = DateTime.UtcNow;
                    claimsInvestigation.UpdatedBy = user;
                    claimsInvestigation.CurrentUserId = User?.Claims.FirstOrDefault(u => u.Type == ClaimTypes.NameIdentifier)?.Value;
                    claimsInvestigation.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains("IN-PROGRESS")).InvestigationCaseStatusId;
                    claimsInvestigation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains("ASSIGNED_TO_ASSIGNER")).InvestigationCaseSubStatusId;
                }
                _context.UpdateRange(casesAssigned);
                toastNotification.AddSuccessToastMessage("case(s) assigned successfully!");
                await _context.SaveChangesAsync();
                var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

                var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

                var assignerRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.ClientAssigner.ToString()));

                var assignerClaims = await roleManager.GetClaimsAsync(assignerRole);

                var allcompany = await userManager.GetUsersForClaimAsync(assignerClaims.FirstOrDefault());


                var assignerUsers = _context.ApplicationRole.FirstOrDefault(r => r.Name == assignerClaims.FirstOrDefault().Value);

                var companyUsers = _context.ClientCompanyApplicationUser.Where(c => c.ClientCompanyId == clientCompanyUser.ClientCompanyId);


                //var companyAssigners = _context.ClientCompanyApplicationUser.Where(u => u.)
                //mailboxService.InsertMessage(new MailboxMessage
                //{
                //    ReceipientEmail = "",
                //    Created = DateTime.UtcNow,
                //    Message = JsonSerializer.Serialize(casesAssigned),
                //    Subject = "New case created: case Id(s) = " + casesAssigned.Select(c => c.ClaimsInvestigationCaseId),
                //    SenderEmail = clientCompanyUser.FirstName + clientCompanyUser.LastName,
                //    Priority = ContactMessagePriority.HIGH,
                //    SendDate = DateTime.UtcNow,
                //    Updated = DateTime.UtcNow,
                //    Read = false,
                //    UpdatedBy = userEmail.Value
                //});

                return RedirectToAction(nameof(Index));
            }
            return Problem();
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
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationCaseId == id);
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
                userEmailToSend = _context.ApplicationUser.FirstOrDefault(u => u.isSuperAdmin).Email;
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
            if (claimsInvestigation is not null)
            {
                var user = User?.Claims.FirstOrDefault(u => u.Type == ClaimTypes.Email)?.Value;
                claimsInvestigation.Updated = DateTime.UtcNow;
                claimsInvestigation.UpdatedBy = user;
                claimsInvestigation.CurrentUserId = User?.Claims.FirstOrDefault(u => u.Type == ClaimTypes.NameIdentifier)?.Value;
                claimsInvestigation.InvestigationCaseStatusId = _context.InvestigationCaseStatus.FirstOrDefault(i => i.Name.Contains("INITIATED")).InvestigationCaseStatusId;
                claimsInvestigation.InvestigationCaseSubStatusId = _context.InvestigationCaseSubStatus.FirstOrDefault(i => i.Name.Contains("CREATED")).InvestigationCaseSubStatusId;
                IFormFile? claimDocument = Request.Form?.Files?.FirstOrDefault();
                if (claimDocument is not null)
                {
                    claimsInvestigation.Document = claimDocument;
                    using var dataStream = new MemoryStream();
                    await claimsInvestigation.Document.CopyToAsync(dataStream);
                    claimsInvestigation.DocumentImage = dataStream.ToArray();
                }
                _context.Add(claimsInvestigation);
                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("case(s) created successfully!");

                var userEmailToSend = string.Empty;

                var userClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

                var clientCompanyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userClaim.Value);
                if (clientCompanyUser == null)
                {
                    userEmailToSend = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userClaim.Value).Email;
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
                }

                //mailboxService.InsertMessage(new MailboxMessage
                //{
                //    ReceipientEmail = userEmailToSend,
                //    Created = DateTime.UtcNow,
                //    Message = JsonSerializer.Serialize(claimsInvestigation),
                //    Subject = "New case created: case Id = " + claimsInvestigation.ClaimsInvestigationCaseId,
                //    SenderEmail = clientCompanyUser.FirstName + clientCompanyUser.LastName,
                //    Priority = ContactMessagePriority.NORMAL,
                //    SendDate = DateTime.UtcNow,
                //    Updated = DateTime.UtcNow,
                //    Read = false,
                //    UpdatedBy = userClaim.Value
                //});
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
            if (id != claimsInvestigation.ClaimsInvestigationCaseId)
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
                    if (!ClaimsInvestigationExists(claimsInvestigation.ClaimsInvestigationCaseId))
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
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationCaseId == id);
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
            return (_context.ClaimsInvestigation?.Any(e => e.ClaimsInvestigationCaseId == id)).GetValueOrDefault();
        }
    }
}
