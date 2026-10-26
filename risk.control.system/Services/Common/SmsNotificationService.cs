using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Services.Common
{
    public interface ISmsNotificationService
    {
        Task<string> SendSms2Customer(string currentUser, long caseId, string sms);

        Task<string> SendSms2Beneficiary(string currentUser, long caseId, string sms);

        Task<List<CaseMessage>> GetSmsHistory(long caseId, bool isCustomer);
    }

    internal class SmsNotificationService(ApplicationDbContext context,
        ITinyUrlService tinyUrlService,
        ILogger<SmsNotificationService> logger,
        ISmsService SmsService) : ISmsNotificationService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ISmsService _smsService = SmsService;
        private readonly ITinyUrlService _tinyUrlService = tinyUrlService;
        private readonly ILogger<SmsNotificationService> _logger = logger;
        private static readonly string _logo = Applicationsettings.WEBSITE_SITE_URL;

        public async Task<string> SendSms2Customer(string currentUser, long caseId, string sms)
        {
            try
            {
                var recipient = await _context.CustomerDetail.AsNoTracking()
               .Include(c => c.Country)
               .FirstOrDefaultAsync(c => c.InvestigationTaskId == caseId);

                return await ProcessSmsSending(recipient?.Name, recipient?.PhoneNumber, recipient?.Country, currentUser, caseId, sms, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending SMS to Customer for Case {CaseId} by user {UserEmail}", caseId, currentUser);
                return string.Empty;
            }
        }

        public async Task<string> SendSms2Beneficiary(string currentUser, long caseId, string sms)
        {
            try
            {
                var recipient = await _context.BeneficiaryDetail.AsNoTracking()
                .Include(b => b.Country)
                .FirstOrDefaultAsync(c => c.InvestigationTaskId == caseId);

                return await ProcessSmsSending(recipient?.Name, recipient?.PhoneNumber, recipient?.Country, currentUser, caseId, sms, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending SMS to Beneficiary for Case {CaseId} by user {UserEmail}", caseId, currentUser);
                return string.Empty;
            }
        }

        private async Task<string> ProcessSmsSending(string? name, string? phone, Country? country, string currentUser, long caseId, string sms, bool isCustomer)
        {
            if (string.IsNullOrEmpty(name) || country == null) return string.Empty;

            var user = await _context.ApplicationUser.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == currentUser);

            if (user == null || (user.ClientCompanyId <= 0 && user.VendorId <= 0)) return string.Empty;

            var entityName = await GetEntityNameAsync(user);
            var caseTask = await _context.Investigations
                .Include(c => c.CaseMessages)
                .Include(c => c.PolicyDetail)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (caseTask == null) return string.Empty;

            var message = $"Dear {name}\n" +
                          $"{sms}\n" +
                          $"Thanks\n" +
                          $"{user.FirstName} {user.LastName}\n" + // Fixed interpolation bug
                          $"Policy #:{caseTask.PolicyDetail?.ContractNumber}\n" +
                          $"{entityName}\n" +
                          $"{_logo}";

            var scheduleMessage = new CaseMessage
            {
                Message = sms,
                IsCustomer = isCustomer,
                InvestigationTaskId = caseId,
                RecepicientEmail = name,
                SenderEmail = user.Email,
                UpdatedBy = user.Email,
                Updated = DateTime.UtcNow
            };

            caseTask.CaseMessages!.Add(scheduleMessage);
            await _context.SaveChangesAsync(null, false);

            var fullMobile = $"+{country.ISDCode}{phone}";
            await _smsService.DoSendSmsAsync(country.Code, fullMobile, message);

            return name;
        }

        public async Task<List<CaseMessage>> GetSmsHistory(long caseId, bool isCustomer)
        {
            var messages = await _context.CaseMessages
                .Where(m => m.InvestigationTaskId == caseId && m.IsCustomer == isCustomer)
                .OrderByDescending(m => m.Updated) // Show newest first
                .ToListAsync();
            return messages;
        }

        private async Task<string> GetEntityNameAsync(ApplicationUser user)
        {
            if (user.ClientCompanyId > 0)
            {
                var entity = await _context.ClientCompany.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ClientCompanyId == user.ClientCompanyId);
                return entity?.Name ?? string.Empty;
            }

            if (user.VendorId > 0)
            {
                var entity = await _context.Vendor.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.VendorId == user.VendorId);
                return entity?.Name ?? string.Empty;
            }

            return string.Empty;
        }
    }
}