﻿using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IMailboxService
    {
        /// <summary>
        /// Get all ContactMessage
        /// </summary>
        /// <returns>List of ContactMessage entities</returns>
        IList<ContactMessage> GetAllMessages();
        IList<ContactMessage> GetAllMessages(string email);

        /// <summary>
        /// Get ContactMessage using id
        /// </summary>
        /// <param name="id">ContactMessage id</param>
        /// <returns>ContactMessage entity</returns>
        ContactMessage GetMessageById(string id);

        /// <summary>
        /// Insert ContactMessage
        /// </summary>
        /// <param name="message">ContactMessage entity</param>
        void InsertMessage(ContactMessage message);

        /// <summary>
        /// Update ContactMessage
        /// </summary>
        /// <param name="message">ContactMessage entity</param>
        void UpdateMessage(ContactMessage message);

        /// <summary>
        /// Delete ContactMessage
        /// </summary>
        /// <param name="ids">List of ContactMessage ids</param>
        void DeleteMessages(IList<string> ids);

        /// <summary>
        /// Mark the ContactMessage as read
        /// </summary>
        /// <param name="id">ContactMessage id</param>
        void MarkAsRead(string id);
    }
    public class MailboxService : IMailboxService
    {
        #region Fields 
        private readonly IRepository<ContactMessage> _contactUsRepository;

        #endregion

        #region Constructor

        public MailboxService(IRepository<ContactMessage> contactUsRepository)
        {
            _contactUsRepository = contactUsRepository;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get all ContactMessage
        /// </summary>
        /// <returns>List of ContactMessage entities</returns>
        public IList<ContactMessage> GetAllMessages()
        {
            var entities = _contactUsRepository.GetAll()
                .OrderByDescending(x => x.SendDate)
                .ToList();

            return entities;
        }

        /// <summary>
        /// Get ContactMessage using id
        /// </summary>
        /// <param name="id">ContactMessage id</param>
        /// <returns>ContactMessage entity</returns>
        public ContactMessage GetMessageById(string id)
        {
            return _contactUsRepository.FindByExpression(x => x.ContactMessageId == id);
        }

        /// <summary>
        /// Insert ContactMessage
        /// </summary>
        /// <param name="message">ContactMessage entity</param>
        public void InsertMessage(ContactMessage message)
        {
            if (message == null)
                throw new ArgumentException("message");

            _contactUsRepository.Insert(message);
            _contactUsRepository.SaveChanges();
        }

        /// <summary>
        /// Update ContactMessage
        /// </summary>
        /// <param name="message">ContactMessage entity</param>
        public void UpdateMessage(ContactMessage message)
        {
            if (message == null)
                throw new ArgumentException("message");

            _contactUsRepository.Update(message);
            _contactUsRepository.SaveChanges();
        }

        /// <summary>
        /// Delete ContactMessage
        /// </summary>
        /// <param name="ids">List of ContactMessage ids</param>
        public void DeleteMessages(IList<string> ids)
        {
            if (ids == null)
                throw new ArgumentNullException("ids");

            foreach (var id in ids)
                _contactUsRepository.Delete(GetMessageById(id));

            _contactUsRepository.SaveChanges();
        }

        /// <summary>
        /// Mark the ContactMessage as read
        /// </summary>
        /// <param name="id">ContactMessage id</param>
        public void MarkAsRead(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException("id");

            var message = GetMessageById(id);
            message.Read = true;

            _contactUsRepository.Update(message);
            _contactUsRepository.SaveChanges();
        }

        public IList<ContactMessage> GetAllMessages(string email)
        {
            return _contactUsRepository.FindManyByExpression(users => users.Email == email).OrderByDescending(x => x.SendDate).ToList();
        }

        #endregion
    }
}