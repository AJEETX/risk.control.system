using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class ServiceReportTemplatesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceReportTemplatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ServiceReportTemplates
        public async Task<IActionResult> Index()
        {
            var userEmail = HttpContext?.User?.Identity?.Name;
            var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

            var applicationDbContext = _context.ServiceReportTemplate
                .Include(s => s.ClientCompany)
                .Include(s => s.InvestigationServiceType)
                .Include(s => s.LineOfBusiness)
                .Include(s => s.ClientCompany)
                .Include(s => s.ReportTemplate).Where(c => c.ClientCompanyId == user.ClientCompanyId);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ServiceReportTemplates/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.ServiceReportTemplate == null)
            {
                return NotFound();
            }

            var serviceReportTemplate = await _context.ServiceReportTemplate
                .Include(s => s.ClientCompany)
                .Include(s => s.InvestigationServiceType)
                .Include(s => s.LineOfBusiness)
                .Include(s => s.ReportTemplate)
                .FirstOrDefaultAsync(m => m.ServiceReportTemplateId == id);
            if (serviceReportTemplate == null)
            {
                return NotFound();
            }

            return View(serviceReportTemplate);
        }

        // GET: ServiceReportTemplates/Create
        public IActionResult Create()
        {
            var userEmail = HttpContext?.User?.Identity?.Name;
            var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);

            var model = new ServiceReportTemplate
            {
                ClientCompany = user.ClientCompany,
                ClientCompanyId = user.ClientCompanyId
            };

            //ViewData["ClientCompanyId"] = new SelectList(_context.ClientCompany, "ClientCompanyId", "Name");
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name");
            ViewData["ReportTemplateId"] = new SelectList(_context.ReportTemplate, "ReportTemplateId", "Name");
            return View(model);
        }

        // POST: ServiceReportTemplates/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceReportTemplateId,Name,ClientCompanyId,LineOfBusinessId,InvestigationServiceTypeId,ReportTemplateId,Created,Updated,UpdatedBy")] ServiceReportTemplate serviceReportTemplate, string ClientCompanyId)
        {
            if (ModelState.IsValid)
            {
                var userEmail = HttpContext?.User?.Identity?.Name;
                var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                serviceReportTemplate.ClientCompanyId = user.ClientCompanyId;
                serviceReportTemplate.Updated = DateTime.UtcNow;
                serviceReportTemplate.UpdatedBy = userEmail;
                _context.Add(serviceReportTemplate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", serviceReportTemplate.InvestigationServiceTypeId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", serviceReportTemplate.LineOfBusinessId);
            ViewData["ReportTemplateId"] = new SelectList(_context.ReportTemplate, "ReportTemplateId", "Name", serviceReportTemplate.ReportTemplateId);
            return View(serviceReportTemplate);
        }

        // GET: ServiceReportTemplates/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.ServiceReportTemplate == null)
            {
                return NotFound();
            }

            var serviceReportTemplate = await _context.ServiceReportTemplate.FindAsync(id);
            if (serviceReportTemplate == null)
            {
                return NotFound();
            }
            var lob = _context.LineOfBusiness.FirstOrDefault(l => l.LineOfBusinessId == serviceReportTemplate.LineOfBusinessId);
            var svc = _context.InvestigationServiceType
                .Include(b => b.LineOfBusiness)
                .Where(l => l.LineOfBusinessId == serviceReportTemplate.LineOfBusinessId);
            ViewData["InvestigationServiceTypeId"] = new SelectList(svc, "InvestigationServiceTypeId", "Name", serviceReportTemplate.InvestigationServiceTypeId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", serviceReportTemplate.LineOfBusinessId);
            ViewData["ReportTemplateId"] = new SelectList(_context.ReportTemplate, "ReportTemplateId", "Name", serviceReportTemplate.ReportTemplateId);
            return View(serviceReportTemplate);
        }

        // POST: ServiceReportTemplates/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("ServiceReportTemplateId,Name,ClientCompanyId,LineOfBusinessId,InvestigationServiceTypeId,ReportTemplateId,Created,Updated,UpdatedBy")] ServiceReportTemplate serviceReportTemplate)
        {
            if (id != serviceReportTemplate.ServiceReportTemplateId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userEmail = HttpContext?.User?.Identity?.Name;
                    var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                    serviceReportTemplate.ClientCompanyId = user.ClientCompanyId;
                    serviceReportTemplate.Updated = DateTime.UtcNow;
                    serviceReportTemplate.UpdatedBy = userEmail;
                    _context.Update(serviceReportTemplate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceReportTemplateExists(serviceReportTemplate.ServiceReportTemplateId))
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
            ViewData["InvestigationServiceTypeId"] = new SelectList(_context.InvestigationServiceType, "InvestigationServiceTypeId", "Name", serviceReportTemplate.InvestigationServiceTypeId);
            ViewData["LineOfBusinessId"] = new SelectList(_context.LineOfBusiness, "LineOfBusinessId", "Name", serviceReportTemplate.LineOfBusinessId);
            ViewData["ReportTemplateId"] = new SelectList(_context.ReportTemplate, "ReportTemplateId", "Name", serviceReportTemplate.ReportTemplateId);
            return View(serviceReportTemplate);
        }

        // GET: ServiceReportTemplates/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.ServiceReportTemplate == null)
            {
                return NotFound();
            }

            var serviceReportTemplate = await _context.ServiceReportTemplate
                .Include(s => s.InvestigationServiceType)
                .Include(s => s.LineOfBusiness)
                .Include(s => s.ReportTemplate)
                .FirstOrDefaultAsync(m => m.ServiceReportTemplateId == id);
            if (serviceReportTemplate == null)
            {
                return NotFound();
            }

            return View(serviceReportTemplate);
        }

        // POST: ServiceReportTemplates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.ServiceReportTemplate == null)
            {
                return Problem("Entity set 'ApplicationDbContext.ServiceReportTemplate'  is null.");
            }
            var serviceReportTemplate = await _context.ServiceReportTemplate.FindAsync(id);
            if (serviceReportTemplate != null)
            {
                _context.ServiceReportTemplate.Remove(serviceReportTemplate);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ServiceReportTemplateExists(string id)
        {
            return (_context.ServiceReportTemplate?.Any(e => e.ServiceReportTemplateId == id)).GetValueOrDefault();
        }
    }
}