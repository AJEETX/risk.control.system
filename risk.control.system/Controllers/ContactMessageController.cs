using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

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
            IQueryable<ContactMessage> applicationDbContext = _context.ContactUsMessage.Where(c => c.MessageStatus == MessageStatus.SENT || c.MessageStatus == MessageStatus.RECEIVED)
                .Include(c => c.ApplicationUser).OrderByDescending(o => o.SendDate);
            var currentUserEmail = HttpContext.User.Identity.Name;
            var currentUser = _context.ApplicationUser.FirstOrDefault(u => u.isSuperAdmin);
            if (currentUser != null)
            {
                return View(await applicationDbContext.ToListAsync());
            }
            applicationDbContext = applicationDbContext.Where(u => u.ReceipientEmail == currentUserEmail);
            return View(await applicationDbContext.ToListAsync());
        }
        public async Task<IActionResult> Trash()
        {
            IQueryable<ContactMessage> applicationDbContext = _context.ContactUsMessage.Where(c => c.MessageStatus == MessageStatus.DELETED)
                .Include(c => c.ApplicationUser).OrderByDescending(o => o.SendDate);
            var currentUserEmail = HttpContext.User.Identity.Name;
            var currentUser = _context.ApplicationUser.FirstOrDefault(u => u.isSuperAdmin);
            if (currentUser != null)
            {
                return View(await applicationDbContext.ToListAsync());
            }
            applicationDbContext = applicationDbContext.Where(u => u.ReceipientEmail == currentUserEmail);
            return View(await applicationDbContext.ToListAsync());
        }

        public async Task<IActionResult> Sent()
        {
            IQueryable<ContactMessage> applicationDbContext = _context.ContactUsMessage.Where(c => c.SendDate != null && c.MessageStatus == MessageStatus.SENT)
                .Include(c => c.ApplicationUser).OrderByDescending(o => o.SendDate);
            var currentUserEmail = HttpContext.User.Identity.Name;
            var adminUser = _context.ApplicationUser.FirstOrDefault(u => u.isSuperAdmin && currentUserEmail == u.Email);
            if (adminUser != null)
            {
                return View(await applicationDbContext.ToListAsync());
            }
            applicationDbContext = applicationDbContext.Where(u => u.SenderEmail == currentUserEmail);
            return View(await applicationDbContext.ToListAsync());
        }

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
                contactMessage.MessageStatus = MessageStatus.RECEIVED;
                contactMessage.ReceiveDate = DateTime.Now;
                _context.ContactUsMessage.Update(contactMessage);
                await _context.SaveChangesAsync();
            }

            return View(contactMessage);
        }

        public async Task<IActionResult> DraftIndex()
        {
            IQueryable<ContactMessage> applicationDbContext = _context.ContactUsMessage.Where(c => c.MessageStatus == MessageStatus.DRAFTED)
                .Include(c => c.ApplicationUser).OrderByDescending(o => o.SendDate);
            var userEmail = HttpContext.User.Identity.Name;
            var currentUser = _context.ApplicationUser.FirstOrDefault(u => u.isSuperAdmin);
            if (currentUser != null)
            {
                return View(await applicationDbContext.ToListAsync());
            }
            applicationDbContext = applicationDbContext.Where(u => u.SenderEmail == userEmail);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ContactMessage/Edit/5
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
            ViewData["ApplicationUserId"] = new SelectList(_context.ApplicationUser, "Id", "CountryId", contactMessage.ApplicationUserId);
            return View(contactMessage);
        }
        [HttpPost]
        public async Task<IActionResult> Draft(ContactMessage contactMessage)
        {
            var existingContactMessage = await _context.ContactUsMessage
                .Include(c => c.ApplicationUser)
                .FirstOrDefaultAsync(m => m.ContactMessageId == contactMessage.ContactMessageId);
            var userEmail = HttpContext.User.Identity.Name;
            var userData = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (userData != null)
            {
                return NotFound();
            }
            if (existingContactMessage is null)
            {
                contactMessage.ApplicationUser = userData;
                contactMessage.SenderEmail = userEmail;
                contactMessage.SendDate = DateTime.Now;
                contactMessage.Priority = 0;
                contactMessage.Read = false;
                contactMessage.MessageStatus = MessageStatus.DRAFTED;
                contactMessage.IsDraft = true;
                _context.ContactUsMessage.Add(contactMessage);
                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("mail drafted successfully!");
                return RedirectToAction(nameof(Index));
            }
            else
            {
                existingContactMessage.Subject = contactMessage.Subject;
                existingContactMessage.Message = contactMessage.Message;
                existingContactMessage.SenderEmail = userEmail;
                existingContactMessage.SendDate = DateTime.Now;
                existingContactMessage.Priority = 0;
                existingContactMessage.Read = false;
                contactMessage.MessageStatus = MessageStatus.DRAFTED;
                existingContactMessage.IsDraft = true;
                _context.ContactUsMessage.Update(existingContactMessage);
                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("mail drafted successfully!");
                return RedirectToAction(nameof(Index));
            }
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
        public async Task<IActionResult> Create(ContactMessage contactMessage)
        {
            var existingContactMessage = await _context.ContactUsMessage
                .Include(c => c.ApplicationUser)
                .FirstOrDefaultAsync(m => m.ContactMessageId == contactMessage.ContactMessageId);
            var userEmail = HttpContext.User.Identity.Name;

            var userData = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (userData == null)
            {
                return NotFound();
            }
            if (existingContactMessage is null)
            {
                contactMessage.SenderEmail = userEmail;
                contactMessage.SendDate = DateTime.Now;
                contactMessage.Priority = 0;
                contactMessage.Read = false;
                contactMessage.IsDraft = false;
                contactMessage.MessageStatus = MessageStatus.SENT;
                contactMessage.ApplicationUser = userData;
                _context.ContactUsMessage.Add(contactMessage);
                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("mail sent successfully!");
                return RedirectToAction(nameof(Index));
            }
            else
            {
                existingContactMessage.Subject = contactMessage.Subject;
                existingContactMessage.Message = contactMessage.Message;
                existingContactMessage.SenderEmail = userEmail;
                existingContactMessage.SendDate = DateTime.Now;
                existingContactMessage.Priority = 0;
                existingContactMessage.Read = false;
                contactMessage.MessageStatus = MessageStatus.SENT;
                contactMessage.ApplicationUser = userData;
                existingContactMessage.IsDraft = false;
                _context.ContactUsMessage.Update(existingContactMessage);
                await _context.SaveChangesAsync();
                toastNotification.AddSuccessToastMessage("mail sent successfully!");
                return RedirectToAction(nameof(Index));
            }
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
            foreach (var contact in contactMessages)
            {
                contact.MessageStatus = MessageStatus.DELETED;
            }
            _context.ContactUsMessage.UpdateRange(contactMessages);
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
