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
    public class ReportQuestionairesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportQuestionairesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ReportQuestionaires
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ReportQuestionaire.Include(r => r.ReportTemplate);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ReportQuestionaires/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.ReportQuestionaire == null)
            {
                return NotFound();
            }

            var reportQuestionaire = await _context.ReportQuestionaire
                .Include(r => r.ReportTemplate)
                .FirstOrDefaultAsync(m => m.ReportQuestionaireId == id);
            if (reportQuestionaire == null)
            {
                return NotFound();
            }

            return View(reportQuestionaire);
        }

        // GET: ReportQuestionaires/Create
        public IActionResult Create()
        {
            ViewData["ReportTemplateId"] = new SelectList(_context.ReportTemplate, "ReportTemplateId", "ReportTemplateId");
            return View();
        }

        // POST: ReportQuestionaires/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReportQuestionaireId,Question,Answer,Type,Optional,Question1,Question2,Question3,Question4,ReportTemplateId,Created,Updated,UpdatedBy")] ReportQuestionaire reportQuestionaire)
        {
            if (ModelState.IsValid)
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;

                reportQuestionaire.Updated = DateTime.Now;
                reportQuestionaire.UpdatedBy = currentUserEmail;
                _context.Add(reportQuestionaire);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ReportTemplateId"] = new SelectList(_context.ReportTemplate, "ReportTemplateId", "ReportTemplateId", reportQuestionaire.ReportTemplateId);
            return View(reportQuestionaire);
        }

        // GET: ReportQuestionaires/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.ReportQuestionaire == null)
            {
                return NotFound();
            }

            var reportQuestionaire = await _context.ReportQuestionaire.FindAsync(id);
            if (reportQuestionaire == null)
            {
                return NotFound();
            }
            ViewData["ReportTemplateId"] = new SelectList(_context.ReportTemplate, "ReportTemplateId", "ReportTemplateId", reportQuestionaire.ReportTemplateId);
            return View(reportQuestionaire);
        }

        // POST: ReportQuestionaires/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("ReportQuestionaireId,Question,Answer,Type,Optional,Question1,Question2,Question3,Question4,ReportTemplateId,Created,Updated,UpdatedBy")] ReportQuestionaire reportQuestionaire)
        {
            if (id != reportQuestionaire.ReportQuestionaireId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUserEmail = HttpContext.User?.Identity?.Name;

                    reportQuestionaire.Updated = DateTime.Now;
                    reportQuestionaire.UpdatedBy = currentUserEmail;
                    _context.Update(reportQuestionaire);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReportQuestionaireExists(reportQuestionaire.ReportQuestionaireId))
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
            ViewData["ReportTemplateId"] = new SelectList(_context.ReportTemplate, "ReportTemplateId", "ReportTemplateId", reportQuestionaire.ReportTemplateId);
            return View(reportQuestionaire);
        }

        // GET: ReportQuestionaires/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.ReportQuestionaire == null)
            {
                return NotFound();
            }

            var reportQuestionaire = await _context.ReportQuestionaire
                .Include(r => r.ReportTemplate)
                .FirstOrDefaultAsync(m => m.ReportQuestionaireId == id);
            if (reportQuestionaire == null)
            {
                return NotFound();
            }

            return View(reportQuestionaire);
        }

        // POST: ReportQuestionaires/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.ReportQuestionaire == null)
            {
                return Problem("Entity set 'ApplicationDbContext.ReportQuestionaire'  is null.");
            }
            var reportQuestionaire = await _context.ReportQuestionaire.FindAsync(id);
            if (reportQuestionaire != null)
            {
                _context.ReportQuestionaire.Remove(reportQuestionaire);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReportQuestionaireExists(string id)
        {
          return (_context.ReportQuestionaire?.Any(e => e.ReportQuestionaireId == id)).GetValueOrDefault();
        }
    }
}
