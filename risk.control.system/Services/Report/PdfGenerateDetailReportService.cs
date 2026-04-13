using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using Gehtsoft.PDFFlow.Utils;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services.Report
{
    public interface IPdfGenerateDetailReportService
    {
        Task<SectionBuilder> Build(SectionBuilder section, InvestigationTask investigation, ReportTemplate investigationReport, bool isClaim = true);
    }

    internal class PdfGenerateDetailReportService : IPdfGenerateDetailReportService
    {
        internal static readonly FontBuilder FNT9 = Fonts.Helvetica(9f);
        internal static readonly FontBuilder FNT10 = Fonts.Helvetica(10f);
        internal static readonly FontBuilder FNT12 = Fonts.Helvetica(12f);
        internal static readonly FontBuilder FNT12B = Fonts.Helvetica(12f).SetBold(true);
        internal static readonly FontBuilder FNT20 = Fonts.Helvetica(20f);
        internal static readonly FontBuilder FNT19B = Fonts.Helvetica(19f).SetBold();
        internal static readonly FontBuilder FNT8 = Fonts.Helvetica(8f);
        internal static readonly FontBuilder FNT8_G = Fonts.Helvetica(8f).SetColor(Gehtsoft.PDFFlow.Models.Shared.Color.Gray);
        internal static readonly FontBuilder FNT9B = Fonts.Helvetica(9f).SetBold();
        internal static readonly FontBuilder FNT11B = Fonts.Helvetica(11f).SetBold();
        internal static readonly FontBuilder FNT15 = Fonts.Helvetica(15f);
        internal static readonly FontBuilder FNT16 = Fonts.Helvetica(16f);
        internal static readonly FontBuilder FNT16_R = Fonts.Helvetica(16f).SetColor(Gehtsoft.PDFFlow.Models.Shared.Color.Red);
        internal static readonly FontBuilder FNT16_G = Fonts.Helvetica(16f).SetColor(Gehtsoft.PDFFlow.Models.Shared.Color.Green);
        internal static readonly FontBuilder FNT17 = Fonts.Helvetica(17f);
        internal static readonly FontBuilder FNT18 = Fonts.Helvetica(18f);
        public IPdfGenerateAgentLocationService agentService;
        private readonly IPdfGenerateFaceLocationService faceService;
        private readonly IPdfGenerateDocumentLocationService documentService;
        private readonly IWebHostEnvironment env;
        private readonly IPdfGenerateQuestionLocationService questionService;

        public PdfGenerateDetailReportService(IPdfGenerateAgentLocationService agentService,
            IPdfGenerateFaceLocationService faceService, IPdfGenerateDocumentLocationService documentService,
            IWebHostEnvironment env,
            IPdfGenerateQuestionLocationService questionService)
        {
            this.agentService = agentService;
            this.faceService = faceService;
            this.documentService = documentService;
            this.env = env;
            this.questionService = questionService;
        }

        public async Task<SectionBuilder> Build(SectionBuilder section, InvestigationTask investigation, ReportTemplate investigationReport, bool isClaim = true)
        {
            var paragraph = section.AddParagraph();
            var pngBytes = ImageConverter.ConvertToPngFromPath(env, investigation.Vendor!.DocumentUrl!);
            paragraph.AddInlineImage(pngBytes).SetWidth(100).SetHeight(100); // optional small space between image and text
            paragraph.AddText($" {investigation.Vendor!.Email} : Investigation detail").SetFontSize(18).SetBold().SetUnderline();
            int locationCount = 1;
            foreach (var loc in investigationReport.LocationReport)
            {
                if (loc.ValidationExecuted)
                {
                    section.AddParagraph().SetLineSpacing(1).AddText($"{locationCount}. Location Verified: {loc.LocationName}").SetBold().SetFontSize(14);
                    section = await agentService.Build(section, loc, isClaim);
                    section = await faceService.Build(section, loc, isClaim);
                    section = await documentService.Build(section, loc, isClaim);
                    section = questionService.Build(section, loc);
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
        private static void AddEnquiry(SectionBuilder section, InvestigationTask investigation)
        {
            if (investigation.InvestigationReport!.EnquiryRequests != null && investigation.InvestigationReport.EnquiryRequests.Any())
            {
                section.AddParagraph().SetLineSpacing(1).AddText($"Enquiry Report").SetBold().SetFontSize(14);
                var questionTableBuilder = section.AddTable().SetBorder(Stroke.Solid);
                questionTableBuilder.AddColumnPercentToTable("Detailed Question", 35).AddColumnPercentToTable("Detailed Answer", 35).AddColumnPercentToTable("Time", 10).AddColumnPercentToTable("Query Attachment", 10).AddColumnPercentToTable("Answer Attachment", 10);
                var questionRowBuilder = questionTableBuilder.AddRow();
                questionRowBuilder.AddCell().AddParagraph().AddText(investigation.InvestigationReport!.EnquiryRequest!.DescriptiveQuestion ?? "N/A").SetFontSize(10);
                questionRowBuilder.AddCell().AddParagraph().AddText(investigation.InvestigationReport.EnquiryRequest.DescriptiveAnswer ?? "N/A").SetFontSize(10);
                questionRowBuilder.AddCell().AddParagraph().AddText(investigation.InvestigationReport.EnquiryRequest.Updated?.ToString("dd-MMM-yy hh:mm tt") ?? "N/A").SetFontSize(8);
                if (investigation.InvestigationReport!.EnquiryRequest!.QuestionImageAttachment != null)
                {
                    var pngBytes = ImageConverter.ConvertToPng(investigation.InvestigationReport.EnquiryRequest.QuestionImageAttachment);
                    questionRowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center).AddParagraph().AddInlineImage(pngBytes).SetWidth(40);
                }
                else
                {
                    questionRowBuilder.AddCell().AddParagraph().AddText("");
                }

                if (investigation.InvestigationReport!.EnquiryRequest!.AnswerImageAttachment != null)
                {
                    var pngBytes = ImageConverter.ConvertToPng(investigation.InvestigationReport.EnquiryRequest.AnswerImageAttachment);
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