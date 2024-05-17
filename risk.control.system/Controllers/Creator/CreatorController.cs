using AspNetCoreHero.ToastNotification.Notyf;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;

using SmartBreadcrumbs.Attributes;
using AspNetCoreHero.ToastNotification.Abstractions;
using risk.control.system.Data;
using risk.control.system.Services;

namespace risk.control.system.Controllers.Creator
{
    public partial class CreatorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmpanelledAgencyService empanelledAgencyService;
        private readonly IFtpService ftpService;
        private readonly INotyfService notifyService;
        private readonly IInvestigationReportService investigationReportService;
        private readonly IClaimPolicyService claimPolicyService;

        public CreatorController(ApplicationDbContext context,
            IEmpanelledAgencyService empanelledAgencyService,
            IFtpService ftpService,
            INotyfService notifyService,
            IInvestigationReportService investigationReportService,
            IClaimPolicyService claimPolicyService)
        {
            _context = context;
            this.claimPolicyService = claimPolicyService;
            this.empanelledAgencyService = empanelledAgencyService;
            this.ftpService = ftpService;
            this.notifyService = notifyService;
            this.investigationReportService = investigationReportService;
        }

    }
}
