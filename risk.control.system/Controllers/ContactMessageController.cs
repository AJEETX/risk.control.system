using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Services;

namespace risk.control.system.Controllers
{
    public class ContactMessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMailboxService mailboxService;
        private readonly IToastNotification toastNotification;

        public ContactMessageController(ApplicationDbContext context, IMailboxService mailboxService, IToastNotification toastNotification)
        {
            _context = context;
            this.mailboxService = mailboxService;
            this.toastNotification = toastNotification;
        }

        // GET: ContactMessage
        public async Task<IActionResult> Index()
        {
            IQueryable<ContactMessage> applicationDbContext = _context.ContactUsMessage.Include(c => c.ApplicationUser).OrderByDescending(o => o.SendDate);
            var user = HttpContext.User.Identity.Name;
            if (user == Applicationsettings.PORTAL_ADMIN.EMAIL || user == Applicationsettings.CLIENT_ADMIN.EMAIL)
            {
                return View(await applicationDbContext.ToListAsync());
            }
            applicationDbContext = applicationDbContext.Where(u => u.ApplicationUser.Email == user);
            return View(await applicationDbContext.ToListAsync());
        }

        //public IActionResult Details(string id)
        //{
        //    if (id != null)
        //    {
        //        var message = mailboxService.GetMessageById(id);
        //        if (message != null)
        //        {
        //            if (message.Read == false)
        //                mailboxService.MarkAsRead(id);

        //            return View(message);
        //        }
        //    }

        //    return RedirectToAction("List");
        //}
        // GET: ContactMessage/Details/5
        public async Task<IActionResult> Details(string id)
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

            if (contactMessage.Read == false)
            {
                contactMessage.Read = true;
                _context.ContactUsMessage.Update(contactMessage);
                await _context.SaveChangesAsync();
            }

            return View(contactMessage);
        }

        // GET: ContactMessage/Create
        public IActionResult Create()
        {
            ViewData["ApplicationUserId"] = new SelectList(_context.ApplicationUser, "Id", "CountryId");
            return View();
        }

        public async Task<IActionResult> Draft(string id)
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
        public async Task<IActionResult> DeleteConfirmed(List<string> messages)
        {
            if (_context.ContactUsMessage == null)
            {
                return NotFound();
            }

            var contactMessages = _context.ContactUsMessage
                .Include(c => c.ApplicationUser)
                .Where(m => messages.Contains(m.ContactMessageId));

            if (contactMessages == null)
            {
                return NotFound();
            }

            _context.ContactUsMessage.RemoveRange(contactMessages);
            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("mail(s) deleted successfully!");

            return RedirectToAction(nameof(Index));
        }

        private bool ContactMessageExists(string id)
        {
            return (_context.ContactUsMessage?.Any(e => e.ContactMessageId == id)).GetValueOrDefault();
        }
    }
}
