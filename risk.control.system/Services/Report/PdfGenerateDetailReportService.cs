using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using Gehtsoft.PDFFlow.Utils;
using risk.control.system.Models;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Report
{
    public interface IPdfGenerateDetailReportService
    {
        Task<SectionBuilder> Build(SectionBuilder section, InvestigationTask investigation, ReportTemplate investigationReport, bool isClaim = true);
    }

    internal class PdfGenerateDetailReportService(IPdfGenerateAgentLocationService agentService,
        IPdfGenerateFaceLocationService faceService, IPdfGenerateDocumentLocationService documentService,
        IWebHostEnvironment env,
        IImageConverter imageConverter,
        IPdfGenerateQuestionLocationService questionService) : IPdfGenerateDetailReportService
    {
        internal static readonly FontBuilder FNT9 = Fonts.Helvetica(9f);
        public IPdfGenerateAgentLocationService _agentService = agentService;
        private readonly IPdfGenerateFaceLocationService _faceService = faceService;
        private readonly IPdfGenerateDocumentLocationService _documentService = documentService;
        private readonly IWebHostEnvironment _env = env;
        private readonly IImageConverter _imageConverter = imageConverter;
        private readonly IPdfGenerateQuestionLocationService _questionService = questionService;

        public async Task<SectionBuilder> Build(SectionBuilder section, InvestigationTask investigation, ReportTemplate investigationReport, bool isClaim = true)
        {
            var paragraph = section.AddParagraph();

            var tableBuilder = section.AddTable().SetBorder(Stroke.Solid);

            tableBuilder.AddColumnPercentToTable("Agency Logo", 35).AddColumnPercentToTable("Investigating Agency Name", 65);
            var rowBuilder = tableBuilder.AddRow();

            var pngBytes = _imageConverter.ConvertToPngFromPath(_env, investigation.Vendor!.DocumentUrl!);
            rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center).AddParagraph().AddInlineImage(pngBytes).SetWidth(160F);
            rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center).AddParagraph(investigation.Vendor!.Email).SetFontSize(14);

            int locationCount = 1;
            foreach (var loc in investigationReport.LocationReport)
            {
                if (loc.ValidationExecuted)
                {
                    section.AddParagraph().SetLineSpacing(1).AddText($"{locationCount}. Location Verified: {loc.LocationName}").SetBold().SetFontSize(14);
                    section = await _agentService.Build(section, loc, isClaim);
                    section = await _faceService.Build(section, loc, isClaim);
                    section = await _documentService.Build(section, loc, isClaim);
                    section = _questionService.Build(section, loc);
                    section.AddParagraph().AddText(""); // Additional spacing
                    section.AddParagraph().AddText("----------------------------------------------").SetFontSize(10).SetItalic();
                    section.AddParagraph().AddText("");
                    locationCount++;
                }
            }
            section.AddParagraph().AddText("");
            AddEnquiry(section, investigation);
            section.AddParagraph().AddText("");
            section = AddRemarks(section, "Agent Remarks", investigation.InvestigationReport!.AgentRemarks!);
            section = AddRemarks(section, "Agent Edited Remarks", investigation.InvestigationReport.AgentRemarksEdit!);
            section = AddRemarks(section, "Agency Remarks", investigation.InvestigationReport.SupervisorRemarks!);
            return section;
        }
        private void AddEnquiry(SectionBuilder section, InvestigationTask investigation)
        {
            if (investigation.InvestigationReport!.EnquiryRequests != null && investigation.InvestigationReport.EnquiryRequests.Count != 0)
            {
                section.AddParagraph().SetLineSpacing(1).AddText("Enquiry Report").SetBold().SetFontSize(14);
                var questionTableBuilder = section.AddTable().SetBorder(Stroke.Solid);
                questionTableBuilder.AddColumnPercentToTable("Detailed Question", 35).AddColumnPercentToTable("Detailed Answer", 35).AddColumnPercentToTable("Time", 10).AddColumnPercentToTable("Query Attachment", 10).AddColumnPercentToTable("Answer Attachment", 10);
                var questionRowBuilder = questionTableBuilder.AddRow();
                questionRowBuilder.AddCell().AddParagraph().AddText(investigation.InvestigationReport!.EnquiryRequest!.DescriptiveQuestion ?? "N/A").SetFontSize(10);
                questionRowBuilder.AddCell().AddParagraph().AddText(investigation.InvestigationReport.EnquiryRequest.DescriptiveAnswer ?? "N/A").SetFontSize(10);
                questionRowBuilder.AddCell().AddParagraph().AddText(investigation.InvestigationReport.EnquiryRequest.Updated?.ToString("dd-MMM-yy hh:mm tt") ?? "N/A").SetFontSize(8);
                if (investigation.InvestigationReport!.EnquiryRequest!.QuestionImageAttachment != null)
                {
                    var pngBytes = _imageConverter.ConvertToPng(investigation.InvestigationReport.EnquiryRequest.QuestionImageAttachment);
                    questionRowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center).AddParagraph().AddInlineImage(pngBytes).SetWidth(40);
                }
                else
                {
                    questionRowBuilder.AddCell().AddParagraph().AddText("");
                }

                if (investigation.InvestigationReport!.EnquiryRequest!.AnswerImageAttachment != null)
                {
                    var pngBytes = _imageConverter.ConvertToPng(investigation.InvestigationReport.EnquiryRequest.AnswerImageAttachment);
                    questionRowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center).AddParagraph().AddInlineImage(pngBytes).SetWidth(40);
                }
                else
                {
                    questionRowBuilder.AddCell().AddParagraph().AddText("");
                }

                var tableBuilder = section.AddTable().SetBorder(Stroke.Solid);

                tableBuilder.AddColumnPercentToTable("Multiple Choice Question", 60).AddColumnPercentToTable("Selected Answer", 30).AddColumnPercentToTable("Time", 10);

                foreach (var request in investigation.InvestigationReport.EnquiryRequests)
                {
                    var rowBuilder = tableBuilder.AddRow();
                    rowBuilder.AddCell().AddParagraph().AddText(request.MultipleQuestionText ?? "N/A").SetFontSize(10);
                    rowBuilder.AddCell().AddParagraph().AddText(request.AnswerSelected ?? "N/A").SetFontSize(10);
                    rowBuilder.AddCell().AddParagraph().AddText($"{request.Created:dd-MMM-yy hh:mm tt}").SetFontSize(8);
                }
            }
        }
        private static SectionBuilder AddRemarks(SectionBuilder section, string title, string content)
        {
            var table = section.AddTable().SetBorder(Stroke.Solid);
            table.AddColumnPercentToTable("", 30);
            table.AddColumnPercentToTable("", 70);
            var row = table.AddRow();
            row.AddCell().AddParagraph(title).SetFontSize(12).SetBold();
            row.AddCell().AddParagraph(string.IsNullOrWhiteSpace(content) ? "N/A" : content).SetFontSize(11);
            return section;
        }
    }
}