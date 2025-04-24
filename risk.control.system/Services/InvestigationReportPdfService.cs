using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using risk.control.system.Models;

namespace risk.control.system.Services
{
    public class InvestigationReportPdfService : IDocument
    {
        private readonly InvestigationReport _report;

        public InvestigationReportPdfService(InvestigationReport report)
        {
            _report = report;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(12));
                page.Content()
                    .Column(column =>
                    {
                        column.Item().Text($"Investigation Report #{_report.Id}").FontSize(16).Bold();
                        column.Item().Text($"Agent Email: {_report.AgentEmail}");
                        column.Item().Text($"Agent Remarks: {_report.AgentRemarks}");
                        column.Item().Text($"Supervisor Remarks: {_report.SupervisorRemarks}");
                        column.Item().Text($"Assessor Remarks: {_report.AssessorRemarks}");

                        column.Item().LineHorizontal(1);

                        column.Item().Text("Questionnaire").FontSize(14).Bold();

                        foreach (var question in _report.CaseQuestionnaire?.Questions ?? new List<Question>())
                        {
                            column.Item().Text($"Q: {question.QuestionText}");
                            column.Item().Text($"A: {question.AnswerText}");
                            column.Item().Text("");
                        }

                        column.Item().LineHorizontal(1);

                        column.Item().Text("Digital ID Info").FontSize(14).Bold();
                        column.Item().Text($"Location: {_report.DigitalIdReport?.DigitalIdImageLocationAddress}");
                        column.Item().Text($"Distance: {_report.DigitalIdReport?.Distance}");
                        column.Item().Text($"Match Confidence: {_report.DigitalIdReport?.DigitalIdImageMatchConfidence}");

                        column.Item().Text("");

                        column.Item().Text("PAN ID Info").FontSize(14).Bold();
                        column.Item().Text($"Address: {_report.PanIdReport?.DocumentIdImageLocationAddress}");
                        column.Item().Text($"Document Type: {_report.PanIdReport?.DocumentIdImageType}");
                    });
            });
        }
    }
}