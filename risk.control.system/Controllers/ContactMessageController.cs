using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class ContactMessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toastNotification;
        JsonSerializerOptions options = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };
        public ContactMessageController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
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
            var userMailbox = _context.Mailbox.Include(m => m.Inbox).FirstOrDefault(c => c.Name == applicationUser.Email);

            return View(userMailbox.Inbox.OrderByDescending(o=>o.SendDate).ToList());

        }
        public async Task<IActionResult> Trash()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox.Include(m => m.Trash).FirstOrDefault(c => c.Name == applicationUser.Email);

            return View(userMailbox.Trash.OrderByDescending(o => o.SendDate).ToList());
        }

        public async Task<IActionResult> Sent()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox.Include(m => m.Sent).FirstOrDefault(c => c.Name == applicationUser.Email);

            return View(userMailbox.Sent.OrderByDescending(o => o.SendDate).ToList());
        }
        public async Task<IActionResult> Outbox()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox.Include(m => m.Outbox).FirstOrDefault(c => c.Name == applicationUser.Email);

            return View(userMailbox.Outbox.OrderByDescending(o => o.SendDate).ToList());
        }

        // GET: ContactMessage/Details/5
        public async Task<IActionResult> Details(long id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox
                .Include(m => m.Inbox)
                .Include(m => m.Outbox)
                .Include(m => m.Sent)
                .Include(m => m.Trash)
                .Include(m => m.Draft)
                .FirstOrDefault(c => c.Name == applicationUser.Email);

            var userMessage = userMailbox.Inbox.FirstOrDefault(c => c.InboxMessageId == id);

            OutboxMessage outBoxessage = default!;
   
            if (userMessage is not null && userMessage.Read == false)
            {
                userMessage.Read = true;
                var jsonMessage = JsonSerializer.Serialize(userMessage, options);
                outBoxessage = JsonSerializer.Deserialize<OutboxMessage>(jsonMessage);

            }
            var userDraftMessage = userMailbox.Draft.FirstOrDefault(c => c.DraftMessageId == id);

            if (userDraftMessage is not null && userDraftMessage.Read == false)
            {
                //userMailbox.Draft.Remove(userDraftMessage);
                userDraftMessage.Read = true;
                var jsonMessage = JsonSerializer.Serialize(userDraftMessage, options);
                outBoxessage = JsonSerializer.Deserialize<OutboxMessage>(jsonMessage);
                //userMailbox.Draft.Add(userDraftMessage);
            }
            var userSentMessage = userMailbox.Sent.FirstOrDefault(c => c.SentMessageId == id);

            if (userSentMessage is not null && userSentMessage.Read == false)
            {
                //userMailbox.Sent.Remove(userSentMessage);
                userSentMessage.Read = true;
                var jsonMessage = JsonSerializer.Serialize(userSentMessage, options);
                outBoxessage = JsonSerializer.Deserialize<OutboxMessage>(jsonMessage);
                //userMailbox.Sent.Add(userSentMessage);
            }
            var userOutboxMessage = userMailbox.Outbox.FirstOrDefault(c => c.OutboxMessageId == id);

            if (userOutboxMessage is not null && userOutboxMessage.Read == false)
            {
                //userMailbox.Outbox.Remove(userOutboxMessage);
                userOutboxMessage.Read = true;
                var jsonMessage = JsonSerializer.Serialize(userOutboxMessage, options);
                outBoxessage = JsonSerializer.Deserialize<OutboxMessage>(jsonMessage);
                //userMailbox.Outbox.Add(userOutboxMessage);

            }
            var userTrashMessage = userMailbox.Trash.FirstOrDefault(c => c.TrashMessageId == id);

            if (userTrashMessage is not null && userTrashMessage.Read == false)
            {
                //userMailbox.Trash.Remove(userTrashMessage);
                userTrashMessage.Read = true;
                var jsonMessage = JsonSerializer.Serialize(userTrashMessage, options);
                outBoxessage = JsonSerializer.Deserialize<OutboxMessage>(jsonMessage);
                //userMailbox.Trash.Add(userTrashMessage);

            }

            var userDeletedMessage = userMailbox.Deleted.FirstOrDefault(c => c.DeletedMessageId == id);

            if (userDeletedMessage is not null && userDeletedMessage.Read == false)
            {
                userDeletedMessage.Read = true;
                var jsonMessage = JsonSerializer.Serialize(userDeletedMessage, options);
                outBoxessage = JsonSerializer.Deserialize<OutboxMessage>(jsonMessage);
            }

            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            return View(outBoxessage);
        }

        public async Task<IActionResult> DraftIndex()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox.Include(m => m.Draft).FirstOrDefault(c => c.Name == applicationUser.Email);

            return View(userMailbox.Draft.OrderByDescending(o => o.SendDate).ToList());
        }

        // GET: ContactMessage/Edit/5
        public async Task<IActionResult> Draft(long id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox
                .Include(m => m.Draft)
                .FirstOrDefault(c => c.Name == applicationUser.Email);

            var userDraftMessage = userMailbox.Draft.FirstOrDefault(c => c.DraftMessageId == id);

            if (userDraftMessage is not null)
            {
                userDraftMessage.Read = true;
            }
 
            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            return View(userDraftMessage);
        }
        [HttpPost]
        public async Task<IActionResult> Draft(DraftMessage contactMessage)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox.Include(m => m.Draft).FirstOrDefault(c => c.Name == applicationUser.Email);

            var existingContactMessage = userMailbox.Draft.FirstOrDefault(d => d.DraftMessageId == contactMessage.DraftMessageId);

            if (existingContactMessage is null)
            {
                contactMessage.SenderEmail = userEmail;
                contactMessage.SendDate = DateTime.Now;
                contactMessage.Priority = 0;
                contactMessage.Read = false;
                contactMessage.MessageStatus = MessageStatus.DRAFTED;
                var jsonMessage = JsonSerializer.Serialize(contactMessage,options);
                DraftMessage draftMessage = JsonSerializer.Deserialize<DraftMessage>(jsonMessage);
                userMailbox.Draft.Add(draftMessage);
                _context.Mailbox.Attach(userMailbox);
                _context.Mailbox.Update(userMailbox);
            }
            else
            {
                existingContactMessage.Subject = contactMessage?.Subject;
                existingContactMessage.Message = contactMessage?.Message;
                existingContactMessage.SenderEmail = userEmail;
                existingContactMessage.SendDate = DateTime.Now;
                existingContactMessage.Priority = 0;
                existingContactMessage.Read = false;
                existingContactMessage.MessageStatus = MessageStatus.DRAFTED;
                var jsonMessage = JsonSerializer.Serialize(contactMessage, options);
                DraftMessage draftMessage = JsonSerializer.Deserialize<DraftMessage>(jsonMessage);
                userMailbox.Draft.Add(draftMessage);
                _context.Mailbox.Attach(userMailbox);
                _context.Mailbox.Update(userMailbox);
            }
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
        public async Task<IActionResult> Create(OutboxMessage contactMessage)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var userMailbox = _context.Mailbox.Include(m => m.Draft).FirstOrDefault(c => c.Name == applicationUser.Email);
        
            var existingContactMessage = userMailbox.Draft.FirstOrDefault(d => d.DraftMessageId == contactMessage.OutboxMessageId);

            var recepientMailbox = _context.Mailbox.Include(m => m.Inbox).FirstOrDefault(c => c.Name == contactMessage.ReceipientEmail);

            if (existingContactMessage is null)
            {
                contactMessage.SenderEmail = userEmail;
                contactMessage.SendDate = DateTime.Now;
                contactMessage.Read = false;
                contactMessage.IsDraft = false;
 
                if(recepientMailbox is not null )
                {
                    contactMessage.MessageStatus = MessageStatus.SENT;
                    var jsonMessage = JsonSerializer.Serialize(contactMessage);
                    SentMessage sentMessage = JsonSerializer.Deserialize<SentMessage>(jsonMessage, options);
                    userMailbox.Sent.Add(sentMessage);
                    _context.Mailbox.Attach(userMailbox);
                    _context.Mailbox.Update(userMailbox);
                    InboxMessage inboxMessage = JsonSerializer.Deserialize<InboxMessage>(jsonMessage, options);
                    recepientMailbox.Inbox.Add(inboxMessage);
                    _context.Mailbox.Attach(recepientMailbox);
                    _context.Mailbox.Update(recepientMailbox);
                }
                else
                {
                    userMailbox.Outbox.Add(contactMessage);
                    _context.Mailbox.Update(userMailbox);
                }
 
                var rowse = await _context.SaveChangesAsync();
            }
            else
            {
                contactMessage.SenderEmail = userEmail;
                contactMessage.SendDate = DateTime.Now;
                contactMessage.Read = false;
                contactMessage.IsDraft = false;

                if (recepientMailbox is not null)
                {
                    contactMessage.MessageStatus = MessageStatus.SENT;
                    var jsonMessage = JsonSerializer.Serialize(contactMessage);
                    SentMessage sentMessage = JsonSerializer.Deserialize<SentMessage>(jsonMessage, options);
                    userMailbox.Sent.Add(sentMessage);
                    _context.Mailbox.Attach(userMailbox);
                    _context.Mailbox.Update(userMailbox);
                    InboxMessage inboxMessage = JsonSerializer.Deserialize<InboxMessage>(jsonMessage, options);
                    recepientMailbox.Inbox.Add(inboxMessage);
                    _context.Mailbox.Attach(recepientMailbox);
                    _context.Mailbox.Update(recepientMailbox);
                }
                else
                {
                    userMailbox.Outbox.Add(contactMessage);
                    _context.Mailbox.Update(userMailbox);
                }

                var rowse = await _context.SaveChangesAsync();
            }

            toastNotification.AddSuccessToastMessage("mail sent successfully!");
            return RedirectToAction(nameof(Index));
        }


        // POST: ContactMessage/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.


        // POST: ContactMessage/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(List<long> messages)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox
                .Include(m => m.Inbox)
                .Include(m => m.Outbox)
                .Include(m => m.Sent)
                .Include(m => m.Trash)
                .Include(m => m.Draft)
                .FirstOrDefault(c => c.ApplicationUserId == applicationUser.Id);

            var userInboxMails = userMailbox.Inbox.Where(d => messages.Contains(d.InboxMessageId)).ToList();
            var userOutboxMails = userMailbox.Outbox.Where(d => messages.Contains(d.OutboxMessageId)).ToList();
            var userDraftMails = userMailbox.Draft.Where(d => messages.Contains(d.DraftMessageId)).ToList();
            var userSentMails = userMailbox.Sent.Where(d => messages.Contains(d.SentMessageId)).ToList();
            var userTrashMails = userMailbox.Trash.Where(d => messages.Contains(d.TrashMessageId)).ToList();



            if (userInboxMails is not null && userInboxMails.Count > 0)
            {
                foreach (var message in userInboxMails)
                {
                    message.MessageStatus = MessageStatus.TRASHED;
                    userMailbox.Inbox.Remove(message);
                    var jsonMessage = JsonSerializer.Serialize(message);
                    TrashMessage trashMessage = JsonSerializer.Deserialize<TrashMessage>(jsonMessage, options);
                    userMailbox.Trash.Add(trashMessage);
                }
            }
            else if (userOutboxMails is not null && userOutboxMails.Count > 0)
            {
                foreach (var message in userOutboxMails)
                {
                    message.MessageStatus = MessageStatus.TRASHED;
                    userMailbox.Outbox.Remove(message);
                    var jsonMessage = JsonSerializer.Serialize(message);
                    TrashMessage trashMessage = JsonSerializer.Deserialize<TrashMessage>(jsonMessage, options);
                    userMailbox.Trash.Add(trashMessage);
                }
            }
            else if (userDraftMails is not null && userDraftMails.Count > 0)
            {
                foreach (var message in userDraftMails)
                {
                    message.MessageStatus = MessageStatus.TRASHED;
                    userMailbox.Draft.Remove(message);
                    var jsonMessage = JsonSerializer.Serialize(message);
                    TrashMessage trashMessage = JsonSerializer.Deserialize<TrashMessage>(jsonMessage, options);
                    userMailbox.Trash.Add(trashMessage);
                }
            }
            else if (userSentMails is not null && userSentMails.Count > 0)
            {
                foreach (var message in userSentMails)
                {
                    message.MessageStatus = MessageStatus.TRASHED;
                    userMailbox.Sent.Remove(message);
                    var jsonMessage = JsonSerializer.Serialize(message);
                    TrashMessage trashMessage = JsonSerializer.Deserialize<TrashMessage>(jsonMessage, options);
                    userMailbox.Trash.Add(trashMessage);
                }
            }
            else if (userTrashMails is not null && userTrashMails.Count > 0)
            {
                foreach (var message in userTrashMails)
                {
                    message.MessageStatus = MessageStatus.TRASHED;
                    userMailbox.Trash.Remove(message);
                    var jsonMessage = JsonSerializer.Serialize(message);
                    TrashMessage trashMessage = JsonSerializer.Deserialize<TrashMessage>(jsonMessage, options);
                    userMailbox.Trash.Add(trashMessage);
                }
            }
            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("mail(s) deleted successfully!");

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> TrashDelete(List<long> messages)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox
                           .Include(m => m.Trash)
                           .Include(m => m.Deleted)
                           .FirstOrDefault(c => c.ApplicationUserId == applicationUser.Id);

            var userTrashMails = userMailbox.Trash.Where(d => messages.Contains(d.TrashMessageId)).ToList();

            if (userTrashMails is not null && userTrashMails.Count > 0)
            {
                foreach (var message in userTrashMails)
                {
                    message.MessageStatus = MessageStatus.TRASHDELETED;
                    userMailbox.Trash.Remove(message);
                    var jsonMessage = JsonSerializer.Serialize(message);
                    DeletedMessage deletedMessage = JsonSerializer.Deserialize<DeletedMessage>(jsonMessage, options);
                    userMailbox.Deleted.Add(deletedMessage);
                }
            }
            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage($" {rows} mail(s) deleted permanently successfully!");

            return RedirectToAction(nameof(Index));
        }
    }
}
