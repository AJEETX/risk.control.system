using Microsoft.AspNetCore.Hosting;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using risk.control.system.Models;
namespace risk.control.system.Services
{
    public interface IInvestigationReportPdfService
    {
        byte[] GeneratePdf(InvestigationTask task, ReportTemplate report);
        InvestigationTask SaveReport(InvestigationTask task, ReportTemplate report);
    }

    public class InvestigationReportPdfService : IInvestigationReportPdfService
    {
        public byte[] GeneratePdf(InvestigationTask task,  ReportTemplate report)
        {
            var document = new InvestigationReportPdf(task, report);
            return document.GeneratePdf();
        }

        public InvestigationTask SaveReport(InvestigationTask task, ReportTemplate report)
        {
            try
            {
                var document = new InvestigationReportPdf(task, report);

                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                var reportFilename = "report" + task.Id + ".pdf";

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "reports", reportFilename);

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                // 👉 Saves PDF to disk
                document.GeneratePdf(filePath);
                task.InvestigationReport.PdfReportFilePath = filePath;

                return task;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            
        }
    }

    public class InvestigationReportPdf : IDocument
    {
        private readonly InvestigationTask _task;
        private readonly ReportTemplate _report;

        public InvestigationReportPdf(InvestigationTask task, ReportTemplate report)
        {
            _task = task;
            _report = report;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Generated on ");
                    x.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm"));
                });
            });
        }

        // ---------------- HEADER ----------------
        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("INVESTIGATION REPORT")
                        .FontSize(18)
                        .Bold();

                    col.Item().Text($"Case No: {_task.PolicyDetail?.ContractNumber}");
                    col.Item().Text($"Status: {_task.Status}");
                });
            });
        }

        // ---------------- CONTENT ----------------
        void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(15);

                col.Item().Element(ComposeInsurerSection);
                col.Item().Element(ComposePolicySection);
                col.Item().Element(ComposeCustomerSection);
                col.Item().Element(ComposeBeneficiarySection);

                foreach (var location in _report.LocationReport)
                    col.Item().Element(c => ComposeLocation(c, location));
            });
        }

        // ---------------- INSURER ----------------
        void ComposeInsurerSection(IContainer container)
        {
            var insurer = _task.ClientCompany;

            container.Element(Block).Column(col =>
            {
                col.Item().Text("Insurer Details").Bold();
                col.Item().Text($"Name: {insurer?.Name}");
                col.Item().Text($"Email: {insurer?.Email}");
                col.Item().Text($"Phone: {insurer?.PhoneNumber}");
            });
        }

        // ---------------- POLICY ----------------
        void ComposePolicySection(IContainer container)
        {
            var p = _task.PolicyDetail;

            container.Element(Block).Column(col =>
            {
                col.Item().Text("Policy Details").Bold();
                col.Item().Text($"Type: {p?.InsuranceType}");
                col.Item().Text($"Sum Assured: {p?.SumAssuredValue:N2}");
                col.Item().Text($"Incident Date: {p?.DateOfIncident:dd-MMM-yyyy}");
                col.Item().Text($"Cause of Loss: {p?.CauseOfLoss}");
            });
        }

        // ---------------- CUSTOMER ----------------
        void ComposeCustomerSection(IContainer container)
        {
            var c = _task.CustomerDetail;

            container.Element(Block).Column(col =>
            {
                col.Item().Text("Customer Details").Bold();
                col.Item().Text($"Name: {c?.Name}");
                col.Item().Text($"DOB: {c?.DateOfBirth:dd-MMM-yyyy}");
                col.Item().Text($"Phone: {c?.PhoneNumber}");

                if (c?.ProfilePicture != null)
                {
                    col.Item().Height(100).Image(c.ProfilePicture);
                }
            });
        }

        // ---------------- BENEFICIARY ----------------
        void ComposeBeneficiarySection(IContainer container)
        {
            var b = _task.BeneficiaryDetail;

            container.Element(Block).Column(col =>
            {
                col.Item().Text("Beneficiary Details").Bold();
                col.Item().Text($"Name: {b?.Name}");
                col.Item().Text($"Relation: {b?.BeneficiaryRelation?.Name}");
                col.Item().Text($"Phone: {b?.PhoneNumber}");
            });
        }

        // ---------------- LOCATION ----------------
        void ComposeLocation(IContainer container, LocationReport location)
        {
            container.Element(Block).Column(col =>
            {
                col.Item().Text($"Location: {location.LocationName}")
                    .Bold()
                    .FontSize(12);

                // Agent ID
                if (location.AgentIdReport != null)
                {
                    col.Item().Text("Agent Verification").Bold();
                    col.Item().Text($"Confidence: {location.AgentIdReport.DigitalIdImageMatchConfidence}");
                    col.Item().Text($"Similarity: {location.AgentIdReport.Similarity}%");
                }

                // Face ID
                foreach (var face in location.FaceIds ?? [])
                {
                    col.Item().Text("Face ID Verification").Bold();
                    col.Item().Text($"Similarity: {face.Similarity}%");

                    if (face.Image != null)
                        col.Item().Height(120).Image(face.Image);
                }

                // Documents
                foreach (var doc in location.DocumentIds ?? [])
                {
                    col.Item().Text($"Document: {doc.ReportType}");
                    if (doc.Image != null)
                        col.Item().Height(120).Image(doc.Image);
                }

                // Media
                foreach (var media in location.MediaReports ?? [])
                {
                    col.Item().Text($"Media: {media.MediaType} ({media.MediaExtension})");
                    col.Item().Text($"Transcript: {media.Transcript}");
                }

                // Questions
                if (location.Questions?.Any() == true)
                {
                    col.Item().Text("Questionnaire").Bold();

                    foreach (var q in location.Questions)
                    {
                        col.Item().Text($"Q: {q.QuestionText}");
                        col.Item().Text($"A: {q.AnswerText ?? "Not Answered"}");
                    }
                }
            });
        }

        // ---------------- COMMON BLOCK ----------------
        static IContainer Block(IContainer container) =>
            container.Padding(10)
                     .Border(1)
                     .BorderColor(Colors.Grey.Lighten2);
    }

}
