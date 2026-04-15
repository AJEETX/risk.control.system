using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Services.Report
{
    public interface IPdfGenerateDetailService
    {
        Task<string> BuildInvestigationPdfReport(InvestigationTask investigation, PolicyDetail policy, CustomerDetail customer, BeneficiaryDetail beneficiary
             , ReportTemplate investigationReport);
    }
    internal class PdfGenerateDetailService : IPdfGenerateDetailService
    {
        private const string reportFilename = "report.pdf";
        private readonly IWebHostEnvironment _env;
        private readonly IPdfGenerateCaseDetailService _caseDetailService;
        private readonly IPdfGenerateDetailReportService _detailReportService;

        public PdfGenerateDetailService(
            IWebHostEnvironment env,
            IPdfGenerateCaseDetailService caseDetailService,
            IPdfGenerateDetailReportService detailReportService)
        {
            _env = env;
            _env = env;
            _caseDetailService = caseDetailService;
            _detailReportService = detailReportService;
        }
        public async Task<string> BuildInvestigationPdfReport(InvestigationTask investigation, PolicyDetail policy, CustomerDetail customer, BeneficiaryDetail beneficiary
           , ReportTemplate investigationReport)
        {
            string ReportFilePath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, CONSTANTS.DOCUMENT, CONSTANTS.CASE, policy.ContractNumber, reportFilename));
            DocumentBuilder builder = DocumentBuilder.New();
            SectionBuilder section = builder.AddSection();
            section.SetOrientation(PageOrientation.Landscape);
            bool isClaim = true;
            if (policy.InsuranceType == InsuranceType.UNDERWRITING)
            {
                isClaim = false;
                section = _caseDetailService.BuildUnderwritng(section, investigation, policy, customer, beneficiary);
            }
            else
            {
                section = _caseDetailService.BuildClaim(section, investigation, policy, customer, beneficiary);
            }
            section = await _detailReportService.Build(section, investigation, investigationReport, isClaim);
            section.AddParagraph().AddText("");
            section = AddRemarks(section, "Assessor remarks", investigation.InvestigationReport!.AssessorRemarks!);
            section.AddParagraph().AddText("");
            section = AddRemarks(section, "Report Status", investigation.SubStatus);
            section.AddParagraph().AddText("");
            section.AddParagraph().AddText("");
            section.AddParagraph().AddText("");
            section.AddParagraph().AddText($"Generated on: {DateTime.UtcNow:dd-MMM-yy hh:mm tt}").SetItalic().SetFontSize(10);
            builder.Build(ReportFilePath);
            investigation.InvestigationReport.PdfReportFilePath = ReportFilePath;

            return reportFilename;
        }

        private static SectionBuilder AddRemarks(SectionBuilder section, string title, string content)
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
