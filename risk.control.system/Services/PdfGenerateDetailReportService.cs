using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using Gehtsoft.PDFFlow.Utils;

using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IPdfGenerateDetailReportService
    {
        Task<SectionBuilder> Build(SectionBuilder section, InvestigationTask investigation, ReportTemplate investigationReport, bool isClaim = true);
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
            this.agentService = agentService;
            this.faceService = faceService;
            this.documentService = documentService;
            this.questionService = questionService;
        }
        public async Task<SectionBuilder> Build(SectionBuilder section, InvestigationTask investigation, ReportTemplate investigationReport, bool isClaim = true)
        {

            //var concertTable = section.AddTable();
            var paragraph = section.AddParagraph();

            try
            {
                //var rowBuilder = concertTable.AddRow();
                //var cellBuilder = rowBuilder.AddCell();
                //cellBuilder.SetPadding(2, 2, 2, 0);
                //cellBuilder.AddImage("");
                var pngBytes = ImageConverterToPng.ConvertToPng(investigation.Vendor.DocumentImage, investigation.Vendor.DocumentImageExtension);


                paragraph.AddInlineImage(pngBytes)
                     .SetWidth(150); // optional small space between image and text
            }
            catch (Exception ex)
            {
                paragraph.AddText("Invalid image");
                Console.WriteLine("Image conversion error: " + ex.Message);
            }

            // Add the image inline (before the text)


            paragraph.AddText($" {investigation.Vendor.Email} : Investigation detail")
                     .SetFontSize(18)
                     .SetBold()
                     .SetUnderline();
            int locationCount = 1;

            foreach (var loc in investigationReport.LocationTemplate)
            {
                if (loc.ValidationExecuted)
                {
                    section.AddParagraph()
                    .SetLineSpacing(1)
                       .AddText($"{locationCount}.  Location Verified: {loc.LocationName}")
                       .SetBold()
                       .SetFontSize(14);

                    // =================== AGENT ID REPORT ====================
                    section = await agentService.Build(section, loc, isClaim);

                    // =================== FACE IDs ====================
                    section = await faceService.Build(section, loc, isClaim);

                    //// =================== DOCUMENT IDs ====================
                    section = await documentService.Build(section, loc, isClaim);

                    // =================== QUESTIONS ====================
                    section = questionService.Build(section, loc);

                    // ====== Add Gap Between Locations ======
                    section.AddParagraph().AddText(""); // Empty line
                    section.AddParagraph().AddText(""); // Additional spacing
                    section.AddParagraph().AddText("----------------------------------------------") // Optional separator
                           .SetFontSize(10)
                           .SetItalic();
                    section.AddParagraph().AddText(""); // More space if needed
                    section.AddParagraph().AddText("");


                    locationCount++;
                }

            }

            section.AddParagraph().AddText("");

            // Add Enquiry Report
            if (investigation.InvestigationReport.EnquiryRequests != null && investigation.InvestigationReport.EnquiryRequests.Any())
            {
                section.AddParagraph()
                       .SetLineSpacing(1)
                       .AddText($"Enquiry Report")
                       .SetBold()
                       .SetFontSize(14);

                // Build the table
                var tableBuilder = section.AddTable()
                                          .SetBorder(Stroke.Solid);
                // Add columns
                tableBuilder
                    .AddColumnPercentToTable("Question", 20)
                    .AddColumnPercentToTable("Selected Answer", 15)
                    .AddColumnPercentToTable("Detailed Query", 20)
                    .AddColumnPercentToTable("Query Answer", 20)
                    .AddColumnPercentToTable("Time", 10)
                    .AddColumnPercentToTable("Query Attachment", 7)
                    .AddColumnPercentToTable("Answer Attachment", 8);

                foreach (var request in investigation.InvestigationReport.EnquiryRequests)
                {
                    var rowBuilder = tableBuilder.AddRow();
                    rowBuilder.AddCell().AddParagraph().AddText(request.Subject ?? "N/A").SetFontSize(10);
                    rowBuilder.AddCell().AddParagraph().AddText(request.AnswerSelected ?? "N/A").SetFontSize(10);
                    rowBuilder.AddCell().AddParagraph().AddText(request.Description ?? "N/A").SetFontSize(10);
                    rowBuilder.AddCell().AddParagraph().AddText(request.Answer ?? "N/A").SetFontSize(10);
                    rowBuilder.AddCell().AddParagraph().AddText($"{request.Created:dd-MMM-yy hh:mm tt}").SetFontSize(10);

                    // Question Image Cell
                    if (request.QuestionImageAttachment != null)
                    {
                        try
                        {
                            var pngBytes = ImageConverterToPng.ConvertToPng(request.QuestionImageAttachment, request.QuestionImageFileExtension);
                            rowBuilder.AddCell().AddParagraph().AddInlineImage(pngBytes);
                        }
                        catch (Exception ex)
                        {
                            rowBuilder.AddCell().AddParagraph().AddText("X");
                            Console.WriteLine("Image conversion error: " + ex.Message);
                        }
                    }
                    else
                    {
                        rowBuilder.AddCell().AddParagraph().AddText("");
                    }

                    // Answer Image Cell
                    if (request.AnswerImageAttachment != null)
                    {
                        try
                        {
                            var pngBytes = ImageConverterToPng.ConvertToPng(request.AnswerImageAttachment, request.AnswerImageFileExtension);
                            rowBuilder.AddCell().AddParagraph().AddInlineImage(pngBytes);
                        }
                        catch (Exception ex)
                        {
                            rowBuilder.AddCell().AddParagraph().AddText("X");
                            Console.WriteLine("Image conversion error: " + ex.Message);
                        }
                    }
                    else
                    {
                        rowBuilder.AddCell().AddParagraph().AddText("");
                    }
                }
            }

            section.AddParagraph().AddText("");

            section = AddRemarks(section, "Agent Remarks", investigation.InvestigationReport.AgentRemarks);
            section = AddRemarks(section, "Agent Edited Remarks", investigation.InvestigationReport.AgentRemarksEdit);
            section = AddRemarks(section, "Supervisor Remarks", investigation.InvestigationReport.SupervisorRemarks);
            return section;
        }
        private void AddLogoImage(TableCellBuilder cellBuilder)
        {
            cellBuilder
                .SetPadding(2, 2, 2, 0);
            cellBuilder
                .AddImage("").SetHeight(340);
        }
        SectionBuilder AddRemarks(SectionBuilder section, string title, string content)
        {
            var table = section.AddTable()
                               .SetBorder(Stroke.Solid);

            table.AddColumnPercentToTable("", 30);
            table.AddColumnPercentToTable("", 70);

            var row = table.AddRow();

            // Title cell
            row.AddCell()
               .AddParagraph(title)
               .SetFontSize(12)
               .SetBold();

            // Content cell
            row.AddCell()
               .AddParagraph(string.IsNullOrWhiteSpace(content) ? "N/A" : content)
               .SetFontSize(11);

            return section;
        }

    }
}
