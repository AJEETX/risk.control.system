
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

public class SmsForCasesController : Controller
{
    private readonly ApplicationDbContext _context;

    public SmsForCasesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: SMSFORCASES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.SmsForCase.ToListAsync());
    }

    // GET: SMSFORCASES/Details/5
    public async Task<IActionResult> Details(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var smsforcase = await _context.SmsForCase
            .FirstOrDefaultAsync(m => m.Id == id);
        if (smsforcase == null)
        {
            return NotFound();
        }

        return View(smsforcase);
    }

    // GET: SMSFORCASES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: SMSFORCASES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,ClientCompanyId,SenderEmail,RecepientEmail,Message,InvestigationTaskId,InvestigationTasks,Created,IsUpdated,Updated,CreatedUser,UpdatedBy")] SmsForCase smsforcase)
    {
        if (ModelState.IsValid)
        {
            _context.Add(smsforcase);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(smsforcase);
    }

    // GET: SMSFORCASES/Edit/5
    public async Task<IActionResult> Edit(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var smsforcase = await _context.SmsForCase.FindAsync(id);
        if (smsforcase == null)
        {
            return NotFound();
        }
        return View(smsforcase);
    }

    // POST: SMSFORCASES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long? id, [Bind("Id,ClientCompanyId,SenderEmail,RecepientEmail,Message,InvestigationTaskId,InvestigationTasks,Created,IsUpdated,Updated,CreatedUser,UpdatedBy")] SmsForCase smsforcase)
    {
        if (id != smsforcase.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(smsforcase);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SmsForCaseExists(smsforcase.Id))
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
        return View(smsforcase);
    }

    // GET: SMSFORCASES/Delete/5
    public async Task<IActionResult> Delete(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var smsforcase = await _context.SmsForCase
            .FirstOrDefaultAsync(m => m.Id == id);
        if (smsforcase == null)
        {
            return NotFound();
        }

        return View(smsforcase);
    }

    // POST: SMSFORCASES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long? id)
    {
        var smsforcase = await _context.SmsForCase.FindAsync(id);
        if (smsforcase != null)
        {
            _context.SmsForCase.Remove(smsforcase);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool SmsForCaseExists(long? id)
    {
        return _context.SmsForCase.Any(e => e.Id == id);
    }
}
