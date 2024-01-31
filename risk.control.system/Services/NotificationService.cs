using System.Net.Http;

using Microsoft.EntityFrameworkCore;

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

        Task<IpApiResponse?> GetClientIp(string? ipAddress, CancellationToken ct);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private static string logo = "https://icheckify.co.in/assets/img/iCheckifyLogo.png";
        private static System.Net.WebClient client = new System.Net.WebClient();
        private const string IP_BASE_URL = "http://ip-api.com";

        private static HttpClient _httpClient = new HttpClient();

        public NotificationService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            this.context = context;
            this.webHostEnvironment = webHostEnvironment;
        }

        public async Task<IpApiResponse?> GetClientIp(string? ipAddress, CancellationToken ct)
        {
            var route = $"{IP_BASE_URL}/json/{ipAddress}";
            var response = await _httpClient.GetFromJsonAsync<IpApiResponse>(route, ct);
            return response;
        }

        public async Task<ClaimsInvestigation> SendVerifySchedule(ClientSchedulingMessage message)
        {
            var claim = context.ClaimsInvestigation
                .Include(c => c.ClaimMessages)
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
            .ThenInclude(c => c.PinCode)
                .FirstOrDefault(c => c.ClaimsInvestigationId == message.ClaimId);
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
                .FirstOrDefault(c => c.ClaimsInvestigationId == message.ClaimId
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

            string device = "0";
            long? timestamp = null;
            bool isMMS = false;

            string? attachments = $"<a href='{logo}'>team</a>";

            string baseUrl = $"{message.BaseUrl}/api/notification/ConfirmSchedule?id={message.ClaimId}&confirm=";
            string yesUrl = $"{baseUrl}Y";
            string noUrl = $"{baseUrl}N";

            var address = new Uri("http://tinyurl.com/api-create.php?url=" + yesUrl);
            var yesTinyUrl = client.DownloadString(address);

            address = new Uri("http://tinyurl.com/api-create.php?url=" + noUrl);
            var noTinyUrl = client.DownloadString(address);
            string agentMessage = $"Dear {recepientName}, {claim.CurrentClaimOwner} visit you on Date: {message.Time}.  Click  (Yes){yesTinyUrl} or  (No){noTinyUrl}";

            var finalMessage = $"{agentMessage}             SMS Sent Date: {DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm")} {logo}";
            bool priority = true;

            var response = SMS.API.SendSingleMessage("+" + mobile, finalMessage, device, timestamp, isMMS, null, priority);

            var scheduleMessage = new ClaimMessage
            {
                Message = finalMessage,
                ClaimsInvestigationId = message.ClaimId,
                RecepicientEmail = recepientName,
                SenderEmail = claim.CurrentClaimOwner,
                UpdatedBy = claim.CurrentClaimOwner,
                Updated = DateTime.UtcNow
            };
            claim.ClaimMessages.Add(scheduleMessage);
            context.SaveChanges();

            return claim;
        }

        public async Task<ClaimsInvestigation> ReplyVerifySchedule(string id, string confirm = "N")
        {
            var claim = context.ClaimsInvestigation
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

            string recepientName = string.Empty;
            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
            {
                recepientName = claim.CustomerDetail.CustomerName;
            }
            else if (claim.PolicyDetail.ClaimType == ClaimType.DEATH)
            {
                recepientName = beneficiary.BeneficiaryName;
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

            string agentMessage = $"Dear {claim.CurrentClaimOwner},  {recepientName} Reply: Your Visit : {confirm}";

            var finalMessage = $"{agentMessage} Dated: {DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm")} {logo}";
            bool priority = true;

            var agent = context.VendorApplicationUser.FirstOrDefault(u => u.Email == claim.CurrentClaimOwner);

            var response = SMS.API.SendSingleMessage("+" + agent.PhoneNumber, finalMessage, device, timestamp, isMMS, null, priority);

            var scheduleMessage = new ClaimMessage
            {
                Message = finalMessage,
                ClaimsInvestigationId = id,
                RecepicientEmail = claim.CurrentClaimOwner,
                SenderEmail = recepientName,
                UpdatedBy = recepientName,
                Updated = DateTime.UtcNow
            };
            claim.ClaimMessages.Add(scheduleMessage);
            context.SaveChanges();
            return claim;
        }
    }
}