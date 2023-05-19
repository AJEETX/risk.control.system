using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NToastNotify;

using NuGet.Packaging.Signing;

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

            return View(userMailbox.Inbox.OrderByDescending(o => o.SendDate).ToList());

        }
        public async Task<IActionResult> InboxDelete(List<long> messages)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox
                           .Include(m => m.Inbox)
                           .Include(m => m.Trash)
                           .FirstOrDefault(c => c.ApplicationUserId == applicationUser.Id);

            var userInboxMails = userMailbox.Inbox.Where(d => messages.Contains(d.InboxMessageId)).ToList();

            if (userInboxMails is not null && userInboxMails.Count > 0)
            {
                foreach (var message in userInboxMails)
                {
                    message.MessageStatus = MessageStatus.TRASHED;
                    userMailbox.Inbox.Remove(message);
                    var jsonMessage = JsonSerializer.Serialize(message, options);
                    TrashMessage trashMessage = JsonSerializer.Deserialize<TrashMessage>(jsonMessage, options);
                    userMailbox.Trash.Add(trashMessage);
                }
            }
            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage($" {rows} mail(s) trashed successfully!");

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> InboxDetails(long id)
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
                .FirstOrDefault(c => c.Name == applicationUser.Email);

            var userMessage = userMailbox.Inbox.FirstOrDefault(c => c.InboxMessageId == id);
            userMessage.Read = true;
            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            return View(userMessage);
        }
        public async Task<IActionResult> InboxDetailsDelete(long id)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox
                .Include(m => m.Inbox)
                .Include(m => m.Trash)
                .FirstOrDefault(c => c.Name == applicationUser.Email);

            var userInboxMessage = userMailbox.Inbox.FirstOrDefault(c => c.InboxMessageId == id);

            if (userInboxMessage is not null)
            {
                userInboxMessage.MessageStatus = MessageStatus.TRASHED;
                userMailbox.Inbox.Remove(userInboxMessage);
                var jsonMessage = JsonSerializer.Serialize(userInboxMessage, options);
                TrashMessage trashMessage = JsonSerializer.Deserialize<TrashMessage>(jsonMessage, options);
                userMailbox.Trash.Add(trashMessage);
            }

            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage($" {rows} mail trashed successfully!");

            return RedirectToAction(nameof(Index));
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
                    var jsonMessage = JsonSerializer.Serialize(message, options);
                    DeletedMessage deletedMessage = JsonSerializer.Deserialize<DeletedMessage>(jsonMessage, options);
                    userMailbox.Deleted.Add(deletedMessage);
                }
            }
            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage($" {rows} mail(s) deleted permanently successfully!");

            return RedirectToAction(nameof(Trash));
        }
        public async Task<IActionResult> TrashDetails(long id)
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
                .Include(m => m.Trash)
                .FirstOrDefault(c => c.Name == applicationUser.Email);

            var userMessage = userMailbox.Trash.FirstOrDefault(c => c.TrashMessageId == id);
            userMessage.Read = true;
            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            return View(userMessage);
        }
        public async Task<IActionResult> TrashDetailsDelete(long id)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox
                .Include(m => m.Trash)
                .FirstOrDefault(c => c.Name == applicationUser.Email);

            var userTrashMessage = userMailbox.Trash.FirstOrDefault(c => c.TrashMessageId == id);

            if (userTrashMessage is not null)
            {
                userTrashMessage.MessageStatus = MessageStatus.TRASHDELETED;
                userMailbox.Trash.Remove(userTrashMessage);
                var jsonMessage = JsonSerializer.Serialize(userTrashMessage, options);
                DeletedMessage trashMessage = JsonSerializer.Deserialize<DeletedMessage>(jsonMessage, options);
                userMailbox.Deleted.Add(trashMessage);
            }

            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage($" {rows} mail deleted permanently successfully!");

            return RedirectToAction(nameof(Trash));
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
        public async Task<IActionResult> SentDelete(List<long> messages)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox
                           .Include(m => m.Sent)
                           .Include(m => m.Deleted)
                           .FirstOrDefault(c => c.ApplicationUserId == applicationUser.Id);

            var userSentMails = userMailbox.Sent.Where(d => messages.Contains(d.SentMessageId)).ToList();

            if (userSentMails is not null && userSentMails.Count > 0)
            {
                foreach (var message in userSentMails)
                {
                    message.MessageStatus = MessageStatus.TRASHED;
                    userMailbox.Sent.Remove(message);
                    var jsonMessage = JsonSerializer.Serialize(message, options);
                    DeletedMessage deletedMessage = JsonSerializer.Deserialize<DeletedMessage>(jsonMessage, options);
                    userMailbox.Deleted.Add(deletedMessage);
                }
            }
            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage($" {rows} mail(s) trashed successfully!");

            return RedirectToAction(nameof(Sent));
        }
        public async Task<IActionResult> SentDetails(long id)
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
                .Include(m => m.Sent)
                .FirstOrDefault(c => c.Name == applicationUser.Email);

            var userMessage = userMailbox.Sent.FirstOrDefault(c => c.SentMessageId == id);
            userMessage.Read = true;
            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            return View(userMessage);
        }
        public async Task<IActionResult> SentDetailsDelete(long id)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox
                .Include(m => m.Sent)
                .Include(m => m.Trash)
                .FirstOrDefault(c => c.Name == applicationUser.Email);

            var userSentMessage = userMailbox.Sent.FirstOrDefault(c => c.SentMessageId == id);

            if (userSentMessage is not null)
            {
                userSentMessage.MessageStatus = MessageStatus.TRASHED;
                userMailbox.Sent.Remove(userSentMessage);
                var jsonMessage = JsonSerializer.Serialize(userSentMessage, options);
                TrashMessage trashMessage = JsonSerializer.Deserialize<TrashMessage>(jsonMessage, options);
                userMailbox.Trash.Add(trashMessage);
            }

            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage($" {rows} mail trashed successfully!");

            return RedirectToAction(nameof(Sent));
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
        public async Task<IActionResult> OutboxDelete(List<long> messages)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox
                           .Include(m => m.Outbox)
                           .Include(m => m.Trash)
                           .FirstOrDefault(c => c.ApplicationUserId == applicationUser.Id);

            var userOutboxMails = userMailbox.Outbox.Where(d => messages.Contains(d.OutboxMessageId)).ToList();

            if (userOutboxMails is not null && userOutboxMails.Count > 0)
            {
                foreach (var message in userOutboxMails)
                {
                    message.MessageStatus = MessageStatus.TRASHED;
                    userMailbox.Outbox.Remove(message);
                    var jsonMessage = JsonSerializer.Serialize(message, options);
                    TrashMessage trashedMessage = JsonSerializer.Deserialize<TrashMessage>(jsonMessage, options);
                    userMailbox.Trash.Add(trashedMessage);
                }
            }
            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage($" {rows} mail(s) trashed successfully!");

            return RedirectToAction(nameof(Outbox));
        }
        // GET: ContactMessage/Details/5
        public async Task<IActionResult> OutBoxDetails(long id)
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
                .Include(m => m.Outbox)
                .FirstOrDefault(c => c.Name == applicationUser.Email);

            var userMessage = userMailbox.Outbox.FirstOrDefault(c => c.OutboxMessageId == id);
            userMessage.Read = true;
            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            return View(userMessage);
        }
        public async Task<IActionResult> OutboxDetailsDelete(long id)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var userMailbox = _context.Mailbox
                .Include(m => m.Outbox)
                .FirstOrDefault(c => c.Name == applicationUser.Email);

            var userOutboxMessage = userMailbox.Outbox.FirstOrDefault(c => c.OutboxMessageId == id);

            if (userOutboxMessage is not null)
            {
                userOutboxMessage.MessageStatus = MessageStatus.TRASHED;
                userMailbox.Outbox.Remove(userOutboxMessage);
                var jsonMessage = JsonSerializer.Serialize(userOutboxMessage, options);
                TrashMessage trashMessage = JsonSerializer.Deserialize<TrashMessage>(jsonMessage, options);
                userMailbox.Trash.Add(trashMessage);
            }

            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage($" {rows} mail trashed successfully!");

            return RedirectToAction(nameof(Outbox));
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
            if (existingContactMessage != null)
            {
                userMailbox.Draft.Remove(existingContactMessage);
            }
            contactMessage.SenderEmail = userEmail;
            contactMessage.SendDate = DateTime.Now;
            contactMessage.Read = false;
            contactMessage.IsDraft = false;

            if (recepientMailbox is not null)
            {

                contactMessage.MessageStatus = MessageStatus.SENT;
                var jsonMessage = JsonSerializer.Serialize(contactMessage, options);
                SentMessage sentMessage = JsonSerializer.Deserialize<SentMessage>(jsonMessage, options);

                IFormFile? messageDocument = Request.Form?.Files?.FirstOrDefault();
                if (messageDocument is not null)
                {
                    var messageDocumentFileName = Path.GetFileNameWithoutExtension(messageDocument.FileName);
                    var extension = Path.GetExtension(messageDocument.FileName);

                    sentMessage.Document = (FormFile?)messageDocument;
                    using var dataStream = new MemoryStream();
                    await sentMessage.Document.CopyToAsync(dataStream);
                    sentMessage.Attachment = dataStream.ToArray();
                    sentMessage.FileType = messageDocument.ContentType;
                    sentMessage.Extension = extension;
                    sentMessage.AttachmentName = messageDocumentFileName;
                }
                userMailbox.Sent.Add(sentMessage);
                _context.Mailbox.Attach(userMailbox);
                _context.Mailbox.Update(userMailbox);
                InboxMessage inboxMessage = JsonSerializer.Deserialize<InboxMessage>(jsonMessage, options);

                if (messageDocument is not null)
                {
                    var messageDocumentFileName = Path.GetFileNameWithoutExtension(messageDocument.FileName);
                    var extension = Path.GetExtension(messageDocument.FileName);
                    inboxMessage.Document = (FormFile?)messageDocument;
                    using var dataStream = new MemoryStream();
                    await inboxMessage.Document.CopyToAsync(dataStream);
                    inboxMessage.Attachment = dataStream.ToArray();
                    inboxMessage.FileType = messageDocument.ContentType;
                    inboxMessage.Extension = extension;
                    inboxMessage.AttachmentName = messageDocumentFileName;
                }

                recepientMailbox.Inbox.Add(inboxMessage);
                _context.Mailbox.Attach(recepientMailbox);
                _context.Mailbox.Update(recepientMailbox);
                var rowse = await _context.SaveChangesAsync();

                toastNotification.AddSuccessToastMessage("mail sent successfully!");
                return RedirectToAction(nameof(Index));
            }
            else
            {
                IFormFile? messageDocument = Request.Form?.Files?.FirstOrDefault();
                if (messageDocument is not null)
                {
                    var messageDocumentFileName = Path.GetFileNameWithoutExtension(messageDocument.FileName);
                    var extension = Path.GetExtension(messageDocument.FileName);

                    contactMessage.Document = (FormFile?)messageDocument;
                    using var dataStream = new MemoryStream();
                    await contactMessage.Document.CopyToAsync(dataStream);
                    contactMessage.Attachment = dataStream.ToArray();
                    contactMessage.FileType = messageDocument.ContentType;
                    contactMessage.Extension = extension;
                    contactMessage.AttachmentName = messageDocumentFileName;
                }
                userMailbox.Outbox.Add(contactMessage);
                _context.Mailbox.Update(userMailbox);
                var rowse = await _context.SaveChangesAsync();

                toastNotification.AddErrorToastMessage("Error: recepient email incorrect!");
                return RedirectToAction(nameof(Create));
            }



        }

        public async Task<IActionResult> DownloadFileAttachment(int id)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var userMailbox = _context.Mailbox
                .Include(m => m.Inbox)
                .Include(m => m.Inbox)
                .Include(m => m.Sent)
                .Include(m => m.Trash)
                .Include(m => m.Draft)
                .FirstOrDefault(c => c.Name == applicationUser.Email);

            var InboxFile = userMailbox.Inbox.FirstOrDefault(c => c.InboxMessageId == id);

            if (InboxFile != null)
            {
                return File(InboxFile.Attachment, InboxFile.FileType, InboxFile.AttachmentName + InboxFile.Extension);
            }
            var OutboxFile = userMailbox.Outbox.FirstOrDefault(c => c.OutboxMessageId == id);
            if (OutboxFile != null)
            {
                return File(OutboxFile.Attachment, OutboxFile.FileType, OutboxFile.AttachmentName + OutboxFile.Extension);
            }
            var SentFile = userMailbox.Sent.FirstOrDefault(c => c.SentMessageId == id);
            if (SentFile != null)
            {
                return File(SentFile.Attachment, SentFile.FileType, SentFile.AttachmentName + SentFile.Extension);
            }

            var draftFile = userMailbox.Draft.FirstOrDefault(c => c.DraftMessageId == id);
            if (draftFile != null)
            {
                return File(draftFile.Attachment, draftFile.FileType, draftFile.AttachmentName + draftFile.Extension);
            }
            var TrashFile = userMailbox.Trash.FirstOrDefault(c => c.TrashMessageId == id);
            if (TrashFile != null)
            {
                return File(TrashFile.Attachment, TrashFile.FileType, TrashFile.AttachmentName + TrashFile.Extension);
            }

            return Problem();
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
                .Include(m => m.Inbox)
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
                    var jsonMessage = JsonSerializer.Serialize(message, options);
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
                    var jsonMessage = JsonSerializer.Serialize(message, options);
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
                    var jsonMessage = JsonSerializer.Serialize(message, options);
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
                    var jsonMessage = JsonSerializer.Serialize(message, options);
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
                    var jsonMessage = JsonSerializer.Serialize(message, options);
                    TrashMessage trashMessage = JsonSerializer.Deserialize<TrashMessage>(jsonMessage, options);
                    userMailbox.Trash.Add(trashMessage);
                }
            }
            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("mail(s) deleted successfully!");

            return RedirectToAction(nameof(Index));
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
                var jsonMessage = JsonSerializer.Serialize(contactMessage, options);
                DraftMessage draftMessage = JsonSerializer.Deserialize<DraftMessage>(jsonMessage);
                IFormFile? messageDocument = Request.Form?.Files?.FirstOrDefault();
                if (messageDocument is not null)
                {
                    var messageDocumentFileName = Path.GetFileNameWithoutExtension(messageDocument.FileName);
                    var extension = Path.GetExtension(messageDocument.FileName);

                    draftMessage.Document = (FormFile?)messageDocument;
                    using var dataStream = new MemoryStream();
                    await draftMessage.Document.CopyToAsync(dataStream);
                    draftMessage.Attachment = dataStream.ToArray();

                    draftMessage.FileType = messageDocument.ContentType;
                    draftMessage.Extension = extension;
                    draftMessage.AttachmentName = messageDocumentFileName;

                }
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
                IFormFile? messageDocument = Request.Form?.Files?.FirstOrDefault();
                if (messageDocument is not null)
                {
                    var messageDocumentFileName = Path.GetFileNameWithoutExtension(messageDocument.FileName);
                    var extension = Path.GetExtension(messageDocument.FileName);

                    draftMessage.Document = (FormFile?)messageDocument;
                    using var dataStream = new MemoryStream();
                    await draftMessage.Document.CopyToAsync(dataStream);
                    draftMessage.Attachment = dataStream.ToArray();

                    draftMessage.FileType = messageDocument.ContentType;
                    draftMessage.Extension = extension;
                    draftMessage.AttachmentName = messageDocumentFileName;
                }
                else
                {
                    draftMessage.Attachment = existingContactMessage.Attachment;
                }
                userMailbox.Draft.Add(draftMessage);
                _context.Mailbox.Attach(userMailbox);
                _context.Mailbox.Update(userMailbox);
            }
            var rows = await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage("mail drafted successfully!");
            return RedirectToAction(nameof(Index));
        }

    }
}
