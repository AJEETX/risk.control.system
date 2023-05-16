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
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var inboxMessages = _context.ContactUsMessage.Where(c =>
            c.ReceipientEmail == applicationUser.Email && c.MessageStatus == MessageStatus.SENT && c.MessageStatus != MessageStatus.DELETED && c.MessageStatus != MessageStatus.TRASHDELETED);

            return View(inboxMessages.ToList());

        }
        public async Task<IActionResult> Trash()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var trash = _context.ContactUsMessage.Where(c =>
            c.ApplicationUser.Email == applicationUser.Email && c.MessageStatus == MessageStatus.DELETED && c.MessageStatus != MessageStatus.TRASHDELETED);

            return View(trash.ToList());
        }

        public async Task<IActionResult> Sent()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var inboxMessages = _context.ContactUsMessage.Where(c =>
            c.SenderEmail == applicationUser.Email && c.MessageStatus != MessageStatus.DRAFTED && c.MessageStatus != MessageStatus.DELETED && c.MessageStatus != MessageStatus.TRASHDELETED);

            return View(inboxMessages.ToList());
        }

        // GET: ContactMessage/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.ContactUsMessage == null)
            {
                return NotFound();
            }

            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var contactMessage = _context.ContactUsMessage.FirstOrDefault(c => c.ContactMessageId == id);

            if (contactMessage == null)
            {
                return NotFound();
            }

            if (contactMessage.Read == false)
            {
                applicationUser.ContactMessages.Remove(contactMessage);

                contactMessage.Read = true;
                contactMessage.ReceiveDate = DateTime.Now;

                applicationUser.ContactMessages.Add(contactMessage);
                _context.ApplicationUser.Update(applicationUser);
                _context.ContactUsMessage.Update(contactMessage);
                await _context.SaveChangesAsync();
            }

            return View(contactMessage);
        }

        public async Task<IActionResult> DraftIndex()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var inboxMessages = _context.ContactUsMessage.Where(c =>
             c.SenderEmail == applicationUser.Email && c.MessageStatus == MessageStatus.DRAFTED);

            return View(inboxMessages.ToList());
        }

        // GET: ContactMessage/Edit/5
        public async Task<IActionResult> Draft(string id)
        {
            if (id == null || _context.ContactUsMessage == null)
            {
                return NotFound();
            }

            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var contactMessage = _context.ContactUsMessage.FirstOrDefault(c => c.ContactMessageId == id);

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
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var existingContactMessage = _context.ContactUsMessage.FirstOrDefault(c => c.ContactMessageId == contactMessage.ContactMessageId);

            if (existingContactMessage is null)
            {
                contactMessage.SenderEmail = userEmail;
                contactMessage.SendDate = DateTime.Now;
                contactMessage.Priority = 0;
                contactMessage.Read = false;
                contactMessage.IsDraft = false;
                contactMessage.MessageStatus = MessageStatus.DRAFTED;
                contactMessage.ApplicationUserId = applicationUser.Id;
                applicationUser.ContactMessages.Add(contactMessage);
                _context.ContactUsMessage.Add(contactMessage);
            }
            else
            {
                applicationUser.ContactMessages.Remove(existingContactMessage);

                existingContactMessage.Subject = contactMessage.Subject;
                existingContactMessage.Message = contactMessage.Message;
                existingContactMessage.SenderEmail = userEmail;
                existingContactMessage.SendDate = DateTime.Now;
                existingContactMessage.Priority = 0;
                existingContactMessage.Read = false;
                existingContactMessage.MessageStatus = MessageStatus.DRAFTED;
                existingContactMessage.ApplicationUserId = applicationUser.Id;
                existingContactMessage.IsDraft = false;
                applicationUser.ContactMessages.Add(existingContactMessage);
                _context.ContactUsMessage.Update(existingContactMessage);
            }
            _context.ApplicationUser.Update(applicationUser);
            var rows = await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("mail drafted successfully!");
            return RedirectToAction(nameof(Index));
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
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var existingContactMessage = _context.ContactUsMessage.FirstOrDefault(c => c.ContactMessageId == contactMessage.ContactMessageId);

            if (existingContactMessage is null)
            {
                contactMessage.SenderEmail = userEmail;
                contactMessage.SendDate = DateTime.Now;
                contactMessage.Priority = 0;
                contactMessage.Read = false;
                contactMessage.IsDraft = false;
                contactMessage.MessageStatus = MessageStatus.SENT;
                contactMessage.ApplicationUserId = applicationUser.Id;
                applicationUser.ContactMessages.Add(contactMessage);
                _context.ContactUsMessage.Add(contactMessage);
            }
            else
            {
                applicationUser.ContactMessages.Remove(existingContactMessage);

                existingContactMessage.Subject = contactMessage.Subject;
                existingContactMessage.Message = contactMessage.Message;
                existingContactMessage.SenderEmail = userEmail;
                existingContactMessage.SendDate = DateTime.Now;
                existingContactMessage.Priority = 0;
                existingContactMessage.Read = false;
                existingContactMessage.MessageStatus = MessageStatus.SENT;
                existingContactMessage.ApplicationUserId = applicationUser.Id;
                existingContactMessage.IsDraft = false;
                applicationUser.ContactMessages.Add(existingContactMessage);
                _context.ContactUsMessage.Update(existingContactMessage);
            }
            _context.ApplicationUser.Update(applicationUser);
            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("mail sent successfully!");
            return RedirectToAction(nameof(Index));
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

            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var contactMessage = applicationUser.ContactMessages.FirstOrDefault(c => c.ContactMessageId == id);

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

            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var contactMessages = _context.ContactUsMessage
                .Where(m => messages.Contains(m.ContactMessageId));

            if (contactMessages == null)
            {
                return NotFound();
            }
            foreach (var contact in contactMessages)
            {
                contact.MessageStatus = MessageStatus.DELETED;
            }
            _context.ApplicationUser.Update(applicationUser);
            _context.ContactUsMessage.UpdateRange(contactMessages);
            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("mail(s) deleted successfully!");

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> TrashDelete(List<string> messages)
        {
            if (_context.ContactUsMessage == null)
            {
                return NotFound();
            }

            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var contactMessages = _context.ContactUsMessage
                .Where(m => messages.Contains(m.ContactMessageId));

            if (contactMessages == null)
            {
                return NotFound();
            }
            foreach (var contact in contactMessages)
            {
                contact.MessageStatus = MessageStatus.TRASHDELETED;
            }
            _context.ApplicationUser.Update(applicationUser);
            _context.ContactUsMessage.UpdateRange(contactMessages);
            await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("mail(s) deleted permanently successfully!");

            return RedirectToAction(nameof(Index));
        }

        private bool ContactMessageExists(string id)
        {
            return (_context.ContactUsMessage?.Any(e => e.ContactMessageId == id)).GetValueOrDefault();
        }
    }
}
