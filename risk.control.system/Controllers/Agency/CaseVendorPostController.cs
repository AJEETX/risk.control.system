using System.Net;
using System.Web;

using AspNetCoreHero.ToastNotification.Abstractions;

using Hangfire;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Agency
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    public class CaseVendorPostController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly IProcessCaseService processCaseService;
        private readonly IVendorInvestigationService vendorInvestigationService;
        private readonly INotyfService notifyService;
        private readonly IMailService mailboxService;
        private readonly ILogger<CaseVendorPostController> logger;
        private readonly ApplicationDbContext _context;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IHttpContextAccessor httpContextAccessor;

        public CaseVendorPostController(
            IProcessCaseService processCaseService,
            IVendorInvestigationService vendorInvestigationService,
            INotyfService notifyService,
            IBackgroundJobClient backgroundJobClient,
            IHttpContextAccessor httpContextAccessor,
            IMailService mailboxService,
            ILogger<CaseVendorPostController> logger,
            ApplicationDbContext context)
        {
            this.processCaseService = processCaseService;
            this.vendorInvestigationService = vendorInvestigationService;
            this.notifyService = notifyService;
            this.mailboxService = mailboxService;
            this.logger = logger;
            this.backgroundJobClient = backgroundJobClient;
            this.httpContextAccessor = httpContextAccessor;
            _context = context;
            UserList = new List<UsersViewModel>();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        public async Task<IActionResult> AllocateToVendorAgent(string selectedcase, long claimId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedcase) || claimId < 1)
                {
                    notifyService.Error($"No case selected!!!. Please select case to be allocate.", 3);
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendorAgent = _context.VendorApplicationUser.Include(a => a.Vendor).FirstOrDefault(c => c.Id.ToString() == selectedcase);
                if (vendorAgent == null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var claim = await vendorInvestigationService.AssignToVendorAgent(vendorAgent.Email, currentUserEmail, vendorAgent.VendorId.Value, claimId);
                if (claim == null)
                {
                    notifyService.Error("OOPs !!!..Error occurred.");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimAssignmentToVendorAgent(currentUserEmail, claimId, vendorAgent.Email, vendorAgent.VendorId.Value, baseUrl));

                notifyService.Custom($"Case #{claim.PolicyDetail.ContractNumber} tasked to {vendorAgent.Email}", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AGENT.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        public async Task<IActionResult> SubmitReport(CaseInvestigationVendorsModel model, string remarks, long claimId)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(AgentController.GetInvestigate), "Agent", new { selectedcase = model.ClaimsInvestigation.Id });
                }

                var (vendor, contract) = await vendorInvestigationService.SubmitToVendorSupervisor(currentUserEmail, claimId,
                    WebUtility.HtmlDecode(remarks));
                if (vendor == null)
                {
                    notifyService.Error("OOPs !!!..Error submitting.");
                    return RedirectToAction(nameof(AgentController.GetInvestigate), "Agent", new { selectedcase = claimId });
                }
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimReportSubmitToVendorSupervisor(currentUserEmail, claimId, baseUrl));

                notifyService.Custom($"Case #{contract} report submitted", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(AgentController.Index), "Agent");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(AgentController.GetInvestigate), "Agent", new { selectedcase = claimId });
            }
        }
        private string GetSelectedOptionText(CaseInvestigationVendorsModel model, int questionId)
        {
            var question = model.QuestionFormViewModel.Questions.FirstOrDefault(q => q.Id == questionId);
            if (question == null) return "N/A";
            if (question.QuestionType == "text")
                return model.QuestionFormViewModel.Answers.TryGetValue(questionId, out var _val) ? _val : "N/A";

            if (question.QuestionType == "file")
                return model.QuestionFormViewModel.Answers.TryGetValue(questionId, out var _val) ? _val : "N/A";
            if (question.QuestionType == "date")
                return model.QuestionFormViewModel.Answers.TryGetValue(questionId, out var _val) ? _val : "N/A";
            if (question.QuestionType == "checkbox")
                return model.QuestionFormViewModel.Answers.TryGetValue(questionId, out var _val) ? _val : "N/A";
            if (question.QuestionType == "radio")
                return model.QuestionFormViewModel.Answers.TryGetValue(questionId, out var _val) ? _val : "N/A";
            if (question.QuestionType == "dropdown")
            {
                var selectedValue = model.QuestionFormViewModel.Answers.TryGetValue(questionId, out var value) ? value : "N/A";
                var options = question.Options?.Split(',') ?? Array.Empty<string>();
                return options.Contains(selectedValue) ? selectedValue : "N/A";
            }
            return "N/A";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        public async Task<IActionResult> ProcessReport(string supervisorRemarks, string supervisorRemarkType, long claimId, string remarks = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(supervisorRemarks) || claimId < 1)
                {
                    notifyService.Error("No Supervisor remarks entered!!!. Please enter remarks.");
                    return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), new { selectedcase = claimId });
                }
                string userEmail = HttpContext?.User?.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), new { selectedcase = claimId });
                }

                var reportUpdateStatus = SupervisorRemarkType.OK;

                var success = await processCaseService.ProcessAgentReport(userEmail, supervisorRemarks, claimId, reportUpdateStatus, Request.Form.Files.FirstOrDefault(), remarks);

                if (success != null)
                {
                    var agencyUser = _context.VendorApplicationUser.Include(a => a.Vendor).FirstOrDefault(c => c.Email == userEmail);
                    var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                    var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                    var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                    backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimReportSubmitToCompany(userEmail, claimId, baseUrl));
                    //await mailboxService.NotifyClaimReportSubmitToCompany(userEmail, claimId);

                    notifyService.Custom($"Case #{success.PolicyDetail.ContractNumber}  report submitted to Company", 3, "green", "far fa-file-powerpoint");
                }
                else
                {
                    notifyService.Custom($"Case #{success.PolicyDetail.ContractNumber}  report sent to review", 3, "orange", "far fa-file-powerpoint");
                }
                return RedirectToAction(nameof(VendorInvestigationController.ClaimReport), "VendorInvestigation");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.GetInvestigateReport), new { selectedcase = claimId });
            }
        }

        [HttpPost]
        [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawCase(CaseTransactionModel model, long claimId, string policyNumber)
        {
            try
            {
                if (model == null || claimId < 1)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");

                }
                string userEmail = HttpContext?.User?.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");

                }
                var agency = await processCaseService.WithdrawCase(userEmail, model, claimId);

                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimWithdrawlToCompany(userEmail, claimId, agency.VendorId, baseUrl));

                notifyService.Custom($"Case #{policyNumber}  declined successfully", 3, "red", "far fa-file-powerpoint");

                return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");

            }
        }
        [HttpPost]
        [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawCaseFromAgent(CaseTransactionModel model, long claimId, string policyNumber)
        {
            try
            {
                if (model == null || claimId < 1)
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");

                }
                string userEmail = HttpContext?.User?.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");

                }
                var agency = await processCaseService.WithdrawCaseFromAgent(userEmail, model, claimId);

                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                var jobId = backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimWithdrawlFromAgent(userEmail, claimId, agency.VendorId, baseUrl));

                notifyService.Custom($"Case #{policyNumber} withdrawn from Agent successfully", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");

            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        public async Task<IActionResult> ReplyQuery(long claimId, CaseInvestigationVendorsModel request, EnquiryRequest enquiryRequest, List<EnquiryRequest> enquiryRequests)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
                }
                if (request == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");

                }
                request.InvestigationReport.EnquiryRequest.DescriptiveAnswer = HttpUtility.HtmlEncode(request.InvestigationReport.EnquiryRequest.DescriptiveAnswer);

                IFormFile? messageDocument = Request.Form?.Files?.FirstOrDefault();

                var claim = await processCaseService.SubmitQueryReplyToCompany(currentUserEmail, claimId, request.InvestigationReport.EnquiryRequest, request.InvestigationReport.EnquiryRequests, messageDocument);

                if (claim != null)
                {
                    var agencyUser = _context.VendorApplicationUser.Include(a => a.Vendor).FirstOrDefault(c => c.Email == currentUserEmail);
                    var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                    var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                    var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                    backgroundJobClient.Enqueue(() => mailboxService.NotifySubmitReplyToCompany(currentUserEmail, claimId, baseUrl));

                    notifyService.Success("Enquiry Reply Sent to Company");
                    return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
                }
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");

            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(VendorInvestigationController.Allocate), "VendorInvestigation");
            }
        }
    }
}