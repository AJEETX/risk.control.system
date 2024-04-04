using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface INotificationService
    {
        Task<ClaimsInvestigation> SendVerifySchedule(ClientSchedulingMessage message);

        Task<ClaimsInvestigation> ReplyVerifySchedule(string id, string confirm = "N");

        Task<IpApiResponse?> GetClientIp(string? ipAddress, CancellationToken ct, string page, string userEmail = "", bool isAuthenticated = false);

        Task<(ClaimMessage message, string yes, string no)> GetClaim(string baseUrl, string id);

        string SendSms2Customer(string currentUser, string claimId, string sms);

        string SendSms2Beneficiary(string currentUser, string claimId, string sms);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IFeatureManager featureManager;
        private static string logo = "https://icheckify.co.in";
        private static System.Net.WebClient client = new System.Net.WebClient();
        private const string IP_BASE_URL = "http://ip-api.com";

        private static HttpClient _httpClient = new HttpClient();

        public NotificationService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IFeatureManager featureManager)
        {
            this.context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.featureManager = featureManager;
        }

        public async Task<IpApiResponse?> GetClientIp(string? ipAddress, CancellationToken ct, string page, string userEmail = "", bool isAuthenticated = false)
        {
            var curTimeZone = TimeZone.CurrentTimeZone;

            var route = $"{IP_BASE_URL}/json/{ipAddress}";
            page = page == "/" ? "dashboard" : page;
            var response = await _httpClient.GetFromJsonAsync<IpApiResponse>(route, ct);
            var longLatString = response?.lat.GetValueOrDefault().ToString() + "," + response?.lon.GetValueOrDefault().ToString();
            var mapUrl = $"https://maps.googleapis.com/maps/api/staticmap?center={longLatString}&zoom=6&size=560x300&maptype=roadmap&markers=color:red%7Clabel:S%7C{longLatString}&key={Applicationsettings.GMAPData}";

            if (response != null && (await featureManager.IsEnabledAsync(FeatureFlags.IPTracking)))
            {
                if ((isAuthenticated && !string.IsNullOrWhiteSpace(userEmail) && !userEmail.StartsWith("admin")) || !isAuthenticated)
                {
                    response.page = page;
                    response.user = userEmail;
                    response.isAuthenticated = isAuthenticated;
                    response.MapUrl = mapUrl;
                    context.IpApiResponse.Add(response);
                    await context.SaveChangesAsync();
                }
            }
            return response;
        }

        public async Task<ClaimsInvestigation> SendVerifySchedule(ClientSchedulingMessage message)
        {
            var claim = context.ClaimsInvestigation
                .Include(c => c.ClaimMessages)
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .FirstOrDefault(c => c.ClaimsInvestigationId == message.ClaimId);
            var assignedToAgentStatus = context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var beneficiary = context.CaseLocation
                .FirstOrDefault(c => c.ClaimsInvestigationId == message.ClaimId
                && c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId);

            string mobile = string.Empty;
            string recepientName = string.Empty;
            string recepientPhone = string.Empty;
            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
            {
                mobile = claim.CustomerDetail.ContactNumber.ToString();
                recepientName = claim.CustomerDetail.CustomerName;
                recepientPhone = claim.CustomerDetail.ContactNumber.ToString();
            }
            else if (claim.PolicyDetail.ClaimType == ClaimType.DEATH)
            {
                mobile = beneficiary.BeneficiaryContactNumber.ToString();
                recepientName = beneficiary.BeneficiaryName;
                recepientPhone = beneficiary.BeneficiaryContactNumber.ToString();
            }

            string device = "0";
            long? timestamp = null;
            bool isMMS = false;

            string? attachments = $"<a href='{logo}'>team</a>";

            string baseUrl = $"{message.BaseUrl}/api/notification/ConfirmSchedule?id={message.ClaimId}&confirm=";
            string yesUrl = $"{baseUrl}Y";
            string noUrl = $"{baseUrl}N";

            //var address = new Uri("http://tinyurl.com/api-create.php?url=" + yesUrl);
            //var yesTinyUrl = client.DownloadString(address);

            //address = new Uri("http://tinyurl.com/api-create.php?url=" + noUrl);
            //var noTinyUrl = client.DownloadString(address);
            string agentMessage = $"Dear {recepientName}";
            agentMessage += "                       ";
            agentMessage += $"  {claim.CurrentClaimOwner} visit you on Date: {message.Time} for the claim policy {claim.PolicyDetail.ContractNumber}.            ";

            var verifyMessage = agentMessage;
            verifyMessage += "                                                       ";
            //verifyMessage += $"                                                       Click  (Yes){yesTinyUrl} ";
            //verifyMessage += "---------------------------------------";
            //verifyMessage += "                                                       ";
            //verifyMessage += $"                                                       or  (No){noTinyUrl}";
            //verifyMessage += "                                                       ";
            bool priority = true;

            //var path = Path.Combine(webHostEnvironment.WebRootPath, "form", "ConfirmSchedule.html");

            //var subject = "Verify Your E-mail Address ";
            //string HtmlBody = "";
            //using (StreamReader stream = File.OpenText(path))
            //{
            //    HtmlBody = stream.ReadToEnd();
            //}

            var confirmPage = $"{message.BaseUrl + "/Confirm?id=" + message.ClaimId}";
            var address = new Uri("http://tinyurl.com/api-create.php?url=" + confirmPage);
            var confirmTinyUrl = client.DownloadString(address);

            string? callbackUrl = message.BaseUrl + "";
            string confirmPageUrl = "                                                                      Click to ";
            confirmPageUrl += $"                                                                      (CONFIRM){confirmTinyUrl}";
            confirmPageUrl += "                                                                      ";
            string finalMessage = verifyMessage;
            finalMessage += "                                                                               ";
            finalMessage += confirmPageUrl;
            finalMessage += "Thanks";
            finalMessage += "                                                                               ";
            finalMessage += logo;
            //string messageBody = string.Format(HtmlBody,
            //    subject,
            //    string.Format("{0:dddd, d MMMM yyyy}", DateTime.Now),
            //    recepientName,
            //    recepientName,
            //    confirmPageUrl,
            //    yesTinyUrl,
            //    noTinyUrl
            //    );

            var response = SMS.API.SendSingleMessage("+" + mobile, finalMessage, device, timestamp, isMMS, null, priority);
            var meetingTime = DateTime.Now.AddDays(1);
            if (DateTime.TryParse(message.Time, out DateTime date))
            {
                meetingTime = date;
            }

            var senderDetail = context.ApplicationUser.FirstOrDefault(u => u.Email == claim.CurrentClaimOwner);

            var scheduleMessage = new ClaimMessage
            {
                Message = finalMessage,
                ClaimsInvestigationId = message.ClaimId,
                RecepicientEmail = recepientName,
                SenderEmail = claim.CurrentClaimOwner,
                UpdatedBy = claim.CurrentClaimOwner,
                Updated = DateTime.UtcNow,
                ScheduleTime = meetingTime,
                SenderPhone = senderDetail.PhoneNumber,
                RecepicientPhone = recepientPhone
            };
            claim.ClaimMessages.Add(scheduleMessage);
            await context.SaveChangesAsync();

            return claim;
        }

        public async Task<ClaimsInvestigation> ReplyVerifySchedule(string id, string confirm = "N")
        {
            var claim = context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .FirstOrDefault(c => c.ClaimsInvestigationId == id);
            var assignedToAgentStatus = context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var beneficiary = context.CaseLocation
                .FirstOrDefault(c => c.ClaimsInvestigationId == id
                && c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId);

            string recepientName = string.Empty;
            string recepientPhone = string.Empty;
            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
            {
                recepientName = claim.CustomerDetail.CustomerName;
                recepientPhone = claim.CustomerDetail.ContactNumber.ToString();
            }
            else if (claim.PolicyDetail.ClaimType == ClaimType.DEATH)
            {
                recepientName = beneficiary.BeneficiaryName;
                recepientPhone = beneficiary.BeneficiaryContactNumber.ToString();
            }

            if (confirm.ToUpper() == "Y")
            {
                confirm = "YES";
            }
            else if (confirm.ToUpper() == "N")
            {
                confirm = "NO";
            }

            string device = "0";
            long? timestamp = null;
            bool isMMS = false;

            string agentMessage = $"Dear {claim.CurrentClaimOwner},";
            agentMessage += $"                              ";
            agentMessage += $"{recepientName} has replied  {confirm} to your Visit schedule for claim Policy :{claim.PolicyDetail.ContractNumber}";
            agentMessage += "                              ";

            var finalMessage = $"{agentMessage}";
            finalMessage += "                               ";
            finalMessage += $"Dated: {DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm")}";
            finalMessage += "                               ";
            finalMessage += $"{logo}";
            bool priority = true;

            var previousMessage = context.ClaimMessage.FirstOrDefault(u => u.ClaimsInvestigationId == claim.ClaimsInvestigationId);
            var agent = context.VendorApplicationUser.FirstOrDefault(u => u.Email == claim.CurrentClaimOwner);

            var response = SMS.API.SendSingleMessage("+" + agent.PhoneNumber, finalMessage, device, timestamp, isMMS, null, priority);

            var scheduleMessage = new ClaimMessage
            {
                Message = finalMessage,
                ClaimsInvestigationId = id,
                RecepicientEmail = claim.CurrentClaimOwner,
                SenderEmail = recepientName,
                UpdatedBy = recepientName,
                Updated = DateTime.UtcNow,
                ScheduleTime = previousMessage.ScheduleTime,
                SenderPhone = recepientPhone,
                RecepicientPhone = agent.PhoneNumber,
                PreviousClaimMessageId = previousMessage.PreviousClaimMessageId
            };
            claim.ClaimMessages.Add(scheduleMessage);
            await context.SaveChangesAsync();
            return claim;
        }

        public async Task<(ClaimMessage message, string yes, string no)> GetClaim(string baseUrl, string id)
        {
            var claim = context.ClaimsInvestigation
             .Include(c => c.ClaimMessages)
             .Include(c => c.PolicyDetail)
             .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
             .FirstOrDefault(c => c.ClaimsInvestigationId == id);
            var assignedToAgentStatus = context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var beneficiary = context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.ReportQuestionaire)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DocumentIdReport)
                .Include(c => c.District)
                .Include(c => c.Country)
                .Include(c => c.State)
                .Include(c => c.Vendor)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.ServiceReportTemplate.ReportTemplate.DigitalIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.ServiceReportTemplate.ReportTemplate.DocumentIdReport)
                .Include(c => c.ClaimReport)
                    .ThenInclude(c => c.ServiceReportTemplate.ReportTemplate.ReportQuestionaire)
                .FirstOrDefault(c => c.ClaimsInvestigationId == id
                && c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId);

            string mobile = string.Empty;
            string recepientName = string.Empty;
            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
            {
                mobile = claim.CustomerDetail.ContactNumber.ToString();
                recepientName = claim.CustomerDetail.CustomerName;
            }
            else if (claim.PolicyDetail.ClaimType == ClaimType.DEATH)
            {
                mobile = beneficiary.BeneficiaryContactNumber.ToString();
                recepientName = beneficiary.BeneficiaryName;
            }

            //var path = Path.Combine(webHostEnvironment.WebRootPath, "form", "ConfirmAcountRegister.html");

            //var subject = "Verify Your E-mail Address ";
            //string HtmlBody = "";
            //using (StreamReader stream = File.OpenText(path))
            //{
            //    HtmlBody = stream.ReadToEnd();
            //}

            string fullUrl = $"{baseUrl}/api/notification/ConfirmSchedule?id={id}&confirm=";
            string yesUrl = $"{fullUrl}Y";
            string noUrl = $"{fullUrl}N";

            var address = new Uri("http://tinyurl.com/api-create.php?url=" + yesUrl);
            var yesTinyUrl = client.DownloadString(address);

            address = new Uri("http://tinyurl.com/api-create.php?url=" + noUrl);
            var noTinyUrl = client.DownloadString(address);

            var agent = context.VendorApplicationUser.FirstOrDefault(u => u.Email == claim.CurrentClaimOwner);

            //string messageBody = string.Format(HtmlBody,
            //    subject,
            //    string.Format("{0:dddd, d MMMM yyyy}", DateTime.Now),
            //    recepientName,
            //    recepientName,
            //    "#",
            //    yesTinyUrl,
            //    noTinyUrl
            //    );
            var scheduleMessage = context.ClaimMessage
                .Where(m => m.ClaimsInvestigationId == id)?
                .OrderByDescending(m => m.Created)?.FirstOrDefault();
            return (scheduleMessage, yesUrl, noUrl);
        }

        public string SendSms2Customer(string currentUser, string claimId, string sms)
        {
            var claim = context.ClaimsInvestigation
            .Include(c => c.ClaimMessages)
            .Include(c => c.PolicyDetail)
            .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
            .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);

            var mobile = claim.CustomerDetail.ContactNumber.ToString();
            var user = context.ApplicationUser.FirstOrDefault(u => u.Email == currentUser);

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
            var message = $"Dear {claim.CustomerDetail.CustomerName}";
            message += "                                                                                ";
            message += $"{sms}";
            message += "                                                                                ";
            message += $"Thanks";
            message += "                                                                                ";
            message += $"{user.FirstName} {user.LastName}";
            message += "                                                                                ";
            message += $"Policy #:{claim.PolicyDetail.ContractNumber}";
            message += "                                                                                ";
            message += $"{company}";
            message += "                                                                                ";
            message += $"{logo}";

            var scheduleMessage = new ClaimMessage
            {
                Message = message,
                ClaimsInvestigationId = claimId,
                RecepicientEmail = claim.CurrentClaimOwner,
                SenderEmail = user.Email,
                UpdatedBy = user.Email,
                Updated = DateTime.UtcNow
            };
            claim.ClaimMessages.Add(scheduleMessage);
            context.SaveChanges();
            var response = SMS.API.SendSingleMessage("+" + mobile, message, "0", null, false, null, true);
            return claim.CustomerDetail.CustomerName;
        }

        public string SendSms2Beneficiary(string currentUser, string claimId, string sms)
        {
            var beneficiary = context.CaseLocation.Include(b => b.ClaimsInvestigation).ThenInclude(c => c.PolicyDetail)
               .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);

            var mobile = beneficiary.BeneficiaryContactNumber.ToString();
            var user = context.ApplicationUser.FirstOrDefault(u => u.Email == currentUser);

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
            var message = $"Dear {beneficiary.BeneficiaryName}";
            message += "                                                                                ";
            message += $"{sms}";
            message += "                                                                                ";
            message += $"Thanks";
            message += "                                                                                ";
            message += $"{user.FirstName} {user.LastName}";
            message += "                                                                                ";
            message += $"Policy #:{beneficiary.ClaimsInvestigation.PolicyDetail.ContractNumber}";
            message += "                                                                                ";
            message += $"{company}";
            message += "                                                                                ";
            message += $"{logo}";

            var scheduleMessage = new ClaimMessage
            {
                Message = message,
                ClaimsInvestigationId = claimId,
                RecepicientEmail = beneficiary.BeneficiaryName,
                SenderEmail = user.Email,
                UpdatedBy = user.Email,
                Updated = DateTime.UtcNow
            };
            var claim = context.ClaimsInvestigation
            .Include(c => c.ClaimMessages)
            .Include(c => c.PolicyDetail)
            .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
            .FirstOrDefault(c => c.ClaimsInvestigationId == claimId);
            claim.ClaimMessages.Add(scheduleMessage);
            context.SaveChanges();
            var response = SMS.API.SendSingleMessage("+" + mobile, message, "0", null, false, null, true);
            return beneficiary.BeneficiaryName;
        }
    }
}