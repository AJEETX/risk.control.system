using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Common;
using SmartBreadcrumbs.Attributes;

namespace risk.control.system.Controllers.CompanyAdmin
{
    [Breadcrumb("Company Settings ")]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME}")]
    public class SmsForCaseController(
        ApplicationDbContext context,
        IEncryptionService encryptionService
        ) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IEncryptionService _encryptionService = encryptionService;
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Profile));
        }

        [Breadcrumb("SMS for Case")]
        public IActionResult Profile()
        {
            return View();
        }
        public async Task<IActionResult> GetSms()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var user = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);

            var investigations = await _context.Investigations.Where(i => i.ClientCompanyId == user!.ClientCompanyId).ToListAsync();

            var listOfTasks = investigations.ConvertAll(i => i.Id);

            var data = await _context.SmsForCase
                .Where(u => u.ClientCompanyId == user!.ClientCompanyId || listOfTasks.Contains(u.InvestigationTaskId ?? 0))
                .Select(c => new
                {
                    Id = _encryptionService.ProtectId(c.Id),
                    c.SenderEmail,
                    c.RecepientEmail,
                    c.Message,
                    c.Status,
                    Updated = c.Updated ?? c.Created,
                }).OrderByDescending(o => o.Updated)
                .ToListAsync();

            return Json(new { data });
        }


        // GET: SmsForCaseController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

    }
}
