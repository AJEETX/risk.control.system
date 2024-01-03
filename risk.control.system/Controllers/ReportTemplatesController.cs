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
    public class ReportTemplatesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportTemplatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ReportTemplates
        public async Task<IActionResult> Index()
        {
            return _context.ReportTemplate != null ?
                        View(await _context.ReportTemplate.ToListAsync()) :
                        Problem("Entity set 'ApplicationDbContext.ReportTemplate'  is null.");
        }

        // GET: ReportTemplates/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.ReportTemplate == null)
            {
                return NotFound();
            }

            var reportTemplate = await _context.ReportTemplate
                .FirstOrDefaultAsync(m => m.ReportTemplateId == id);
            if (reportTemplate == null)
            {
                return NotFound();
            }

            return View(reportTemplate);
        }

        // GET: ReportTemplates/Create
        public IActionResult Create()
        {
            var model = new ReportTemplate();

            return View(model);
        }

        // POST: ReportTemplates/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReportTemplateId,Name,Created,Updated,UpdatedBy")] ReportTemplate reportTemplate)
        {
            if (ModelState.IsValid)
            {
                _context.Add(reportTemplate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(reportTemplate);
        }

        // GET: ReportTemplates/Edit/5
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
            return View(reportTemplate);
        }

        // POST: ReportTemplates/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("ReportTemplateId,Name,Created,Updated,UpdatedBy")] ReportTemplate reportTemplate)
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
            return View(reportTemplate);
        }

        // GET: ReportTemplates/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.ReportTemplate == null)
            {
                return NotFound();
            }

            var reportTemplate = await _context.ReportTemplate
                .FirstOrDefaultAsync(m => m.ReportTemplateId == id);
            if (reportTemplate == null)
            {
                return NotFound();
            }

            return View(reportTemplate);
        }

        // POST: ReportTemplates/Delete/5
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