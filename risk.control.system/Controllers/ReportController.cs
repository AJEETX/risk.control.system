using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using SmartBreadcrumbs.Attributes;
using risk.control.system.Models.ViewModel;
using risk.control.system.Models;
using System.Text.RegularExpressions;
using risk.control.system.Helpers;
using risk.control.system.Services;
using risk.control.system.AppConstant;

namespace risk.control.system.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IHttpClientService httpClientService;
        private readonly HttpClient _httpClient;
        private Regex longLatRegex = new Regex("(?<lat>[-|+| ]\\d+.\\d+)\\s* \\/\\s*(?<lon>\\d+.\\d+)");

        public ReportController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IHttpClientService httpClientService)
        {
            this._context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.httpClientService = httpClientService;
            _httpClient = new HttpClient();
        }

        [Breadcrumb(title: " Completed", FromController = typeof(ClaimsInvestigationController))]
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
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.District)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.State)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.Country)
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
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DocumentIdReport)
                .Include(l => l.Vendor)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.ReportQuestionaire)
                .FirstOrDefaultAsync(l => l.ClaimsInvestigationId == id);

            if (claimsInvestigation == null)
            {
                return NotFound();
            }
            var invoice = _context.VendorInvoice.FirstOrDefault(i => i.ClaimReportId == location.ClaimReport.ClaimReportId);

            var model = new ClaimTransactionModel
            {
                ClaimsInvestigation = claimsInvestigation,
                Log = caseLogs,
                Location = location,
                VendorInvoice = invoice,
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> PrintReport(string id)
        {
            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .Include(c => c.CaseLocations)
                .ThenInclude(r => r.ClaimReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == id);

            var policy = claim.PolicyDetail;
            var customer = claim.CustomerDetail;
            var beneficiary = claim.CaseLocations.FirstOrDefault();
            var report = claim.CaseLocations.FirstOrDefault()?.ClaimReport;

            string folder = Path.Combine(webHostEnvironment.WebRootPath, "report");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var filename = "report" + id + ".pdf";

            var filePath = Path.Combine(webHostEnvironment.WebRootPath, "report", filename);

            ReportRunner.Run(webHostEnvironment.WebRootPath).Build(filePath); ;
            var memory = new MemoryStream();
            using var stream = new FileStream(filePath, FileMode.Open);
            await stream.CopyToAsync(memory);
            memory.Position = 0;
            return File(memory, "application/pdf", filename);
        }

        [HttpGet]
        public async Task<IActionResult> PrintPdfReport(string id)
        {
            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .Include(c => c.CaseLocations)
                .ThenInclude(r => r.ClaimReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == id);

            var policy = claim.PolicyDetail;
            var customer = claim.CustomerDetail;
            var beneficiary = claim.CaseLocations.FirstOrDefault();
            var report = claim.CaseLocations.FirstOrDefault()?.ClaimReport;

            string folder = Path.Combine(webHostEnvironment.WebRootPath, "report");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var filename = "report" + id + ".pdf";

            var filePath = Path.Combine(webHostEnvironment.WebRootPath, "report", filename);

            PdfReportRunner.Run(webHostEnvironment.WebRootPath).Build(filePath); ;
            var memory = new MemoryStream();
            using var stream = new FileStream(filePath, FileMode.Open);
            await stream.CopyToAsync(memory);
            memory.Position = 0;
            return File(memory, "application/pdf", filename);
        }

        public async Task<RootObject> GetAddress(string lat, string lon)
        {
            var jsonData = await _httpClient.GetStringAsync("http://nominatim.openstreetmap.org/reverse?format=json&lat=" + lat + "&lon=" + lon);

            var rootObject = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObject>(jsonData);
            return rootObject;
        }
    }
}