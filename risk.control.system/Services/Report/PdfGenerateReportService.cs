using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Services.Report
{
    public interface IPdfGenerateReportService
    {
        Task<string> BuildInvestigationPdfReport(InvestigationTask investigation, PolicyDetail policy, CustomerDetail customer, BeneficiaryDetail beneficiary, ReportTemplate investigationReport, Vendor vendor);
    }
    internal class PdfGenerateReportService : IPdfGenerateReportService
    {
        private const string reportFilename = "report.pdf";
        private readonly IWebHostEnvironment _env;
        private readonly IPdfGenerateCaseDetailService _caseDetailService;
        private readonly IPdfGenerateDetailReportService _detailReportService;

        public PdfGenerateReportService(
            IWebHostEnvironment env,
            IPdfGenerateCaseDetailService caseDetailService,
            IPdfGenerateDetailReportService detailReportService)
        {
            _env = env;
            _caseDetailService = caseDetailService;
            _detailReportService = detailReportService;
        }
        public async Task<string> BuildInvestigationPdfReport(InvestigationTask investigation, PolicyDetail policy, CustomerDetail customer, BeneficiaryDetail beneficiary, ReportTemplate investigationReport, Vendor vendor)
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
            section.AddParagraph().SetMarginBottom(10f);
            section = await _detailReportService.Build(section, investigation, investigationReport, vendor, isClaim);
            section.AddParagraph().SetMarginBottom(10f);
            section = AddRemarks(section, "Assessor remarks", investigation.InvestigationReport!.AssessorRemarks!);
            section.AddParagraph().SetMarginBottom(10f);
            section.AddParagraph().AddText($"Report Status: {investigation.SubStatus}").SetBold().SetFontSize(10);
            section.AddParagraph().SetMarginBottom(10f);
            section.AddParagraph().AddText($"Generated on: {DateTime.UtcNow.ToLocalTime():dd-MMM-yy hh:mm tt}").SetItalic().SetFontSize(10);
            builder.Build(ReportFilePath);
            investigation.InvestigationReport.PdfReportFilePath = ReportFilePath;

            return reportFilename;
        }

        private static SectionBuilder AddRemarks(SectionBuilder section, string title, string content)
        {
            var table = section.AddTable()
                               .SetBorder(Stroke.Solid);

            table.AddColumnPercentToTable("", 10);
            table.AddColumnPercentToTable("", 90);

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
