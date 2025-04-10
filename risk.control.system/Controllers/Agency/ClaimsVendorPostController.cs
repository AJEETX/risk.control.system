﻿using System.Net;
using System.Web;

using AspNetCoreHero.ToastNotification.Abstractions;

using Hangfire;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Extensions;

using NToastNotify;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Agency
{
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME},{AGENT.DISPLAY_NAME}")]
    public class ClaimsVendorPostController : Controller
    {
        public List<UsersViewModel> UserList;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly INotyfService notifyService;
        private readonly IClaimsVendorService vendorService;
        private readonly IMailboxService mailboxService;
        private readonly ApplicationDbContext _context;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IHttpContextAccessor httpContextAccessor;

        public ClaimsVendorPostController(
            IClaimsInvestigationService claimsInvestigationService,
            INotyfService notifyService,
            IBackgroundJobClient backgroundJobClient,
            IHttpContextAccessor httpContextAccessor,
            IClaimsVendorService vendorService,
            IMailboxService mailboxService,
            ApplicationDbContext context)
        {
            this.claimsInvestigationService = claimsInvestigationService;
            this.notifyService = notifyService;
            this.vendorService = vendorService;
            this.mailboxService = mailboxService;
            this.backgroundJobClient = backgroundJobClient;
            this.httpContextAccessor = httpContextAccessor;
            _context = context;
            UserList = new List<UsersViewModel>();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        public async Task<IActionResult> AllocateToVendorAgent(string selectedcase, string claimId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedcase) || string.IsNullOrWhiteSpace(claimId))
                {
                    notifyService.Error($"No case selected!!!. Please select case to be allocate.", 3);
                    return RedirectToAction(nameof(SupervisorController.Allocate), "Supervisor");
                }
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var vendorAgent = _context.VendorApplicationUser.Include(a=>a.Vendor).FirstOrDefault(c => c.Id.ToString() == selectedcase);
                if (vendorAgent == null)
                {
                    notifyService.Error("OOPs !!!..User Not Found");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }

                var claim = await claimsInvestigationService.AssignToVendorAgent(vendorAgent.Email, currentUserEmail, vendorAgent.VendorId.Value, claimId);
                if (claim == null)
                {
                    notifyService.Error("OOPs !!!..Error occurred.");
                    return RedirectToAction(nameof(Index), "Dashboard");
                }
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                backgroundJobClient.Enqueue(()=> mailboxService.NotifyClaimAssignmentToVendorAgent(currentUserEmail, claimId, vendorAgent.Email, vendorAgent.VendorId.Value, baseUrl));

                notifyService.Custom($"Case #{claim.PolicyDetail.ContractNumber} tasked to {vendorAgent.Email}", 3, "green", "far fa-file-powerpoint");

                return RedirectToAction(nameof(SupervisorController.Allocate), "Supervisor");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(Index), "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AGENT.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        public async Task<IActionResult> SubmitReport(string remarks, string question1, string question2, string question3, string question4, string claimId, long caseLocationId)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (currentUserEmail == null)
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(AgentController.GetInvestigate), "Agent", new { selectedcase = claimId });
                }
                if (string.IsNullOrWhiteSpace(remarks) ||
                    string.IsNullOrWhiteSpace(claimId) ||
                    caseLocationId < 1 ||
                    string.IsNullOrWhiteSpace(question1) ||
                    string.IsNullOrWhiteSpace(question2) ||
                    string.IsNullOrWhiteSpace(question3) ||
                    string.IsNullOrWhiteSpace(question4)
                    )
                {
                    notifyService.Error($"No Agent remarks entered!!!. Please enter remarks.", 3);
                    return RedirectToAction(nameof(AgentController.GetInvestigate), "Agent", new { selectedcase = claimId });
                }

                var (vendor , contract )= await claimsInvestigationService.SubmitToVendorSupervisor(currentUserEmail, claimId,
                    WebUtility.HtmlDecode(remarks),
                    WebUtility.HtmlDecode(question1),
                    WebUtility.HtmlDecode(question2),
                    WebUtility.HtmlDecode(question3),
                    WebUtility.HtmlDecode(question4));
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
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(AgentController.GetInvestigate), "Agent", new { selectedcase = claimId });

            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        public async Task<IActionResult> ProcessReport(string supervisorRemarks, string supervisorRemarkType, string claimId, string remarks = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(supervisorRemarks) || string.IsNullOrWhiteSpace(claimId))
                {
                    notifyService.Error("No Supervisor remarks entered!!!. Please enter remarks.");
                    return RedirectToAction(nameof(SupervisorController.GetInvestigateReport), new { selectedcase = claimId });
                }
                string userEmail = HttpContext?.User?.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(SupervisorController.GetInvestigateReport), new { selectedcase = claimId });
                }

                var reportUpdateStatus = SupervisorRemarkType.OK;

                var success = await claimsInvestigationService.ProcessAgentReport(userEmail, supervisorRemarks, claimId, reportUpdateStatus, Request.Form.Files.FirstOrDefault(), remarks);

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
                return RedirectToAction(nameof(SupervisorController.ClaimReport), "Supervisor");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(SupervisorController.GetInvestigateReport), new { selectedcase = claimId });
            }
        }

        [HttpPost]
        [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawCase(ClaimTransactionModel model, string claimId, string policyNumber)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(claimId))
                {
                    notifyService.Error("OOPs !!!..Contact Admin");
                    return RedirectToAction(nameof(SupervisorController.Allocate), "Supervisor");

                }
                string userEmail = HttpContext?.User?.Identity.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(SupervisorController.Allocate), "Supervisor");

                }
                var agency = await claimsInvestigationService.WithdrawCase(userEmail, model, claimId);

                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                backgroundJobClient.Enqueue(() => mailboxService.NotifyClaimWithdrawlToCompany(userEmail, claimId, agency.VendorId, baseUrl));

                notifyService.Custom($"Case #{policyNumber}  declined successfully", 3, "red", "far fa-file-powerpoint");

                return RedirectToAction(nameof(SupervisorController.Allocate), "Supervisor");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(SupervisorController.Allocate), "Supervisor");

            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
        public async Task<IActionResult> ReplyQuery(string claimId, ClaimsInvestigationVendorsModel request, List<string> flexRadioDefault)
        {
            try
            {
                var currentUserEmail = HttpContext.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    notifyService.Error("OOPs !!!..Unauthenticated Access");
                    return RedirectToAction(nameof(SupervisorController.Allocate), "Supervisor");
                }
                if (request == null)
                {
                    notifyService.Error("NOT FOUND !!!..");
                    return RedirectToAction(nameof(SupervisorController.Allocate), "Supervisor");

                }
                request.ClaimsInvestigation.AgencyReport.EnquiryRequest.Answer = HttpUtility.HtmlEncode(request.ClaimsInvestigation.AgencyReport.EnquiryRequest.Answer);

                IFormFile? messageDocument = Request.Form?.Files?.FirstOrDefault();

                var claim = await claimsInvestigationService.SubmitQueryReplyToCompany(currentUserEmail, claimId, request.ClaimsInvestigation.AgencyReport.EnquiryRequest, messageDocument, flexRadioDefault);

                if (claim != null)
                {
                    var agencyUser = _context.VendorApplicationUser.Include(a => a.Vendor).FirstOrDefault(c => c.Email == currentUserEmail);
                    var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                    var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                    var baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";

                    backgroundJobClient.Enqueue(() => mailboxService.NotifySubmitReplyToCompany(currentUserEmail, claimId, baseUrl));

                    notifyService.Success("Enquiry Reply Sent to Company");
                    return RedirectToAction(nameof(SupervisorController.Allocate), "Supervisor");
                }
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(SupervisorController.Allocate), "Supervisor");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                notifyService.Error("OOPs !!!..Contact Admin");
                return RedirectToAction(nameof(SupervisorController.Allocate), "Supervisor");
            }
        }
    }
}