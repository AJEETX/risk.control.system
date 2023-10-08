using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using System.Linq;
using SmartBreadcrumbs.Attributes;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            this._context = context;
        }

        [Breadcrumb(title: " Investigation Report", FromController = typeof(ClaimsInvestigationController))]
        public IActionResult Index()
        {
            return View();
        }

        [Breadcrumb(" Detail")]
        public async Task<IActionResult> Detail(string id)
        {
            if (id == null || _context.ClaimsInvestigation == null)
            {
                return NotFound();
            }

            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == id)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.ClientCompany)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.CaseEnabler)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.CostCentre)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.InvestigationCaseSubStatus)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.PinCode)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.BeneficiaryRelation)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.Country)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.District)
              .Include(c => c.InvestigationCaseStatus)
              .Include(c => c.InvestigationCaseSubStatus)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.InvestigationServiceType)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.LineOfBusiness)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.PinCode)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == id);

            var location = await _context.CaseLocation
                .Include(l => l.ClaimReport)
                .Include(l => l.Vendor)
                .FirstOrDefaultAsync(l => l.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = caseLogs,
                Location = location
            };

            if (location.ClaimReport.LocationLongLat != null)
            {
                var longLat = location.ClaimReport.LocationLongLat.IndexOf("/");
                var longitude = location.ClaimReport.LocationLongLat.Substring(0, longLat)?.Trim();
                var latitude = location.ClaimReport.LocationLongLat.Substring(longLat + 1)?.Trim();
                var longLatString = longitude + "," + latitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=14&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.LocationUrl = url;
            }
            else
            {
                ViewBag.LocationUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            }
            if (location.ClaimReport.OcrLongLat != null)
            {
                var longLat = location.ClaimReport.OcrLongLat.IndexOf("/");
                var longitude = location.ClaimReport.OcrLongLat.Substring(0, longLat)?.Trim();
                var latitude = location.ClaimReport.OcrLongLat.Substring(longLat + 1)?.Trim();
                var longLatString = longitude + "," + latitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=14&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
                ViewBag.OcrLocationUrl = url;
            }
            else
            {
                ViewBag.OcrLocationUrl = "https://maps.googleapis.com/maps/api/staticmap?center=32.661839,-97.263680&zoom=14&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C32.661839,-97.263680&key=AIzaSyDXQq3xhrRFxFATfPD4NcWlHLE8NPkzH2s";
            }
            return View(model);
        }
    }
}