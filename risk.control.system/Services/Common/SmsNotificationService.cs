using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Services.Common
{
    public interface ISmsNotificationService
    {
        Task<string> SendSms2Customer(string currentUser, long claimId, string sms);

        Task<string> SendSms2Beneficiary(string currentUser, long claimId, string sms);

        Task<List<CaseMessage>> GetSmsHistory(long caseId, bool isCustomer);
    }

    internal class SmsNotificationService : ISmsNotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _smsService;
        private static string logo = Applicationsettings.WEBSITE_SITE_URL;

        public SmsNotificationService(ApplicationDbContext context,
            ISmsService SmsService)
        {
            _context = context;
            _smsService = SmsService;
        }

        public async Task<string> SendSms2Customer(string currentUser, long claimId, string sms)
        {
            var caseDetail = await _context.Investigations.Include(c => c.CaseMessages).Include(c => c.PolicyDetail).Include(c => c.CustomerDetail).ThenInclude(c => c!.PinCode)
                .Include(c => c.CustomerDetail).ThenInclude(c => c!.Country).FirstOrDefaultAsync(c => c.Id == claimId);
            var mobile = caseDetail!.CustomerDetail!.PhoneNumber;
            var user = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(u => u.Email == currentUser);
            var isdCode = caseDetail.CustomerDetail.Country!.ISDCode;
            var isInsurerUser = user!.ClientCompanyId > 0;
            var isVendorUser = user.VendorId > 0;
            string entityName = string.Empty;
            ApplicationUser insurerUser;
            ApplicationUser agencyUser;
            if (isInsurerUser)
            {
                insurerUser = user;
                var entity = await _context.ClientCompany.AsNoTracking().FirstOrDefaultAsync(c => c.ClientCompanyId == insurerUser.ClientCompanyId);
                entityName = entity?.Name ?? string.Empty;
            }
            else if (isVendorUser)
            {
                agencyUser = user;
                var entity = await _context.Vendor.AsNoTracking().FirstOrDefaultAsync(v => v.VendorId == agencyUser.VendorId);
                entityName = entity?.Name ?? string.Empty;
            }
            if (!isInsurerUser && !isVendorUser)
            {
                return string.Empty;
            }
            var message = $"Dear {caseDetail.CustomerDetail.Name}\n" + $"{sms}\n" + $"Thanks\n" + "{user.FirstName} {user.LastName}\n" + $"Policy #:{caseDetail.PolicyDetail!.ContractNumber}\n" + $"{entityName}\n";
            message += $"{logo}";

            var scheduleMessage = new CaseMessage
            {
                Message = sms,
                IsCustomer = true,
                InvestigationTaskId = claimId,
                RecepicientEmail = caseDetail.CustomerDetail.Name,
                SenderEmail = user.Email,
                UpdatedBy = user.Email,
                Updated = DateTime.UtcNow
            };
            caseDetail.CaseMessages!.Add(scheduleMessage);
            await _context.SaveChangesAsync(null, false);
            await _smsService.DoSendSmsAsync(caseDetail.CustomerDetail.Country.Code, "+" + isdCode + mobile, message);
            return caseDetail.CustomerDetail.Name;
        }

        public async Task<string> SendSms2Beneficiary(string currentUser, long claimId, string sms)
        {
            var beneficiary = await _context.BeneficiaryDetail.AsNoTracking().Include(b => b.Country).FirstOrDefaultAsync(c => c.InvestigationTaskId == claimId);
            var mobile = beneficiary!.PhoneNumber;
            var user = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(u => u.Email == currentUser);
            var isdCode = beneficiary.Country!.ISDCode;
            var isInsurerUser = user!.ClientCompanyId > 0;
            var isVendorUser = user.VendorId > 0;
            string entityName = string.Empty;
            ApplicationUser insurerUser;
            ApplicationUser agencyUser;
            if (isInsurerUser)
            {
                insurerUser = user;
                var entity = await _context.ClientCompany.AsNoTracking().FirstOrDefaultAsync(c => c.ClientCompanyId == insurerUser.ClientCompanyId);
                entityName = entity!.Name;
            }
            else if (isVendorUser)
            {
                agencyUser = user;
                var entity = await _context.Vendor.AsNoTracking().FirstOrDefaultAsync(v => v.VendorId == agencyUser.VendorId);
                entityName = entity!.Name;
            }
            if (!isInsurerUser && !isVendorUser)
            {
                return string.Empty;
            }
            var caseTask = await _context.Investigations.Include(c => c.CaseMessages).Include(c => c.PolicyDetail).FirstOrDefaultAsync(c => c.Id == claimId);
            var message = $"Dear {beneficiary.Name}\n" + $"{sms}\n" + $"Thanks\n" + $"{user.FirstName} {user.LastName}\n" + $"Policy #:{caseTask!.PolicyDetail!.ContractNumber}\n";
            message += $"{entityName}\n";
            message += $"{logo}";
            var scheduleMessage = new CaseMessage
            {
                Message = sms,
                InvestigationTaskId = claimId,
                RecepicientEmail = beneficiary.Name,
                SenderEmail = user.Email,
                UpdatedBy = user.Email,
                Updated = DateTime.UtcNow
            };
            caseTask.CaseMessages!.Add(scheduleMessage);
            await _context.SaveChangesAsync(null, false);
            await _smsService.DoSendSmsAsync(beneficiary.Country.Code, "+" + isdCode + mobile, message);
            return beneficiary.Name;
        }

        public async Task<List<CaseMessage>> GetSmsHistory(long caseId, bool isCustomer)
        {
            var messages = await _context.CaseMessages
                .Where(m => m.InvestigationTaskId == caseId && m.IsCustomer == isCustomer)
                .OrderByDescending(m => m.Updated) // Show newest first
                .ToListAsync();
            return messages;
        }

        public override bool Equals(object? obj)
        {
            return obj is SmsNotificationService service &&
                   EqualityComparer<ApplicationDbContext>.Default.Equals(_context, service._context);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_context);
        }
    }
}