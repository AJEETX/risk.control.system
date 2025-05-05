using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp;
using Gehtsoft.PDFFlow.Utils;
using static risk.control.system.Helpers.PdfReportBuilder;
using Microsoft.AspNetCore.Hosting;

namespace risk.control.system.Services
{
    public interface IPdfGenerateDetailService
    {
        Task<string> BuildInvestigationPdfReport(InvestigationTask investigation, PolicyDetail policy, CustomerDetail customer, BeneficiaryDetail beneficiary
             , ReportTemplate investigationReport);
    }
    public class PdfGenerateDetailService : IPdfGenerateDetailService
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

        internal static readonly IdInfo EMPTY_ITEM = new IdInfo("", new FontText[0]);
        private string imgPath = string.Empty;
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IPdfGenerateCaseDetailService detailService;
        private readonly IPdfGenerateDetailReportService detailReportService;

        public PdfGenerateDetailService(ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IPdfGenerateCaseDetailService detailService,
            IPdfGenerateDetailReportService detailReportService)
        {
            this.context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.detailService = detailService;
            this.detailReportService = detailReportService;
        }
        public async Task<string> BuildInvestigationPdfReport(InvestigationTask investigation, PolicyDetail policy, CustomerDetail customer, BeneficiaryDetail beneficiary
           , ReportTemplate investigationReport)
        {
            string folder = Path.Combine(webHostEnvironment.WebRootPath, "report");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var reportFilename = "report" + investigation.Id + ".pdf";

            var ReportFilePath = Path.Combine(webHostEnvironment.WebRootPath, "report", reportFilename);
            // Create document
            DocumentBuilder builder = DocumentBuilder.New();
            SectionBuilder section = builder.AddSection();
            section.SetOrientation(PageOrientation.Landscape);

            //CASE DETAIL
            if(policy.InsuranceType == InsuranceType.UNDERWRITING)
            {
                section = detailService.BuildUnderwritng(section, investigation, policy, customer, beneficiary);
            }
            else
            {
                section = detailService.BuildClaim(section, investigation, policy, customer, beneficiary);
            }

            //CASE DETAIL   Investigation Report Section
            section = await detailReportService.Build(section, investigation, investigationReport);

            //add assessor remarks

            section = AddRemarks(section, "Assessor remarks", investigation.InvestigationReport.AgentRemarks);

            //add status
            section = AddRemarks(section, "Report Status", investigation.SubStatus);

            // Footer
            section.AddParagraph().AddText($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}").SetItalic().SetFontSize(10);

            builder.Build(ReportFilePath);
            investigation.InvestigationReport.PdfReportFilePath = ReportFilePath;

            context.Investigations.Update(investigation);
            context.SaveChanges();
            return reportFilename;
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
        public static byte[] ConvertToPng(byte[] imageBytes)
        {
            using var inputStream = new MemoryStream(imageBytes);
            using var image = Image.Load(inputStream); // Auto-detects format
            using var outputStream = new MemoryStream();
            image.Save(outputStream, new PngEncoder()); // Encode as PNG
            return outputStream.ToArray();
        }
    }
}
