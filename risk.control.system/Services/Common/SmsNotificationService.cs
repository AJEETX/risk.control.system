using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Common
{
    public interface ISmsNotificationService
    {
        Task<bool> RequestSchedule(ScheduleRequest request, string portalUrl);
        Task<bool> ResponseSchedule(long caseId);
        Task<bool> GetSchedule(long caseId);
        Task<string> SendSms2Customer(string currentUser, long caseId, string sms);

        Task<string> SendSms2Beneficiary(string currentUser, long caseId, string sms);

        Task<List<CaseMessage>> GetSmsHistory(long caseId, bool isCustomer);
    }

    internal class SmsNotificationService(ApplicationDbContext context,
        ITinyUrlService tinyUrlService,
        IDataProtectionProvider provider,
        ILogger<SmsNotificationService> logger,
        ISmsService SmsService) : ISmsNotificationService
    {
        private readonly IDataProtector _protector = provider.CreateProtector(Applicationsettings.ENCRYPT_URL_QUERYSTRING);
        private readonly ApplicationDbContext _context = context;
        private readonly ISmsService _smsService = SmsService;
        private readonly ITinyUrlService _tinyUrlService = tinyUrlService;
        private readonly ILogger<SmsNotificationService> _logger = logger;
        private static readonly string _logo = Applicationsettings.WEBSITE_SITE_URL;
        public async Task<bool> GetSchedule(long caseId)
        {
            var caseDetails = await _context.Investigations
                .Include(c => c.ScheduleMeeting)
                .FirstOrDefaultAsync(c => c.Id == caseId);
            if (caseDetails == null)
            {
                throw new Exception("Case details not found for the given case ID.");
            }
            return caseDetails.ScheduleMeeting?.ScheduleAccepted ?? false;
        }
        public async Task<bool> ResponseSchedule(long caseId)
        {
            try
            {
                var caseDetails = await _context.Investigations
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.ScheduleMeeting)
                    .FirstOrDefaultAsync(c => c.Id == caseId);
                if (caseDetails == null)
                {
                    throw new Exception("Case details not found for the given case ID.");
                }
                if (caseDetails.ScheduleMeeting.ScheduleAccepted)
                {
                    return false; // Schedule already accepted
                }
                if (caseDetails.ScheduleMeeting!.ScheduleRequestedDateTime.AddHours(48) < DateTime.UtcNow)
                {
                    return false; // Schedule request has expired
                }
                var isClaim = caseDetails?.PolicyDetail?.InsuranceType == InsuranceType.CLAIM;
                var agent = await _context.ApplicationUser.Include(a => a.Country).FirstOrDefaultAsync(u => u.Email == caseDetails!.TaskedAgentEmail);
                if (agent == null)
                {
                    throw new Exception("Agent details not found for the given case ID.");
                }
                string contactNumberForSchedule = string.Empty;
                string contactNameForSchedule = string.Empty;
                string countryCode = string.Empty;
                if (isClaim)
                {
                    var customer = await _context.CustomerDetail
                        .Include(c => c.Country)
                        .FirstOrDefaultAsync(c => c.InvestigationTaskId == caseId);
                    if (customer == null)
                    {
                        throw new Exception("Customer details not found for the given case ID.");
                    }
                    contactNumberForSchedule = customer.Country!.ISDCode.ToString() + customer?.PhoneNumber ?? string.Empty;
                    contactNameForSchedule = customer?.Name ?? string.Empty;
                    countryCode = customer!.Country!.Code;
                }
                else
                {
                    var beneficiary = await _context.BeneficiaryDetail
                        .Include(b => b.Country)
                        .FirstOrDefaultAsync(b => b.InvestigationTaskId == caseId);
                    if (beneficiary == null)
                    {
                        throw new Exception("Beneficiary details not found for the given case ID.");
                    }
                    contactNumberForSchedule = beneficiary.Country!.ISDCode.ToString() + beneficiary?.PhoneNumber ?? string.Empty;
                    contactNameForSchedule = beneficiary?.Name ?? string.Empty;
                    countryCode = beneficiary!.Country!.Code;
                }
                var message = $"Dear {contactNameForSchedule},\n" +
                              $"Your meeting schedule has been confirmed on {caseDetails!.ScheduleMeeting!.ScheduleDateTime:yyyy-MM-dd} at {caseDetails!.ScheduleMeeting!.ScheduleDateTime:HH:mm}.\n" +
                              $"Thanks,\n" +
                              $"{_logo}";

                var agentMessage = $"Dear {agent?.FirstName} {agent?.LastName},\n" +
                                   $"The schedule for Case/Policy # {caseDetails!.PolicyDetail!.ContractNumber} has been confirmed by the {contactNameForSchedule}.\n" +
                                   $"Thanks,\n" +
                                   $"{_logo}";
                await _smsService.DoSendSmsAsync(countryCode, contactNumberForSchedule, message);
                await _smsService.DoSendSmsAsync(agent!.Country!.Code, agent!.Country!.ISDCode.ToString() + agent!.PhoneNumber, agentMessage);
                caseDetails.ScheduleMeeting!.ScheduleAccepted = true;
                _context.Investigations.Update(caseDetails);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while responding to schedule for Case {CaseId}", caseId);
                return false;
            }
        }
        public async Task<bool> RequestSchedule(ScheduleRequest request, string portalUrl)
        {
            try
            {
                var caseDetails = await _context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(c => c.ScheduleMeeting)
                .FirstOrDefaultAsync(c => c.Id == request.CaseId);
                if (caseDetails == null)
                {
                    throw new Exception("Case details not found for the given case ID.");
                }

                var isClaim = caseDetails?.PolicyDetail?.InsuranceType == InsuranceType.CLAIM;
                var agentName = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == caseDetails!.TaskedAgentEmail);
                string contactNumberForSchedule = string.Empty;
                string contactNameForSchedule = string.Empty;
                string countryCode = string.Empty;
                if (isClaim)
                {
                    var customer = await _context.CustomerDetail
                        .Include(c => c.Country)
                        .FirstOrDefaultAsync(c => c.InvestigationTaskId == request.CaseId);
                    if (customer == null)
                    {
                        throw new Exception("Customer details not found for the given case ID.");
                    }
                    contactNumberForSchedule = customer.Country!.ISDCode.ToString() + customer?.PhoneNumber ?? string.Empty;
                    contactNameForSchedule = customer?.Name ?? string.Empty;
                    countryCode = customer!.Country!.Code;
                }
                else
                {
                    var beneficiary = await _context.BeneficiaryDetail
                        .Include(b => b.Country)
                        .FirstOrDefaultAsync(b => b.InvestigationTaskId == request.CaseId);
                    if (beneficiary == null)
                    {
                        throw new Exception("Beneficiary details not found for the given case ID.");
                    }
                    contactNumberForSchedule = beneficiary.Country!.ISDCode.ToString() + beneficiary?.PhoneNumber ?? string.Empty;
                    contactNameForSchedule = beneficiary?.Name ?? string.Empty;
                    countryCode = beneficiary!.Country!.Code;
                }

                var confirmationUrl = GenerateConfirmationUrl(portalUrl, request.CaseId);

                var shortUrl = await _tinyUrlService.ShortenUrlAsync(confirmationUrl);
                var message = $"Dear {contactNameForSchedule},\n\n" +
                       $"You have received a request to schedule a meeting with the agent on {request.ScheduleDateTime:dd-MM-yyyy} at {request.ScheduleDateTime:HH:mm}.\n\n" +
                       $"Reference Policy/Case Number: {caseDetails!.PolicyDetail!.ContractNumber}\n\n" +
                       $"Please confirm your availability by clicking the link below (valid for 48 hours):\n" +
                       $"{shortUrl}\n\n" +
                       $"If you did not request this meeting, please disregard this message.\n\n" +
                       $"Best regards,\n" +
                       $"{_logo}";

                await _smsService.DoSendSmsAsync(countryCode, contactNumberForSchedule, message);
                caseDetails!.ScheduleMeeting = new ScheduleMeeting
                {
                    CaseId = request.CaseId,
                    ScheduleDateTime = request.ScheduleDateTime,
                    ScheduleRequestedDateTime = DateTime.UtcNow,
                    ScheduleRequested = true,
                };
                _context.Investigations.Update(caseDetails);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while requesting schedule for Case {CaseId}", request.CaseId);
                return false;
            }
        }
        public async Task<string> SendSms2Customer(string currentUser, long caseId, string sms)
        {
            var recipient = await _context.CustomerDetail.AsNoTracking()
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.InvestigationTaskId == caseId);

            return await ProcessSmsSending(recipient?.Name, recipient?.PhoneNumber, recipient?.Country, currentUser, caseId, sms, true);
        }

        public async Task<string> SendSms2Beneficiary(string currentUser, long caseId, string sms)
        {
            var recipient = await _context.BeneficiaryDetail.AsNoTracking()
                .Include(b => b.Country)
                .FirstOrDefaultAsync(c => c.InvestigationTaskId == caseId);

            return await ProcessSmsSending(recipient?.Name, recipient?.PhoneNumber, recipient?.Country, currentUser, caseId, sms, false);
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
        private string GenerateConfirmationUrl(string portalUrl, long caseId)
        {
            // Encrypt the caseId into a secure, URL-safe string token
            var encryptedCaseId = _protector.Protect(caseId.ToString());

            // Construct the URL with the encrypted token
            return $"{portalUrl}/confirm/schedule?token={Uri.EscapeDataString(encryptedCaseId)}";
        }
    }
}