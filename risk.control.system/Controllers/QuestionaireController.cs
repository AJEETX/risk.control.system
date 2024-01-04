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
    public class QuestionaireController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuestionaireController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Questionaire
        public async Task<IActionResult> Index()
        {
            var userEmail = HttpContext?.User?.Identity?.Name;
            var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var model = await _context.ReportQuestionaire
                            .Include(d => d.ClientCompany)
                            .Where(c => c.ClientCompanyId == user.ClientCompanyId)?.ToListAsync();

            return View(model);
        }

        // GET: Questionaire/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.ReportQuestionaire == null)
            {
                return NotFound();
            }

            var reportQuestionaire = await _context.ReportQuestionaire
                .FirstOrDefaultAsync(m => m.ReportQuestionaireId == id);
            if (reportQuestionaire == null)
            {
                return NotFound();
            }

            return View(reportQuestionaire);
        }

        // GET: Questionaire/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Questionaire/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReportQuestionaireId,Question,Answer,Type,Optional,Question1,Question2,Question3,Question4,Created,Updated,UpdatedBy")] ReportQuestionaire reportQuestionaire)
        {
            if (ModelState.IsValid)
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == currentUserEmail);
                reportQuestionaire.ClientCompanyId = user.ClientCompanyId;
                reportQuestionaire.Updated = DateTime.Now;
                reportQuestionaire.UpdatedBy = currentUserEmail;
                _context.Add(reportQuestionaire);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(reportQuestionaire);
        }

        // GET: Questionaire/Edit/5
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
            return View(reportQuestionaire);
        }

        // POST: Questionaire/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("ReportQuestionaireId,Question,Answer,Type,Optional,Question1,Question2,Question3,Question4,Created,Updated,UpdatedBy")] ReportQuestionaire reportQuestionaire)
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
                    var user = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(u => u.Email == currentUserEmail);
                    reportQuestionaire.ClientCompanyId = user.ClientCompanyId;
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
            return View(reportQuestionaire);
        }

        // GET: Questionaire/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.ReportQuestionaire == null)
            {
                return NotFound();
            }

            var reportQuestionaire = await _context.ReportQuestionaire
                .FirstOrDefaultAsync(m => m.ReportQuestionaireId == id);
            if (reportQuestionaire == null)
            {
                return NotFound();
            }

            return View(reportQuestionaire);
        }

        // POST: Questionaire/Delete/5
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