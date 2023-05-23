using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

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
        private readonly IInboxMailService inboxMailService;
        private readonly IToastNotification toastNotification;
        private readonly JsonSerializerOptions options = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };
        public ContactMessageController(ApplicationDbContext context, IInboxMailService inboxMailService, IToastNotification toastNotification)
        {
            _context = context;
            this.inboxMailService = inboxMailService;
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
            var userMailboxMessages = await inboxMailService.GetAllUserInboxMessages(userEmail);

            return View(userMailboxMessages.OrderBy(o=>o.SendDate));

        }
        public async Task<IActionResult> InboxDelete(List<long> messages)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var rows = await inboxMailService.InboxDelete(messages, applicationUser.Id);
            toastNotification.AddSuccessToastMessage($" {messages.Count} mail(s) trashed successfully!");

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
            var userMessage = await inboxMailService.GetInboxMessagedetail(id, userEmail);
            return View(userMessage);
        }
        public async Task<IActionResult> InboxDetailsReply(long id, string actiontype)
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
            var userMessage = await inboxMailService.GetInboxMessagedetailReply(id, userEmail, actiontype);
            ViewBag.ActionType = actiontype;
            return View(userMessage);
        }
        [HttpPost]
        public async Task<IActionResult> InboxDetailsReply(OutboxMessage contactMessage)
        {
            contactMessage.Message = HttpUtility.HtmlEncode(contactMessage.RawMessage);

            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }

            IFormFile? messageDocument = Request.Form?.Files?.FirstOrDefault();

            var mailSent = await inboxMailService.SendMessage(contactMessage, userEmail, messageDocument);

            if (mailSent)
            {
                toastNotification.AddSuccessToastMessage("mail sent successfully!");
                return RedirectToAction(nameof(Index));
            }
            else
            {
                toastNotification.AddErrorToastMessage("Error: recepient email incorrect!");
                return RedirectToAction(nameof(Create));
            }
        }
        public async Task<IActionResult> InboxDetailsDelete(long id)
        {
            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }
            var rows = await inboxMailService.InboxDetailsDelete(id, userEmail);
            toastNotification.AddSuccessToastMessage($"mail trashed successfully!");

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
                    if(message.Attachment?.Length>0)
                    {
                        //TO-DO
                    }
                    var jsonMessage = JsonSerializer.Serialize(message, options);
                    DeletedMessage deletedMessage = JsonSerializer.Deserialize<DeletedMessage>(jsonMessage, options);
                    userMailbox.Deleted.Add(deletedMessage);
                }
            }
            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            toastNotification.AddSuccessToastMessage($" {messages.Count} mail(s) deleted permanently successfully!");

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
                           .Include(m => m.Trash)
                           .FirstOrDefault(c => c.ApplicationUserId == applicationUser.Id);

            var userSentMails = userMailbox.Sent.Where(d => messages.Contains(d.SentMessageId)).ToList();

            if (userSentMails is not null && userSentMails.Count > 0)
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
        public async Task<IActionResult> SentDetailsReply(long id, string actiontype)
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
            var userMessage = await inboxMailService.GetSentMessagedetailReply(id, userEmail, actiontype);

            ViewBag.ActionType = actiontype;
            return View(userMessage);
        }

        [HttpPost]
        public async Task<IActionResult> SentDetailsReply(SentMessage contactMessage)
        {
            contactMessage.Message = HttpUtility.HtmlEncode(contactMessage.RawMessage);

            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }

            var userMailbox = _context.Mailbox.FirstOrDefault(c => c.Name == applicationUser.Email);

            var recepientMailbox = _context.Mailbox.FirstOrDefault(c => c.Name == contactMessage.ReceipientEmail);
            contactMessage.SenderEmail = userEmail;
            contactMessage.SendDate = DateTime.Now;
            contactMessage.Read = false;
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
                sentMessage.SendDate = DateTime.Now;
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
                inboxMessage.SendDate = DateTime.Now;
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

                var jsonMessage = JsonSerializer.Serialize(contactMessage, options);
                OutboxMessage outboxMessage = JsonSerializer.Deserialize<OutboxMessage>(jsonMessage, options);
                userMailbox.Outbox.Add(outboxMessage);
                _context.Mailbox.Update(userMailbox);
                var rowse = await _context.SaveChangesAsync();

                toastNotification.AddErrorToastMessage("Error: recepient email incorrect!");
                return RedirectToAction(nameof(Create));
            }
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
            contactMessage.Message = HttpUtility.HtmlEncode(contactMessage.RawMessage);

            var userEmail = HttpContext.User.Identity.Name;

            var applicationUser = _context.ApplicationUser.Where(u => u.Email == userEmail).FirstOrDefault();
            if (applicationUser == null)
            {
                return NotFound();
            }

            IFormFile? messageDocument = Request.Form?.Files?.FirstOrDefault();
            
            var mailSent = await inboxMailService.SendMessage(contactMessage, userEmail, messageDocument);

            if (mailSent)
            {
                toastNotification.AddSuccessToastMessage("mail sent successfully!");
                return RedirectToAction(nameof(Index));
            }
            else
            {
                toastNotification.AddErrorToastMessage("Error: recepient email incorrect!");
                return RedirectToAction(nameof(Create));
            }
        }


    }
}
