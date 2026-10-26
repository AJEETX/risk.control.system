using Hangfire;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Tool;

namespace risk.control.system.Services.Agent
{
    public interface IAgentMeetingSetupService
    {
        Task<bool> RequestSchedule(ScheduleRequest request, string portalUrl);
        Task<bool> ClearSchedule(long caseId);
        Task<ScheduleResponse> GetScheduleResponse(long caseId);
        Task<ScheduleResponse> GetScheduleResponseForReport(long caseId);
        Task<(bool, string?)> GetSchedule(long caseId);
        Task<bool> SetAgentCallInfo(AgentCallRequest agentCallRequest);
        Task<List<AgentCallRequest>> GetAgentCallInfo(long caseId);
    }
    internal class AgentMeetingSetupService(ApplicationDbContext context,
        IFileStorageService fileStorageService,
        IBackgroundJobClient backgroundJobClient,
        ITinyUrlService tinyUrlService,
        IDataProtectionProvider provider,
        ILogger<AgentMeetingSetupService> logger,
        ISmsService SmsService) : IAgentMeetingSetupService
    {
        private readonly IDataProtector _protector = provider.CreateProtector(Applicationsettings.ENCRYPT_URL_QUERYSTRING);
        private readonly ApplicationDbContext _context = context;
        private readonly ISmsService _smsService = SmsService;
        private readonly ITinyUrlService _tinyUrlService = tinyUrlService;
        private readonly IFileStorageService _fileStorageService = fileStorageService;
        private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;
        private readonly ILogger<AgentMeetingSetupService> _logger = logger;
        private static readonly string _logo = Applicationsettings.WEBSITE_SITE_URL;
        public async Task<(bool, string?)> GetSchedule(long caseId)
        {
            try
            {
                var caseDetails = await _context.Investigations
                                .Include(c => c.ScheduleMeeting)
                                .FirstOrDefaultAsync(c => c.Id == caseId);
                if (caseDetails == null)
                {
                    throw new Exception("Case details not found for the given case ID.");
                }
                var scheduled = caseDetails.ScheduleMeeting?.ScheduleAccepted ?? false;
                var scheduleDate = caseDetails.ScheduleMeeting?.ScheduleDateTime != null ? caseDetails.ScheduleMeeting?.ScheduleDateTime.ToString("dd/MM/yyyy HH:mm") : null;
                return (scheduled, scheduleDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching schedule for case ID: {CaseId}", caseId);
                return (false, null);
            }
        }
        public async Task<bool> ClearSchedule(long caseId)
        {
            try
            {
                var caseDetails = await _context.Investigations
                    .Include(c => c.ScheduleMeeting)
                    .FirstOrDefaultAsync(c => c.Id == caseId);
                if (caseDetails == null)
                {
                    throw new Exception("Case details not found for the given case ID.");
                }
                caseDetails.ScheduleMeeting = null;
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while clearing schedule for case ID: {CaseId}", caseId);
                return false;
            }
        }
        public async Task<ScheduleResponse> GetScheduleResponseForReport(long caseId)
        {
            try
            {
                var caseDetails = await _context.Investigations
                    .Include(c => c.ClientCompany)
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.ScheduleMeeting)
                    .FirstOrDefaultAsync(c => c.Id == caseId);
                if (caseDetails == null)
                {
                    throw new Exception("Case details not found for the given case ID.");
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
                long scheduleeId = 0;
                string insurerName = caseDetails?.ClientCompany?.Name ?? string.Empty;
                if (!isClaim)
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
                    scheduleeId = customer.CustomerDetailId;
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
                    scheduleeId = beneficiary.BeneficiaryDetailId;
                }
                var scheduleResponse = new ScheduleResponse
                {
                    IsClaim = isClaim,
                    ScheduledDateTime = caseDetails!.ScheduleMeeting!.ScheduleDateTime,
                    ScheduleRequestedDateTime = caseDetails!.ScheduleMeeting!.ScheduleRequestedDateTime.ToLocalTime(),
                    ScheduleeName = contactNameForSchedule,
                    AgentName = $"{agent.FirstName} {agent.LastName}",
                    PolicyNumber = caseDetails.PolicyDetail!.ContractNumber,
                    AgentId = agent.Id,
                    ScheduleeId = scheduleeId,
                    InsurerName = insurerName,
                    ScheduleRequested = caseDetails.ScheduleMeeting!.ScheduleRequested,
                    ScheduleAccepted = caseDetails.ScheduleMeeting!.ScheduleAccepted
                };
                return scheduleResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while responding to schedule for Case {CaseId}", caseId);
                return new ScheduleResponse();
            }
        }
        public async Task<ScheduleResponse> GetScheduleResponse(long caseId)
        {
            try
            {
                var caseDetails = await _context.Investigations
                    .Include(c => c.ClientCompany)
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.ScheduleMeeting)
                    .FirstOrDefaultAsync(c => !c.Deleted && c.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT && c.Id == caseId);
                if (caseDetails == null)
                {
                    throw new Exception("Case details not found for the given case ID.");
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
                long scheduleeId = 0;
                string insurerName = caseDetails?.ClientCompany?.Name ?? string.Empty;
                if (!isClaim)
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
                    scheduleeId = customer.CustomerDetailId;
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
                    scheduleeId = beneficiary.BeneficiaryDetailId;
                }
                if (caseDetails!.ScheduleMeeting!.ScheduleAccepted || caseDetails!.ScheduleMeeting!.ScheduleRequestedDateTime.AddHours(48) < DateTime.UtcNow)
                {
                    var alreadyScheduled = new ScheduleResponse
                    {
                        IsClaim = isClaim,
                        ScheduledDateTime = caseDetails.ScheduleMeeting.ScheduleDateTime,
                        ScheduleRequestedDateTime = caseDetails.ScheduleMeeting.ScheduleRequestedDateTime.ToLocalTime(),
                        ScheduleeName = contactNameForSchedule,
                        AgentName = $"{agent.FirstName} {agent.LastName}",
                        PolicyNumber = caseDetails.PolicyDetail!.ContractNumber,
                        AgentId = agent.Id,
                        ScheduleeId = scheduleeId,
                        InsurerName = insurerName,
                        ScheduleAccepted = caseDetails!.ScheduleMeeting.ScheduleAccepted
                    };
                    return alreadyScheduled;
                }
                var message = $"Dear {contactNameForSchedule},\n" +
                              $"Your meeting schedule has been confirmed on {caseDetails!.ScheduleMeeting!.ScheduleDateTime:dd-MM-yyyy} at {caseDetails!.ScheduleMeeting!.ScheduleDateTime:HH:mm}.\n\n" +
                              $"Thanks,\n" +
                              $"{_logo}";

                var agentMessage = $"Dear {agent?.FirstName} {agent?.LastName},\n" +
                                   $"The schedule for Case/Policy # {caseDetails!.PolicyDetail!.ContractNumber} has been confirmed by the {contactNameForSchedule}.\n" +
                                    $"The meeting schedule has been confirmed on {caseDetails!.ScheduleMeeting!.ScheduleDateTime:dd-MM-yyyy} at {caseDetails!.ScheduleMeeting!.ScheduleDateTime:HH:mm}.\n\n" +
                                   $"Thanks,\n" +
                                   $"{_logo}";
                await _smsService.DoSendSmsAsync(countryCode, contactNumberForSchedule, message);
                await _smsService.DoSendSmsAsync(agent!.Country!.Code, agent!.Country!.ISDCode.ToString() + agent!.PhoneNumber, agentMessage);
                caseDetails.ScheduleMeeting!.ScheduleAccepted = true;
                _context.Investigations.Update(caseDetails);
                var saved = await _context.SaveChangesAsync() > 0;
                var scheduleResponse = new ScheduleResponse
                {
                    IsClaim = isClaim,
                    ScheduledDateTime = caseDetails.ScheduleMeeting.ScheduleDateTime,
                    ScheduleRequestedDateTime = caseDetails.ScheduleMeeting.ScheduleRequestedDateTime.ToLocalTime(),
                    ScheduleeName = contactNameForSchedule,
                    AgentName = $"{agent.FirstName} {agent.LastName}",
                    PolicyNumber = caseDetails.PolicyDetail!.ContractNumber,
                    AgentId = agent.Id,
                    ScheduleeId = scheduleeId,
                    InsurerName = insurerName,
                    ScheduleAccepted = false
                };
                return scheduleResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while responding to schedule for Case {CaseId}", caseId);
                return new ScheduleResponse();
            }
        }
        public async Task<bool> RequestSchedule(ScheduleRequest request, string portalUrl)
        {
            try
            {
                var caseDetails = await _context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(c => c.ScheduleMeeting)
                .FirstOrDefaultAsync(c => !c.Deleted && c.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT && c.Id == request.CaseId);
                if (caseDetails == null)
                {
                    throw new Exception("Case details not found for the given case ID.");
                }

                var isClaim = caseDetails?.PolicyDetail?.InsuranceType == InsuranceType.CLAIM;
                var agentName = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == caseDetails!.TaskedAgentEmail);
                string contactNumberForSchedule = string.Empty;
                string contactNameForSchedule = string.Empty;
                string countryCode = string.Empty;
                if (!isClaim)
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
        public async Task<bool> SetAgentCallInfo(AgentCallRequest agentCallRequest)
        {
            try
            {
                var caseDetail = await _context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(c => c.AgentCallInfo)
                .FirstOrDefaultAsync(c => !c.Deleted && c.Id == agentCallRequest.CaseId && c.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

                if (caseDetail == null) return false;

                var (fileName, relativePath) = await _fileStorageService.SaveMediaAsync(agentCallRequest.CallRecording!, CONSTANTS.CASE, caseDetail.PolicyDetail!.ContractNumber, CONSTANTS.REPORT);
                agentCallRequest.CallRecordingPath = relativePath;
                caseDetail.AgentCallInfo.Add(agentCallRequest);
                var result = await _context.SaveChangesAsync() > 0;
                var audioCall = await _context.AgentCallRequests.FirstOrDefaultAsync(c => c.CaseId == agentCallRequest.CaseId && c.CallReceivedTime == agentCallRequest.CallReceivedTime && c.StartTime == agentCallRequest.StartTime);
                _backgroundJobClient.Enqueue<ISpeech2TextService>(service => service.ConvertAudioCallSpeech(relativePath, audioCall!.Id));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while setting agent call info for Case {CaseId}", agentCallRequest.CaseId);
                return false;
            }
        }
        public async Task<List<AgentCallRequest>> GetAgentCallInfo(long caseId)
        {
            try
            {
                var caseDetail = await _context.Investigations
                .Include(c => c.AgentCallInfo)
                .FirstOrDefaultAsync(c => !c.Deleted && c.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT && c.Id == caseId);

                return caseDetail?.AgentCallInfo ?? new List<AgentCallRequest>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching agent call info for Case {CaseId}", caseId);
                throw;
            }
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
