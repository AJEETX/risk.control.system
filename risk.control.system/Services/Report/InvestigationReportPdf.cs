using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;

//using QuestPDF.Fluent;
//using QuestPDF.Helpers;
//using QuestPDF.Infrastructure;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Report
{
    public interface IInvestigationReportPdfService
    {
        byte[] GeneratePdf(InvestigationTask task, ReportTemplate report);

        Task<CaseTransactionModel> GetClaimPdfReport(string currentUserEmail, long id);

        //InvestigationTask SaveReport(InvestigationTask task, ReportTemplate report);
    }

    public class InvestigationReportPdfService : IInvestigationReportPdfService
    {
        private readonly ApplicationDbContext _context;

        public InvestigationReportPdfService(ApplicationDbContext context)
        {
            this._context = context;
        }

        public async Task<CaseTransactionModel> GetClaimPdfReport(string currentUserEmail, long id)
        {
            var caseTask = await _context.Investigations.AsNoTracking()
                .Include(c => c.CaseMessages)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
                .Include(c => c.InvestigationTimeline)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.ClientCompany)
                .Include(c => c.Vendor)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .FirstOrDefaultAsync(m => m.Id == id);
            var maskedCustomerContact = new string('*', caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4) + caseTask.CustomerDetail.PhoneNumber.ToString().Substring(caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4);
            caseTask.CustomerDetail.PhoneNumber = maskedCustomerContact;
            var maskedBeneficiaryContact = new string('*', caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + caseTask.BeneficiaryDetail.PhoneNumber.ToString().Substring(caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
            caseTask.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;
            var companyUser = await _context.ApplicationUser.AsNoTracking().Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var lastHistory = caseTask.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            var timeTaken = DateTime.UtcNow - caseTask.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";

            var invoice = await _context.VendorInvoice.AsNoTracking().FirstOrDefaultAsync(i => i.InvestigationReportId == caseTask.InvestigationReportId);
            var templates = await _context.ReportTemplates.AsNoTracking()
               .Include(r => r.LocationReport)
                  .ThenInclude(l => l.AgentIdReport)
              .Include(r => r.LocationReport)
               .ThenInclude(l => l.MediaReports)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.FaceIds)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.DocumentIds)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.Questions)
                  .FirstOrDefaultAsync(q => q.Id == caseTask.ReportTemplateId);

            caseTask.InvestigationReport.ReportTemplate = templates;

            var tracker = await _context.PdfDownloadTracker.AsNoTracking()
                          .FirstOrDefaultAsync(t => t.ReportId == id && t.UserEmail == currentUserEmail);
            bool canDownload = true;
            if (tracker != null)
            {
                canDownload = tracker.DownloadCount <= 3;
                tracker.DownloadCount++;
                tracker.LastDownloaded = DateTime.UtcNow;
                _context.PdfDownloadTracker.Update(tracker);
            }
            else
            {
                tracker = new PdfDownloadTracker
                {
                    ReportId = id,
                    UserEmail = currentUserEmail,
                    DownloadCount = 1,
                    LastDownloaded = DateTime.UtcNow
                };
                _context.PdfDownloadTracker.Add(tracker);
            }
            _context.SaveChanges();
            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = caseTask,
                CaseIsValidToAssign = caseTask.IsValidCaseData(),
                Beneficiary = caseTask.BeneficiaryDetail,
                Assigned = caseTask.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                TimeTaken = totalTimeTaken,
                VendorInvoice = invoice,
                CanDownload = canDownload,
                Withdrawable = (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }

        public byte[] GeneratePdf(InvestigationTask task, ReportTemplate report)
        {
            //var document = new InvestigationReportPdf(task, report);
            //return document.GeneratePdf();
            throw new NotImplementedException();
        }

        //QUESTPDF

        //public InvestigationTask SaveReport(InvestigationTask task, ReportTemplate report)
        //{
        //    try
        //    {
        //        var document = new InvestigationReportPdf(task, report);

        //        string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        //        if (!Directory.Exists(folder))
        //        {
        //            Directory.CreateDirectory(folder);
        //        }
        //        var reportFilename = "report" + task.Id + ".pdf";

        //        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "reports", reportFilename);

        //        // Ensure directory exists
        //        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        //        // 👉 Saves PDF to disk
        //        document.GeneratePdf(filePath);
        //        task.InvestigationReport.PdfReportFilePath = filePath;

        //        return task;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }

        //}
    }

    //public class InvestigationReportPdf : IDocument
    //{
    //    private readonly InvestigationTask _task;
    //    private readonly ReportTemplate _report;

    //    public InvestigationReportPdf(InvestigationTask task, ReportTemplate report)
    //    {
    //        _task = task;
    //        _report = report;
    //    }

    //    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    //    public void Compose(IDocumentContainer container)
    //    {
    //        container.Page(page =>
    //        {
    //            page.Margin(30);
    //            page.Size(PageSizes.A4);
    //            page.DefaultTextStyle(x => x.FontSize(10));

    //            page.Header().Element(ComposeHeader);
    //            page.Content().Element(ComposeContent);
    //            page.Footer().AlignCenter().Text(x =>
    //            {
    //                x.Span("Generated on ");
    //                x.Span(DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm"));
    //            });
    //        });
    //    }

    //    // ---------------- HEADER ----------------
    //    void ComposeHeader(IContainer container)
    //    {
    //        container.Row(row =>
    //        {
    //            row.RelativeItem().Column(col =>
    //            {
    //                col.Item().Text("INVESTIGATION REPORT")
    //                    .FontSize(18)
    //                    .Bold();

    //                col.Item().Text($"Case No: {_task.PolicyDetail?.ContractNumber}");
    //                col.Item().Text($"Status: {_task.Status}");
    //            });
    //        });
    //    }

    //    // ---------------- CONTENT ----------------
    //    void ComposeContent(IContainer container)
    //    {
    //        container.Column(col =>
    //        {
    //            col.Spacing(15);

    //            col.Item().Element(ComposeInsurerSection);
    //            col.Item().Element(ComposePolicySection);
    //            col.Item().Element(ComposeCustomerSection);
    //            col.Item().Element(ComposeBeneficiarySection);

    //            foreach (var location in _report.LocationReport)
    //                col.Item().Element(c => ComposeLocation(c, location));
    //        });
    //    }

    //    // ---------------- INSURER ----------------
    //    void ComposeInsurerSection(IContainer container)
    //    {
    //        var insurer = _task.ClientCompany;

    //        container.Element(Block).Column(col =>
    //        {
    //            col.Item().Text("Insurer Details").Bold();
    //            col.Item().Text($"Name: {insurer?.Name}");
    //            col.Item().Text($"Email: {insurer?.Email}");
    //            col.Item().Text($"Phone: {insurer?.PhoneNumber}");
    //        });
    //    }

    //    // ---------------- POLICY ----------------
    //    void ComposePolicySection(IContainer container)
    //    {
    //        var p = _task.PolicyDetail;

    //        container.Element(Block).Column(col =>
    //        {
    //            col.Item().Text("Policy Details").Bold();
    //            col.Item().Text($"Type: {p?.InsuranceType}");
    //            col.Item().Text($"Sum Assured: {p?.SumAssuredValue:N2}");
    //            col.Item().Text($"Incident Date: {p?.DateOfIncident:dd-MMM-yyyy}");
    //            col.Item().Text($"Cause of Loss: {p?.CauseOfLoss}");
    //        });
    //    }

    //    // ---------------- CUSTOMER ----------------
    //    void ComposeCustomerSection(IContainer container)
    //    {
    //        var c = _task.CustomerDetail;

    //        container.Element(Block).Column(col =>
    //        {
    //            col.Item().Text("Customer Details").Bold();
    //            col.Item().Text($"Name: {c?.Name}");
    //            col.Item().Text($"DOB: {c?.DateOfBirth:dd-MMM-yyyy}");
    //            col.Item().Text($"Phone: {c?.PhoneNumber}");

    //            if (c?.ProfilePicture != null)
    //            {
    //                col.Item().Height(100).Image(c.ProfilePicture);
    //            }
    //        });
    //    }

    //    // ---------------- BENEFICIARY ----------------
    //    void ComposeBeneficiarySection(IContainer container)
    //    {
    //        var b = _task.BeneficiaryDetail;

    //        container.Element(Block).Column(col =>
    //        {
    //            col.Item().Text("Beneficiary Details").Bold();
    //            col.Item().Text($"Name: {b?.Name}");
    //            col.Item().Text($"Relation: {b?.BeneficiaryRelation?.Name}");
    //            col.Item().Text($"Phone: {b?.PhoneNumber}");
    //        });
    //    }

    //    // ---------------- LOCATION ----------------
    //    void ComposeLocation(IContainer container, LocationReport location)
    //    {
    //        container.Element(Block).Column(col =>
    //        {
    //            col.Item().Text($"Location: {location.LocationName}")
    //                .Bold()
    //                .FontSize(12);

    //            // Agent ID
    //            if (location.AgentIdReport != null)
    //            {
    //                col.Item().Text("Agent Verification").Bold();
    //                col.Item().Text($"Confidence: {location.AgentIdReport.DigitalIdImageMatchConfidence}");
    //                col.Item().Text($"Similarity: {location.AgentIdReport.Similarity}%");
    //            }

    //            // Face ID
    //            foreach (var face in location.FaceIds ?? [])
    //            {
    //                col.Item().Text("Face ID Verification").Bold();
    //                col.Item().Text($"Similarity: {face.Similarity}%");

    //                if (face.Image != null)
    //                    col.Item().Height(120).Image(face.Image);
    //            }

    //            // Documents
    //            foreach (var doc in location.DocumentIds ?? [])
    //            {
    //                col.Item().Text($"Document: {doc.ReportType}");
    //                if (doc.Image != null)
    //                    col.Item().Height(120).Image(doc.Image);
    //            }

    //            // Media
    //            foreach (var media in location.MediaReports ?? [])
    //            {
    //                col.Item().Text($"Media: {media.MediaType} ({media.MediaExtension})");
    //                col.Item().Text($"Transcript: {media.Transcript}");
    //            }

    //            // Questions
    //            if (location.Questions?.Any() == true)
    //            {
    //                col.Item().Text("Questionnaire").Bold();

    //                foreach (var q in location.Questions)
    //                {
    //                    col.Item().Text($"Q: {q.QuestionText}");
    //                    col.Item().Text($"A: {q.AnswerText ?? "Not Answered"}");
    //                }
    //            }
    //        });
    //    }

    //    // ---------------- COMMON BLOCK ----------------
    //    static IContainer Block(IContainer container) =>
    //        container.Padding(10)
    //                 .Border(1)
    //                 .BorderColor(Colors.Grey.Lighten2);
    //}
}