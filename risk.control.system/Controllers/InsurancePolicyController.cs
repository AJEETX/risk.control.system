using System.Security.Claims;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers
{
    public class InsurancePolicyController : Controller
    {
        private readonly IClaimPolicyService claimPolicyService;
        private readonly ApplicationDbContext _context;
        private readonly INotyfService notifyService;
        private readonly IToastNotification toastNotification;
        private readonly IInvestigationReportService investigationReportService;

        public InsurancePolicyController(IClaimPolicyService claimPolicyService, ApplicationDbContext context,
            INotyfService notifyService,

            IToastNotification toastNotification, IInvestigationReportService investigationReportService)
        {
            this.claimPolicyService = claimPolicyService;
            this._context = context;
            this.notifyService = notifyService;
            this.toastNotification = toastNotification;
            this.investigationReportService = investigationReportService;
        }

        [Breadcrumb(title: " Add Policy", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
        public IActionResult CreatePolicy()
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("Not Found!!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = claimPolicyService.AddClaimPolicy(currentUserEmail);

                ViewBag.ClientCompanyId = model.PolicyDetail.ClientCompanyId;

                ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name");
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i =>
                i.LineOfBusinessId == model.PolicyDetail.LineOfBusinessId).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", model.PolicyDetail.InvestigationServiceTypeId);
                ViewData["BeneficiaryRelationId"] = new SelectList(_context.BeneficiaryRelation.OrderBy(s => s.Code), "BeneficiaryRelationId", "Name");
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name");
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name");
                ViewData["CountryId"] = new SelectList(_context.Country, "CountryId", "Name");
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS!!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [Breadcrumb(title: " Edit Policy", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
        public async Task<IActionResult> EditPolicy(string id)
        {
            try
            {
                if (id == null || _context.ClaimsInvestigation == null)
                {
                    notifyService.Error("Not Found!!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var claimsInvestigation = await _context.ClaimsInvestigation
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.CustomerDetail)
                    .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

                if (claimsInvestigation == null)
                {
                    notifyService.Error("Not Found!!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name", claimsInvestigation.PolicyDetail.ClientCompanyId);
                ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType.Where(i =>
                i.LineOfBusinessId == claimsInvestigation.PolicyDetail.LineOfBusinessId).OrderBy(s => s.Code), "InvestigationServiceTypeId", "Name", claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
                ViewData["CaseEnablerId"] = new SelectList(_context.CaseEnabler.OrderBy(s => s.Code), "CaseEnablerId", "Name", claimsInvestigation.PolicyDetail.CaseEnablerId);
                ViewData["CostCentreId"] = new SelectList(_context.CostCentre.OrderBy(s => s.Code), "CostCentreId", "Name", claimsInvestigation.PolicyDetail.CostCentreId);
                ViewData["InvestigationCaseStatusId"] = new SelectList(_context.InvestigationCaseStatus, "InvestigationCaseStatusId", "Name", claimsInvestigation.InvestigationCaseStatusId);
                ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", claimsInvestigation.PolicyDetail.LineOfBusinessId);

                return View(claimsInvestigation);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS!!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        // GET: ClaimsInvestigation/Delete/5
        [Breadcrumb(title: " Delete", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (id == null || _context.ClaimsInvestigation == null)
                {
                    notifyService.Error("Not Found!!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("Not Found!!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await investigationReportService.GetClaimDetails(currentUserEmail, id);

                if (model == null)
                {
                    notifyService.Error("Not Found!!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS!!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [Breadcrumb(title: " Delete", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
        public async Task<IActionResult> DeleteManual(string id)
        {
            try
            {
                if (id == null || _context.ClaimsInvestigation == null)
                {
                    notifyService.Error("Not Found!!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("Not Found!!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var model = await investigationReportService.GetClaimDetails(currentUserEmail, id);

                if (model == null)
                {
                    notifyService.Error("Not Found!!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                return View(model);
            }
            catch (Exception)
            {
                notifyService.Error("OOPS!!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }

        [HttpPost, ActionName("DeleteManual")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteManualConfirmed(ClaimTransactionModel model)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("Not Found!!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (model is null)
                {
                    notifyService.Error("Not Found!!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var claimsInvestigation = await _context.ClaimsInvestigation.FindAsync(model.ClaimsInvestigation.ClaimsInvestigationId);
                if(claimsInvestigation == null)
                {
                    notifyService.Error("Not Found!!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                
                claimsInvestigation.Updated = DateTime.UtcNow;
                claimsInvestigation.UpdatedBy = currentUserEmail;
                claimsInvestigation.Deleted = true;
                _context.ClaimsInvestigation.Update(claimsInvestigation);
                await _context.SaveChangesAsync();
                notifyService.Custom("Claim deleted", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(ClaimsInvestigationController.Assigner), "ClaimsInvestigation");
            }
            catch (Exception)
            {
                notifyService.Error("OOPS!!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
            
        }
        // POST: ClaimsInvestigation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ClaimTransactionModel model)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("Not Found!!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                if (model is null)
                {
                    notifyService.Error("Not Found!!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var claimsInvestigation = await _context.ClaimsInvestigation.FindAsync(model.ClaimsInvestigation.ClaimsInvestigationId);
                if (claimsInvestigation == null)
                {
                    notifyService.Error("Not Found!!!..Contact IT support");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                claimsInvestigation.Updated = DateTime.UtcNow;
                claimsInvestigation.UpdatedBy = currentUserEmail;
                claimsInvestigation.Deleted = true;
                _context.ClaimsInvestigation.Update(claimsInvestigation);
                await _context.SaveChangesAsync();
                notifyService.Custom("Claim deleted", 3, "red", "far fa-file-powerpoint");
                return RedirectToAction(nameof(ClaimsInvestigationController.Assigner), "ClaimsInvestigation");
            }
            catch (Exception)
            {
                notifyService.Error("OOPS!!!..Contact IT support");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }
    }
}