using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class ContactMessageController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactMessageController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ContactMessage
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ContactUsMessage.Include(c => c.ApplicationUser);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ContactMessage/Details/5
        public async Task<IActionResult> Details(string id)
        {
            //if (id == null || _context.ContactUsMessage == null)
            //{
            //    return NotFound();
            //}

            //var contactMessage = await _context.ContactUsMessage
            //    .Include(c => c.ApplicationUser)
            //    .FirstOrDefaultAsync(m => m.ContactMessageId == id);
            //if (contactMessage == null)
            //{
            //    return NotFound();
            //}

            return View(new ContactMessage { });
        }

        // GET: ContactMessage/Create
        public IActionResult Create()
        {
            ViewData["ApplicationUserId"] = new SelectList(_context.ApplicationUser, "Id", "CountryId");
            return View();
        }

        // POST: ContactMessage/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ContactMessageId,Name,Email,Title,Message,Read,Priority,SendDate,ReceiveDate,ApplicationUserId,Created,Updated,UpdatedBy")] ContactMessage contactMessage)
        {
            if (ModelState.IsValid)
            {
                _context.Add(contactMessage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ApplicationUserId"] = new SelectList(_context.ApplicationUser, "Id", "CountryId", contactMessage.ApplicationUserId);
            return View(contactMessage);
        }

        // GET: ContactMessage/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.ContactUsMessage == null)
            {
                return NotFound();
            }

            var contactMessage = await _context.ContactUsMessage.FindAsync(id);
            if (contactMessage == null)
            {
                return NotFound();
            }
            ViewData["ApplicationUserId"] = new SelectList(_context.ApplicationUser, "Id", "CountryId", contactMessage.ApplicationUserId);
            return View(contactMessage);
        }

        // POST: ContactMessage/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("ContactMessageId,Name,Email,Title,Message,Read,Priority,SendDate,ReceiveDate,ApplicationUserId,Created,Updated,UpdatedBy")] ContactMessage contactMessage)
        {
            if (id != contactMessage.ContactMessageId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(contactMessage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactMessageExists(contactMessage.ContactMessageId))
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
            ViewData["ApplicationUserId"] = new SelectList(_context.ApplicationUser, "Id", "CountryId", contactMessage.ApplicationUserId);
            return View(contactMessage);
        }

        // GET: ContactMessage/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.ContactUsMessage == null)
            {
                return NotFound();
            }

            var contactMessage = await _context.ContactUsMessage
                .Include(c => c.ApplicationUser)
                .FirstOrDefaultAsync(m => m.ContactMessageId == id);
            if (contactMessage == null)
            {
                return NotFound();
            }

            return View(contactMessage);
        }

        // POST: ContactMessage/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.ContactUsMessage == null)
            {
                return Problem("Entity set 'ApplicationDbContext.ContactUsMessage'  is null.");
            }
            var contactMessage = await _context.ContactUsMessage.FindAsync(id);
            if (contactMessage != null)
            {
                _context.ContactUsMessage.Remove(contactMessage);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactMessageExists(string id)
        {
            return (_context.ContactUsMessage?.Any(e => e.ContactMessageId == id)).GetValueOrDefault();
        }
    }
}
