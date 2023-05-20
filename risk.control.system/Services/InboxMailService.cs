using System.Text.Json.Serialization;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IInboxMailService
    {
        Task<IEnumerable<InboxMessage>> GetAllUserInboxMessages(string userEmail);
        Task<int> InboxDelete(List<long> messages, long userId);
        Task<InboxMessage> GetMessagedetail(long messageId, string userEmail);
        Task<OutboxMessage> GetMessagedetailReply(long messageId, string userEmail, string actiontype);

        Task<bool> SendInboxDetailsReply(OutboxMessage contactMessage, string userEmail);
        Task<int> InboxDetailsDelete(long id, string userEmail);
        Task<bool> SendMessage(OutboxMessage contactMessage, string userEmail);
    }
    public class InboxMailService : IInboxMailService
    {
        private readonly JsonSerializerOptions options = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };
        private readonly ApplicationDbContext _context;

        public InboxMailService(ApplicationDbContext context)
        {
            this._context = context;
        }
        public async Task<IEnumerable<InboxMessage>> GetAllUserInboxMessages(string userEmail)
        {
            var userMailbox = _context.Mailbox.Include(m => m.Inbox).FirstOrDefault(c => c.Name == userEmail);
            return userMailbox.Inbox.OrderByDescending(o => o.SendDate).ToList();
        }

        public async Task<InboxMessage> GetMessagedetail(long messageId, string userEmail)
        {
            var userMailbox = _context.Mailbox
                .Include(m => m.Inbox)
                .FirstOrDefault(c => c.Name == userEmail);

            var userMessage = userMailbox.Inbox.FirstOrDefault(c => c.InboxMessageId == messageId);
            userMessage.Read = true;

            _context.Mailbox.Attach(userMailbox);
            _context.Mailbox.Update(userMailbox);
            var rows = await _context.SaveChangesAsync();
            return userMessage;
        }

        public async Task<OutboxMessage> GetMessagedetailReply(long messageId, string userEmail, string actiontype)
        {
            var userMailbox = _context.Mailbox
                .Include(m => m.Inbox)
                .FirstOrDefault(c => c.Name == userEmail);

            var userMessage = userMailbox.Inbox.FirstOrDefault(c => c.InboxMessageId == messageId);

            var userReplyMessage = new OutboxMessage
            {
                ReceipientEmail = userMessage.SenderEmail,
                SenderEmail = userEmail,
                Subject = actiontype + " :" + userMessage.Subject,
                Attachment = userMessage.Attachment,
                AttachmentName = userMessage.AttachmentName,
                Created = userMessage.Created,
                Extension = userMessage.Extension,
                FileType = userMessage.FileType,
                Message = userMessage.Message,
                Read = false,

            };
            return userReplyMessage;
        }

        public async Task<int> InboxDelete(List<long> messages, long userId)
        {
            var userMailbox = _context.Mailbox
               .Include(m => m.Inbox)
               .Include(m => m.Trash)
               .FirstOrDefault(c => c.ApplicationUserId == userId);

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
            return await _context.SaveChangesAsync();
        }

        public async Task<int> InboxDetailsDelete(long id, string userEmail)
        {
            var userMailbox = _context.Mailbox
                .Include(m => m.Inbox)
                .Include(m => m.Trash)
                .FirstOrDefault(c => c.Name == userEmail);

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
            return await _context.SaveChangesAsync();
        }

        public async Task<bool> SendInboxDetailsReply(OutboxMessage contactMessage, string userEmail)
        {
            var userMailbox = _context.Mailbox.AsNoTracking().Include(m => m.Inbox).Include(m => m.Sent).FirstOrDefault(c => c.Name == userEmail);

            var recepientMailbox = _context.Mailbox.FirstOrDefault(c => c.Name == contactMessage.ReceipientEmail);

            contactMessage.Read = false;
            contactMessage.SendDate = DateTime.Now;
            contactMessage.Updated = DateTime.Now;
            contactMessage.UpdatedBy = userEmail;
            contactMessage.SenderEmail = userEmail;

            if (recepientMailbox is not null)
            {
                //add to sender's sent box
                var jsonMessage = JsonSerializer.Serialize(contactMessage, options);
                SentMessage sentMessage = JsonSerializer.Deserialize<SentMessage>(jsonMessage, options);
                userMailbox.Sent.Add(sentMessage);
                _context.Mailbox.Attach(userMailbox);
                _context.Mailbox.Update(userMailbox);


                //add to receiver's inbox
                InboxMessage inboxMessage = JsonSerializer.Deserialize<InboxMessage>(jsonMessage, options);
                recepientMailbox.Inbox.Add(inboxMessage);
                _context.Mailbox.Attach(recepientMailbox);
                _context.Mailbox.Update(recepientMailbox);

                var rows = await _context.SaveChangesAsync();

                return true;
            }
            else
            {
                var jsonMessage = JsonSerializer.Serialize(contactMessage, options);
                OutboxMessage outboxMessage = JsonSerializer.Deserialize<OutboxMessage>(jsonMessage, options);
                userMailbox.Outbox.Add(outboxMessage);
                _context.Mailbox.Update(userMailbox);
                var rows = await _context.SaveChangesAsync();
            }
            return false;
        }

        public async Task<bool> SendMessage(OutboxMessage contactMessage, string userEmail)
        {
            var userMailbox = _context.Mailbox.Include(m => m.Sent).Include(m => m.Outbox).FirstOrDefault(c => c.Name == userEmail);

            var recepientMailbox = _context.Mailbox.FirstOrDefault(c => c.Name == contactMessage.ReceipientEmail);

            contactMessage.Read = false;
            contactMessage.SendDate = DateTime.Now;
            contactMessage.Updated = DateTime.Now;
            contactMessage.UpdatedBy = userEmail;
            contactMessage.SenderEmail = userEmail;
            if (recepientMailbox is not null)
            {
                //add to sender's sent box
                var jsonMessage = JsonSerializer.Serialize(contactMessage, options);
                SentMessage sentMessage = JsonSerializer.Deserialize<SentMessage>(jsonMessage, options);

                userMailbox.Sent.Add(sentMessage);
                _context.Mailbox.Attach(userMailbox);
                _context.Mailbox.Update(userMailbox);


                //add to receiver's inbox
                InboxMessage inboxMessage = JsonSerializer.Deserialize<InboxMessage>(jsonMessage, options);

                recepientMailbox.Inbox.Add(inboxMessage);
                _context.Mailbox.Attach(recepientMailbox);
                _context.Mailbox.Update(recepientMailbox);

                var rows = await _context.SaveChangesAsync();

                return true;
            }
            else
            {
                var jsonMessage = JsonSerializer.Serialize(contactMessage, options);
                OutboxMessage outboxMessage = JsonSerializer.Deserialize<OutboxMessage>(jsonMessage, options);
                userMailbox.Outbox.Add(outboxMessage);
                _context.Mailbox.Update(userMailbox);
                var rows = await _context.SaveChangesAsync();
            }
            return false;
        }
    }
}
