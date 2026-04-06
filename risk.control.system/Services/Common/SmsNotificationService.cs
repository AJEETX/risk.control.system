using Microsoft.AspNetCore.Identity;
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
        private readonly ApplicationDbContext context;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ISmsService smsService;
        private static string logo = Applicationsettings.WEBSITE_SITE_URL;

        public SmsNotificationService(ApplicationDbContext context,
            RoleManager<ApplicationRole> roleManager,
            ISmsService SmsService)
        {
            this.context = context;
            this.roleManager = roleManager;
            smsService = SmsService;
        }

        public async Task<string> SendSms2Customer(string currentUser, long claimId, string sms)
        {
            var caseDetail = await context.Investigations.Include(c => c.CaseMessages).Include(c => c.PolicyDetail).Include(c => c.CustomerDetail).ThenInclude(c => c!.PinCode)
                .Include(c => c.CustomerDetail).ThenInclude(c => c!.Country).FirstOrDefaultAsync(c => c.Id == claimId);
            var mobile = caseDetail!.CustomerDetail!.PhoneNumber.ToString();
            var user = await context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(u => u.Email == currentUser);
            var isdCode = caseDetail.CustomerDetail.Country!.ISDCode;
            var isInsurerUser = user!.ClientCompanyId > 0;
            var isVendorUser = user.VendorId > 0;
            string entityName = string.Empty;
            ApplicationUser insurerUser;
            ApplicationUser agencyUser;
            if (isInsurerUser)
            {
                insurerUser = user;
                var entity = await context.ClientCompany.AsNoTracking().FirstOrDefaultAsync(c => c.ClientCompanyId == insurerUser.ClientCompanyId);
                entityName = entity?.Name ?? string.Empty;
            }
            else if (isVendorUser)
            {
                agencyUser = user;
                var entity = await context.Vendor.AsNoTracking().FirstOrDefaultAsync(v => v.VendorId == agencyUser.VendorId);
                entityName = entity?.Name ?? string.Empty;
            }
            if (!isInsurerUser && !isVendorUser)
            {
                return string.Empty;
            }
            var message = $"Dear {caseDetail.CustomerDetail.Name}\n\n" + $"{sms}\n\n" + $"Thanks\n\n" + "{user.FirstName} {user.LastName}\n\n" + $"Policy #:{caseDetail.PolicyDetail!.ContractNumber}\n\n" + $"{entityName}\n\n";
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
            await context.SaveChangesAsync(null, false);
            await smsService.DoSendSmsAsync(caseDetail.CustomerDetail.Country.Code, "+" + isdCode + mobile, message);
            return caseDetail.CustomerDetail.Name;
        }

        public async Task<string> SendSms2Beneficiary(string currentUser, long claimId, string sms)
        {
            var beneficiary = await context.BeneficiaryDetail.AsNoTracking().Include(b => b.Country).FirstOrDefaultAsync(c => c.InvestigationTaskId == claimId);
            var mobile = beneficiary!.PhoneNumber.ToString();
            var user = await context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(u => u.Email == currentUser);
            var isdCode = beneficiary.Country!.ISDCode;
            var isInsurerUser = user!.ClientCompanyId > 0;
            var isVendorUser = user.VendorId > 0;
            string entityName = string.Empty;
            ApplicationUser insurerUser;
            ApplicationUser agencyUser;
            if (isInsurerUser)
            {
                insurerUser = user;
                var entity = await context.ClientCompany.AsNoTracking().FirstOrDefaultAsync(c => c.ClientCompanyId == insurerUser.ClientCompanyId);
                entityName = entity!.Name;
            }
            else if (isVendorUser)
            {
                agencyUser = user;
                var entity = await context.Vendor.AsNoTracking().FirstOrDefaultAsync(v => v.VendorId == agencyUser.VendorId);
                entityName = entity!.Name;
            }
            if (!isInsurerUser && !isVendorUser)
            {
                return string.Empty;
            }
            var caseTask = await context.Investigations.Include(c => c.CaseMessages).Include(c => c.PolicyDetail).FirstOrDefaultAsync(c => c.Id == claimId);
            var message = $"Dear {beneficiary.Name}\n\n" + $"{sms}\n\n" + $"Thanks\n\n" + $"{user.FirstName} {user.LastName}\n\n" + $"Policy #:{caseTask!.PolicyDetail!.ContractNumber}\n\n";
            message += $"{entityName}\n\n";
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
            await context.SaveChangesAsync(null, false);
            await smsService.DoSendSmsAsync(beneficiary.Country.Code, "+" + isdCode + mobile, message);
            return beneficiary.Name;
        }

        public async Task<List<CaseMessage>> GetSmsHistory(long caseId, bool isCustomer)
        {
            var messages = await context.CaseMessages
                .Where(m => m.InvestigationTaskId == caseId && m.IsCustomer == isCustomer)
                .OrderByDescending(m => m.Updated) // Show newest first

                .ToListAsync();
            return messages;
        }
    }
}