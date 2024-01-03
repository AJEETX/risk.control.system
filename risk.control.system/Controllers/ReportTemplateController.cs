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
    public class ReportTemplateController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportTemplateController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ReportTemplate
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ReportTemplate.Include(r => r.DigitalIdReport).Include(r => r.DocumentIdReport);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ReportTemplate/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.ReportTemplate == null)
            {
                return NotFound();
            }

            var reportTemplate = await _context.ReportTemplate
                .Include(r => r.DigitalIdReport)
                .Include(r => r.DocumentIdReport)
                .FirstOrDefaultAsync(m => m.ReportTemplateId == id);
            if (reportTemplate == null)
            {
                return NotFound();
            }

            return View(reportTemplate);
        }

        // GET: ReportTemplate/Create
        public IActionResult Create()
        {
            ViewData["DigitalIdReportId"] = new SelectList(_context.DigitalIdReport, "DigitalIdReportId", "ReportType");
            ViewData["DocumentIdReportId"] = new SelectList(_context.DocumentIdReport, "DocumentIdReportId", "DocumentIdReportType");
            return View();
        }

        // POST: ReportTemplate/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReportTemplateId,Name,DigitalIdReportId,DocumentIdReportId,Created,Updated,UpdatedBy")] ReportTemplate reportTemplate)
        {
            if (ModelState.IsValid)
            {
                _context.Add(reportTemplate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DigitalIdReportId"] = new SelectList(_context.DigitalIdReport, "DigitalIdReportId", "DigitalIdReportId", reportTemplate.DigitalIdReportId);
            ViewData["DocumentIdReportId"] = new SelectList(_context.DocumentIdReport, "DocumentIdReportId", "DocumentIdReportId", reportTemplate.DocumentIdReportId);
            return View(reportTemplate);
        }

        // GET: ReportTemplate/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.ReportTemplate == null)
            {
                return NotFound();
            }

            var reportTemplate = await _context.ReportTemplate.FindAsync(id);
            if (reportTemplate == null)
            {
                return NotFound();
            }
            ViewData["DigitalIdReportId"] = new SelectList(_context.DigitalIdReport, "DigitalIdReportId", "DigitalIdReportId", reportTemplate.DigitalIdReportId);
            ViewData["DocumentIdReportId"] = new SelectList(_context.DocumentIdReport, "DocumentIdReportId", "DocumentIdReportId", reportTemplate.DocumentIdReportId);
            return View(reportTemplate);
        }

        // POST: ReportTemplate/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("ReportTemplateId,Name,DigitalIdReportId,DocumentIdReportId,Created,Updated,UpdatedBy")] ReportTemplate reportTemplate)
        {
            if (id != reportTemplate.ReportTemplateId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reportTemplate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReportTemplateExists(reportTemplate.ReportTemplateId))
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
            ViewData["DigitalIdReportId"] = new SelectList(_context.DigitalIdReport, "DigitalIdReportId", "DigitalIdReportId", reportTemplate.DigitalIdReportId);
            ViewData["DocumentIdReportId"] = new SelectList(_context.DocumentIdReport, "DocumentIdReportId", "DocumentIdReportId", reportTemplate.DocumentIdReportId);
            return View(reportTemplate);
        }

        // GET: ReportTemplate/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.ReportTemplate == null)
            {
                return NotFound();
            }

            var reportTemplate = await _context.ReportTemplate
                .Include(r => r.DigitalIdReport)
                .Include(r => r.DocumentIdReport)
                .FirstOrDefaultAsync(m => m.ReportTemplateId == id);
            if (reportTemplate == null)
            {
                return NotFound();
            }

            return View(reportTemplate);
        }

        // POST: ReportTemplate/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.ReportTemplate == null)
            {
                return Problem("Entity set 'ApplicationDbContext.ReportTemplate'  is null.");
            }
            var reportTemplate = await _context.ReportTemplate.FindAsync(id);
            if (reportTemplate != null)
            {
                _context.ReportTemplate.Remove(reportTemplate);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReportTemplateExists(string id)
        {
            return (_context.ReportTemplate?.Any(e => e.ReportTemplateId == id)).GetValueOrDefault();
        }
    }
}