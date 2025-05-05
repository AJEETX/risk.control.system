using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using risk.control.system.Models;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp;
using Gehtsoft.PDFFlow.Utils;
using static System.Collections.Specialized.BitVector32;

namespace risk.control.system.Services
{
    public interface IPdfGenerateDetailReportService
    {
        Task<SectionBuilder> Build(SectionBuilder section,InvestigationTask investigation, ReportTemplate investigationReport);
    }
    public class PdfGenerateDetailReportService : IPdfGenerateDetailReportService
    {
        internal static readonly FontBuilder FNT9 = Fonts.Helvetica(9f);
        internal static readonly FontBuilder FNT10 = Fonts.Helvetica(10f);
        internal static readonly FontBuilder FNT12 = Fonts.Helvetica(12f);
        internal static readonly FontBuilder FNT12B = Fonts.Helvetica(12f).SetBold(true);
        internal static readonly FontBuilder FNT20 = Fonts.Helvetica(20f);
        internal static readonly FontBuilder FNT19B = Fonts.Helvetica(19f).SetBold();

        internal static readonly FontBuilder FNT8 = Fonts.Helvetica(8f);

        internal static readonly FontBuilder FNT8_G =
            Fonts.Helvetica(8f).SetColor(Gehtsoft.PDFFlow.Models.Shared.Color.Gray);

        internal static readonly FontBuilder FNT9B =
            Fonts.Helvetica(9f).SetBold();

        internal static readonly FontBuilder FNT11B =
            Fonts.Helvetica(11f).SetBold();

        internal static readonly FontBuilder FNT15 = Fonts.Helvetica(15f);
        internal static readonly FontBuilder FNT16 = Fonts.Helvetica(16f);

        internal static readonly FontBuilder FNT16_R =
            Fonts.Helvetica(16f).SetColor(Gehtsoft.PDFFlow.Models.Shared.Color.Red);
        internal static readonly FontBuilder FNT16_G =
            Fonts.Helvetica(16f).SetColor(Gehtsoft.PDFFlow.Models.Shared.Color.Green);
        internal static readonly FontBuilder FNT17 = Fonts.Helvetica(17f);
        internal static readonly FontBuilder FNT18 = Fonts.Helvetica(18f);

        public IPdfGenerateAgentLocationService agentService;
        private readonly IPdfGenerateFaceLocationService faceService;
        private readonly IPdfGenerateDocumentLocationService documentService;
        private readonly IPdfGenerateQuestionLocationService questionService;

        public PdfGenerateDetailReportService(IPdfGenerateAgentLocationService agentService, 
            IPdfGenerateFaceLocationService faceService, IPdfGenerateDocumentLocationService documentService,
            IPdfGenerateQuestionLocationService questionService)
        {
            this.agentService= agentService;
            this.faceService = faceService;
            this.documentService = documentService;
            this.questionService = questionService;
        }
        public async Task<SectionBuilder> Build(SectionBuilder section, InvestigationTask investigation,  ReportTemplate investigationReport)
        {
            var img = ImageConverter.ConvertToPng(investigation.Vendor.DocumentImage);
            var paragraph = section.AddParagraph();

            // Add the image inline (before the text)
            paragraph.SetLineSpacing(2).AddInlineImage(img)
                     .SetWidth(100)   // adjust as needed
                     .SetHeight(60); // optional small space between image and text

            paragraph.AddText($" {investigation.Vendor.Email} : Investigation Report")
                     .SetFontSize(18)
                     .SetBold()
                     .SetUnderline();
            int locationCount = 1;

            foreach (var loc in investigationReport.LocationTemplate)
            {
                section.AddParagraph()
                .SetLineSpacing(1)
                   .AddText($"{locationCount}.  Location Verified: {loc.LocationName}")
                   .SetBold()
                   .SetFontSize(14);

                section.AddParagraph()
                       .SetLineSpacing(1)
                       .AddText($"Verifying Agent: {loc.AgentEmail}")
                       .SetFontSize(12)
                       .SetItalic();

                // =================== AGENT ID REPORT ====================
                section = await agentService.Build(section, loc);

                // =================== FACE IDs ====================
                section = await faceService.Build(section, loc);

                //// =================== DOCUMENT IDs ====================
                section = await documentService.Build(section, loc);

                // =================== QUESTIONS ====================
                section = questionService.Build(section, loc);

                section.AddParagraph();
                locationCount++;
            }

            section = AddRemarks(section, "Agent Remarks", investigation.InvestigationReport.AgentRemarks);
            section = AddRemarks(section, "Agent Edited Remarks", investigation.InvestigationReport.AgentRemarksEdit);
            section = AddRemarks(section, "Supervisor Remarks", investigation.InvestigationReport.SupervisorRemarks);
            return section;
        }

        SectionBuilder AddRemarks(SectionBuilder section, string title, string content)
        {
            var table = section.AddTable()
                               .SetBorder(Stroke.Solid);

            table.AddColumnPercentToTable("Title", 30);
            table.AddColumnPercentToTable("Content", 70);

            var row = table.AddRow();

            // Title cell
            row.AddCell()
               .AddParagraph(title)
               .SetFontSize(12)
               .SetBold()
               .SetBackColor(Gehtsoft.PDFFlow.Models.Shared.Color.Gray);

            // Content cell
            row.AddCell()
               .AddParagraph(string.IsNullOrWhiteSpace(content) ? "N/A" : content)
               .SetFontSize(11);

            return section;
        }

    }
}
