using System.Net;

using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface INotificationService
    {
        Task ClearAll(string userEmail);
        Task MarkAsRead(int id, string userEmail);
        Task<List<StatusNotification>> GetNotifications(string userEmail);
        Task<string> SendSms2Customer(string currentUser, long claimId, string sms);

        Task<string> SendSms2Beneficiary(string currentUser, long claimId, string sms);
        bool IsWhiteListIpAddress(IPAddress remoteIp);
    }

    internal class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext context;
        private readonly ISmsService smsService;
        private readonly IHttpClientService httpClientService;
        private readonly IFeatureManager featureManager;
        private static string logo = "https://icheckify.co.in";
        private static System.Net.WebClient client = new System.Net.WebClient();
        private const string IP_BASE_URL = "http://ip-api.com";

        private static HttpClient _httpClient = new HttpClient();

        public NotificationService(ApplicationDbContext context,
            ISmsService SmsService,
            IHttpClientService httpClientService,
            IFeatureManager featureManager)
        {
            this.context = context;
            smsService = SmsService;
            this.httpClientService = httpClientService;
            this.featureManager = featureManager;
        }

        public bool IsWhiteListIpAddress(IPAddress remoteIp)
        {
            var bytes = remoteIp.GetAddressBytes();
            var whitelistedIp = false;
            var ipAddresses = context.ClientCompany.Where(c => !string.IsNullOrWhiteSpace(c.WhitelistIpAddress)).Select(c => c.WhitelistIpAddress).ToList();

            if (ipAddresses.Any())
            {
                var safelist = string.Join(";", ipAddresses);
                var ips = safelist.Split(';');
                var _safelist = new byte[ips.Length][];
                for (var i = 0; i < ips.Length; i++)
                {
                    _safelist[i] = IPAddress.Parse(ips[i]).GetAddressBytes();
                }
                foreach (var address in _safelist)
                {
                    if (address.SequenceEqual(bytes))
                    {
                        return true;
                    }
                }
            }
            return whitelistedIp;
        }

        public async Task<string> SendSms2Customer(string currentUser, long claimId, string sms)
        {
            var claim = await context.Investigations
            .Include(c => c.CaseMessages)
            .Include(c => c.PolicyDetail)
            .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.Country)
            .FirstOrDefaultAsync(c => c.Id == claimId);

            var mobile = claim.CustomerDetail.PhoneNumber.ToString();
            var user = context.ApplicationUser.FirstOrDefault(u => u.Email == currentUser);
            var isdCode = claim.CustomerDetail.Country.ISDCode;
            var isInsurerUser = user is ClientCompanyApplicationUser;
            var isVendorUser = user is VendorApplicationUser;

            string company = string.Empty;
            ClientCompanyApplicationUser insurerUser;
            VendorApplicationUser agencyUser;
            if (isInsurerUser)
            {
                insurerUser = (ClientCompanyApplicationUser)user;
                company = context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == insurerUser.ClientCompanyId)?.Name;
            }
            else if (isVendorUser)
            {
                agencyUser = (VendorApplicationUser)user;
                company = context.Vendor.FirstOrDefault(v => v.VendorId == agencyUser.VendorId).Name;
            }
            if (!isInsurerUser && !isVendorUser)
            {
                return string.Empty;
            }
            var message = $"Dear {claim.CustomerDetail.Name}\n\n";
            message += $"{sms}\n\n";
            message += $"Thanks\n\n";
            message += $"{user.FirstName} {user.LastName}\n\n";
            message += $"Policy #:{claim.PolicyDetail.ContractNumber}\n\n";
            message += $"{company}\n\n";
            message += $"{logo}";

            var scheduleMessage = new CaseMessage
            {
                Message = message,
                InvestigationTaskId = claimId,
                SenderEmail = user.Email,
                UpdatedBy = user.Email,
                Updated = DateTime.Now
            };
            claim.CaseMessages.Add(scheduleMessage);
            await context.SaveChangesAsync(null, false);
            await smsService.DoSendSmsAsync(claim.CustomerDetail.Country.Code, "+" + isdCode + mobile, message);
            return claim.CustomerDetail.Name;
        }

        public async Task<string> SendSms2Beneficiary(string currentUser, long claimId, string sms)
        {
            var beneficiary = await context.BeneficiaryDetail
                .Include(b => b.Country)
                .Include(b => b.InvestigationTask)
                .ThenInclude(c => c.PolicyDetail)
               .FirstOrDefaultAsync(c => c.InvestigationTaskId == claimId);

            var mobile = beneficiary.PhoneNumber.ToString();
            var user = context.ApplicationUser.FirstOrDefault(u => u.Email == currentUser);
            var isdCode = beneficiary.Country.ISDCode;

            var isInsurerUser = user is ClientCompanyApplicationUser;
            var isVendorUser = user is VendorApplicationUser;

            string company = string.Empty;
            ClientCompanyApplicationUser insurerUser;
            VendorApplicationUser agencyUser;
            if (isInsurerUser)
            {
                insurerUser = (ClientCompanyApplicationUser)user;
                company = context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == insurerUser.ClientCompanyId)?.Name;
            }
            else if (isVendorUser)
            {
                agencyUser = (VendorApplicationUser)user;
                company = context.Vendor.FirstOrDefault(v => v.VendorId == agencyUser.VendorId).Name;
            }
            if (!isInsurerUser && !isVendorUser)
            {
                return string.Empty;
            }
            var message = $"Dear {beneficiary.Name}\n\n";
            message += $"{sms}\n\n";
            message += $"Thanks\n\n";
            message += $"{user.FirstName} {user.LastName}\n\n";
            message += $"Policy #:{beneficiary.InvestigationTask.PolicyDetail.ContractNumber}\n\n";
            message += $"{company}\n\n";
            message += $"{logo}";

            var scheduleMessage = new CaseMessage
            {
                Message = message,
                InvestigationTaskId = claimId,
                RecepicientEmail = beneficiary.Name,
                SenderEmail = user.Email,
                UpdatedBy = user.Email,
                Updated = DateTime.Now
            };
            var claim = context.Investigations
            .Include(c => c.CaseMessages)
            .Include(c => c.PolicyDetail)
            .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
            .FirstOrDefault(c => c.Id == claimId);
            claim.CaseMessages.Add(scheduleMessage);
            await context.SaveChangesAsync(null, false);
            await smsService.DoSendSmsAsync(beneficiary.Country.Code, "+" + isdCode + mobile, message);
            return beneficiary.Name;
        }

        public async Task<List<StatusNotification>> GetNotifications(string userEmail)
        {

            var companyUser = context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendorUser = context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            ApplicationRole role = null!;
            ClientCompany company = null!;
            Vendor agency = null!;
            if (companyUser != null)
            {
                role = context.ApplicationRole.FirstOrDefault(r => r.Name == companyUser.Role.ToString());
                company = context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

                var notifications = context.Notifications.Where(n => n.Company == company && (!n.IsReadByCreator || !n.IsReadByManager || !n.IsReadByAssessor));
                if (role.Name == AppRoles.ASSESSOR.ToString())
                {
                    notifications = notifications.Where(n => n.Role == role && !n.IsReadByAssessor);
                }
                else if (role.Name == AppRoles.MANAGER.ToString())
                {
                    notifications = notifications.Where(n => !n.IsReadByManager);
                }

                else if (role.Name == AppRoles.CREATOR.ToString())
                {
                    notifications = notifications.Where(n => (n.Role == role && n.NotifierUserEmail == userEmail) && !n.IsReadByCreator);
                }

                var activeNotifications = await notifications
                    .OrderByDescending(n => n.CreatedAt).ToListAsync();
                return activeNotifications;
            }
            else if (vendorUser != null)
            {
                role = context.ApplicationRole.FirstOrDefault(r => r.Name == vendorUser.Role.ToString());
                agency = context.Vendor.FirstOrDefault(c => c.VendorId == vendorUser.VendorId);

                var notifications = context.Notifications.Where(n => n.Agency == agency && (!n.IsReadByVendor || !n.IsReadByVendorAgent));

                if (role.Name == AppRoles.AGENT.ToString())
                {
                    notifications = notifications.Where(n => n.AgenctUserEmail == userEmail);
                }
                else
                {
                    var superRole = context.ApplicationRole.FirstOrDefault(r => r.Name == AppRoles.SUPERVISOR.ToString());
                    if (role.Name == AppRoles.SUPERVISOR.ToString())
                    {
                        notifications = notifications.Where(n =>
                        (!n.IsReadByVendor && n.NotifierUserEmail == userEmail &&
                        (n.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR || n.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR))
                        ||
                        (!n.IsReadByVendor && n.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
                        );
                    }
                    else
                    {
                        notifications = notifications.Where(n => (!n.IsReadByVendor));
                    }
                }


                var activeNotifications = await notifications
                     .OrderByDescending(n => n.CreatedAt).ToListAsync();
                return activeNotifications;
            }
            var allNotifications = await context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return allNotifications;
        }

        public async Task MarkAsRead(int id, string userEmail)
        {
            var companyUser = context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            var vendorUser = context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            ApplicationRole role = null!;
            ClientCompany company = null!;
            Vendor agency = null!;
            if (companyUser != null)
            {
                role = context.ApplicationRole.FirstOrDefault(r => r.Name == companyUser.Role.ToString());
                company = context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);
                var notification = context.Notifications.FirstOrDefault(s => s.StatusNotificationId == id);
                if (notification == null)
                {
                    return;
                }
                if (role.Name == AppRoles.ASSESSOR.ToString())
                {
                    notification.IsReadByAssessor = true;
                }
                else if (role.Name == AppRoles.MANAGER.ToString())
                {
                    notification.IsReadByManager = true;
                }

                else if (role.Name == AppRoles.CREATOR.ToString())
                {
                    notification.IsReadByCreator = true;
                }
                context.Notifications.Update(notification);
                var rows = await context.SaveChangesAsync(null, false);
            }
            else if (vendorUser != null)
            {
                role = context.ApplicationRole.FirstOrDefault(r => r.Name == vendorUser.Role.ToString());
                agency = context.Vendor.FirstOrDefault(c => c.VendorId == vendorUser.VendorId);
                var notification = context.Notifications.FirstOrDefault(s => s.Agency == agency && s.StatusNotificationId == id);
                if (notification == null)
                {
                    return;
                }
                if (role.Name == AppRoles.AGENCY_ADMIN.ToString() || role.Name == AppRoles.SUPERVISOR.ToString())
                {
                    notification.IsReadByVendor = true;
                }

                else if (role.Name == AppRoles.AGENT.ToString())
                {
                    notification.IsReadByVendorAgent = true;
                }
                context.Notifications.Update(notification);
                var rows = await context.SaveChangesAsync(null, false);
            }
        }

        public async Task ClearAll(string userEmail)
        {
            var notifications = await GetNotifications(userEmail);
            foreach (var notification in notifications)
            {
                await MarkAsRead(notification.StatusNotificationId, userEmail);
            }
        }
    }
}