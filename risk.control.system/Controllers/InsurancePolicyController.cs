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
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var model = claimPolicyService.AddClaimPolicy(userEmail.Value);

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

        [Breadcrumb(title: " Edit Policy", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
        public async Task<IActionResult> EditPolicy(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .FirstOrDefaultAsync(i => i.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
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

        // GET: ClaimsInvestigation/Delete/5
        [Breadcrumb(title: " Delete", FromAction = "Index", FromController = typeof(InsuranceClaimsController))]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }
            var model = await investigationReportService.GetClaimDetails(id);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        // POST: ClaimsInvestigation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ClaimTransactionModel model)
        {
            if (model is null)
            {
                toastNotification.AddAlertToastMessage(string.Format("<i class='far fa-file-powerpoint'></i> Err to delete !"));
                return RedirectToAction(nameof(ClaimsInvestigationController.Draft), "ClaimsInvestigation");
            }
            var claimsInvestigation = await _context.ClaimsInvestigation.FindAsync(model.ClaimsInvestigation.ClaimsInvestigationId);
            string userEmail = HttpContext?.User?.Identity.Name;
            claimsInvestigation.Updated = DateTime.UtcNow;
            claimsInvestigation.UpdatedBy = userEmail;
            claimsInvestigation.Deleted = true;
            _context.ClaimsInvestigation.Update(claimsInvestigation);
            await _context.SaveChangesAsync();
            notifyService.Custom("Claim deleted", 3, "red", "far fa-file-powerpoint");
            return RedirectToAction(nameof(ClaimsInvestigationController.Draft), "ClaimsInvestigation");
        }
    }
}