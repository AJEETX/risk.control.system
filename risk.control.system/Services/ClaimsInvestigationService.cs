using AspNetCoreHero.ToastNotification.Notyf;

using Hangfire;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Company;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Services
{
    public interface IClaimsInvestigationService
    {
        Task<bool> SubmitNotes(string userEmail, long claimId, string notes);
    }

    public class ClaimsInvestigationService : IClaimsInvestigationService
    {
        private const string CLAIMS = "claims";
        private const string UNDERWRITING = "underwriting";
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IHttpContextAccessor accessor;
        private readonly IPdfReportService reportService;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IProgressService progressService;
        private readonly ICustomApiCLient customApiCLient;

        public ClaimsInvestigationService(ApplicationDbContext context,
            IHttpContextAccessor accessor,
            IPdfReportService reportService,
            IBackgroundJobClient backgroundJobClient,
            IProgressService progressService,
            ICustomApiCLient customApiCLient,
            IWebHostEnvironment webHostEnvironment)
        {
            this._context = context;
            this.accessor = accessor;
            this.reportService = reportService;
            this.backgroundJobClient = backgroundJobClient;
            this.progressService = progressService;
            this.customApiCLient = customApiCLient;
            this.webHostEnvironment = webHostEnvironment;
        }
        public async Task<bool> SubmitNotes(string userEmail, long claimId, string notes)
        {
            var claim = _context.Investigations
               .Include(c => c.CaseNotes)
               .FirstOrDefault(c => c.Id == claimId);
            claim.CaseNotes.Add(new CaseNote
            {
                Comment = notes,
                Sender = userEmail,
                Created = DateTime.Now,
                Updated = DateTime.Now,
                UpdatedBy = userEmail
            });
            _context.Investigations.Update(claim);
            return await _context.SaveChangesAsync() > 0;
        }


    }
}