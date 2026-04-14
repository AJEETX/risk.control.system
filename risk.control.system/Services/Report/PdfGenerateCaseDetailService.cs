using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Report
{
    public interface IPdfGenerateCaseDetailService
    {
        SectionBuilder BuildUnderwritng(SectionBuilder section, InvestigationTask investigation, PolicyDetail policy, CustomerDetail customer, BeneficiaryDetail beneficiary);
        SectionBuilder BuildClaim(SectionBuilder section, InvestigationTask investigation, PolicyDetail policy, CustomerDetail customer, BeneficiaryDetail beneficiary);
    }
    internal class PdfGenerateCaseDetailService(IWebHostEnvironment env, IImageConverter imageConverter) : IPdfGenerateCaseDetailService
    {
        private readonly IWebHostEnvironment _env = env;
        private readonly IImageConverter _imageConverter = imageConverter;

        public SectionBuilder BuildUnderwritng(SectionBuilder section, InvestigationTask investigation, PolicyDetail policy, CustomerDetail customer, BeneficiaryDetail beneficiary)
        {
            // Title
            section.AddParagraph()
                .SetAlignment(HorizontalAlignment.Center)
                .AddText($"{policy?.InsuranceType!.GetEnumDisplayName()} Investigation Report")
                .SetFontSize(20).SetBold();
            section.AddParagraph().AddText("");
            section.AddParagraph().SetAlignment(HorizontalAlignment.Center).AddText($"Report Assessed Date: {investigation!.InvestigationReport!.AssessorRemarksUpdated.GetValueOrDefault():dd-MMM-yy hh:mm tt}");

            // Case overview
            var overviewTable = section.AddTable().SetBorder(Stroke.Solid);
            overviewTable.AddColumnPercentToTable("", 35).AddColumnPercentToTable("", 65);
            AddRow(overviewTable, "Investigator", investigation?.Vendor?.Name ?? "N/A");
            AddRow(overviewTable, "Insurer", investigation?.ClientCompany?.Name ?? "N/A");
            AddRow(overviewTable, "Case #", investigation?.PolicyDetail!.ContractNumber ?? "N/A");
            section.AddParagraph().AddText("");

            // Proposal info
            section.AddParagraph().SetLineSpacing(1).AddText("Case Info").SetFontSize(14).SetBold().SetUnderline();
            var proposalTable = section.AddTable().SetBorder(Stroke.Solid);
            proposalTable.AddColumnPercentToTable("", 35).AddColumnPercentToTable("", 65);
            AddRow(proposalTable, "Case Type", policy?.InsuranceType!.GetEnumDisplayName() ?? "N/A");
            AddRow(proposalTable, "Verification Type", policy?.InvestigationServiceType?.Name ?? "N/A");
            var currency = CustomExtensions.GetCultureByCountry(investigation!.ClientCompany!.Country!.Code.ToUpper()).NumberFormat.CurrencySymbol;
            var culture = CustomExtensions.GetCultureByCountry(investigation!.ClientCompany!.Country!.Code.ToUpper());
            var sumAssuredValue = string.Format(culture, "{0:c}", policy?.SumAssuredValue.ToString());
            AddRow(proposalTable, "Assured Amount", currency + " " + policy?.SumAssuredValue.ToString("N2") ?? "N/A");
            section.AddParagraph().AddText("");

            // Life Assured Details
            section.AddParagraph().SetLineSpacing(1).AddText("Life Assured Details").SetFontSize(14).SetBold().SetUnderline();
            BuildPersonSection(section, customer?.ImagePath,
            [
                ("Name", customer?.Name ?? "N/A"),
                ("Date of Birth", customer?.DateOfBirth?.ToString("dd-MMM-yyyy") ?? "N/A"),
                ("Occupation", customer?.Occupation?.GetEnumDisplayName() ?? "N/A"),
                ("Income", customer?.Income?.GetEnumDisplayName() ?? "N/A"),
                ("Address", $"{customer?.Addressline}, {customer?.District?.Name}, {customer?.State?.Name}, {customer?.Country?.Name}"),
                ("Pincode", $"{customer?.PinCode!.Code.ToString() ?? "N/A"}")
            ]);
            section.AddParagraph().AddText("");

            // Beneficiary Details
            section.AddParagraph().SetLineSpacing(1).AddText("Beneficiary Details").SetFontSize(14).SetBold().SetUnderline();
            BuildPersonSection(section, beneficiary?.ImagePath,
            [
                ("Name", beneficiary?.Name ?? "N/A"),
                ("Relation", beneficiary?.BeneficiaryRelation?.Name ?? "N/A"),
                ("Date of Birth", beneficiary?.DateOfBirth?.ToString("dd-MMM-yyyy") ?? "N/A"),
                ("Income", beneficiary?.Income?.GetEnumDisplayName() ?? "N/A"),
                ("Address", $"{beneficiary?.Addressline}, {beneficiary?.District?.Name}, {beneficiary?.State?.Name}, {beneficiary?.Country?.Name}"),
                ("Pincode", $"{beneficiary?.PinCode!.Code.ToString() ?? "N/A"}")
            ]);

            return section;
        }

        public SectionBuilder BuildClaim(SectionBuilder section, InvestigationTask investigation, PolicyDetail policy, CustomerDetail customer, BeneficiaryDetail beneficiary)
        {
            // Title
            section.AddParagraph()
                .SetAlignment(HorizontalAlignment.Center)
                .AddText($"{policy?.InsuranceType!.GetEnumDisplayName()} Investigation Report")
                .SetFontSize(20).SetBold();
            section.AddParagraph().AddText("");
            section.AddParagraph().SetAlignment(HorizontalAlignment.Center).AddText($"Report Assessed Date: {investigation!.InvestigationReport!.AssessorRemarksUpdated.GetValueOrDefault():dd-MMM-yy hh:mm tt}");

            // Case overview
            var overviewTable = section.AddTable().SetBorder(Stroke.Solid);
            overviewTable.AddColumnPercentToTable("", 35).AddColumnPercentToTable("", 65);
            AddRow(overviewTable, "Investigator", investigation!.Vendor!.Name ?? "N/A");
            AddRow(overviewTable, "Insurer", investigation?.ClientCompany?.Name ?? "N/A");
            AddRow(overviewTable, "Case #", investigation?.PolicyDetail!.ContractNumber ?? "N/A");
            AddRow(overviewTable, "Date of Issue", policy?.ContractIssueDate.ToString("dd-MMM-yyyy") ?? "N/A");
            AddRow(overviewTable, "Date of Incident", policy?.DateOfIncident.ToString("dd-MMM-yyyy") ?? "N/A");
            section.AddParagraph().AddText("");

            // Policy details
            section.AddParagraph().SetLineSpacing(1).AddText("Case Details").SetFontSize(14).SetBold().SetUnderline();
            var policyTable = section.AddTable().SetBorder(Stroke.Solid);
            policyTable.AddColumnPercentToTable("", 35).AddColumnPercentToTable("", 65);
            AddRow(policyTable, "Verification Type", policy?.InvestigationServiceType?.Name ?? "N/A");
            var currency = CustomExtensions.GetCultureByCountry(investigation!.ClientCompany!.Country!.Code.ToUpper()).NumberFormat.CurrencySymbol;
            var culture = CustomExtensions.GetCultureByCountry(investigation!.ClientCompany!.Country!.Code.ToUpper());
            var sumAssuredValue = string.Format(culture, "{0:c}", policy?.SumAssuredValue.ToString());
            AddRow(policyTable, "Assured Amount", currency + " " + policy?.SumAssuredValue.ToString("N2") ?? "N/A");
            AddRow(policyTable, "Policy Issue Date", policy?.ContractIssueDate.ToString("dd-MMM-yyyy") ?? "N/A");
            AddRow(policyTable, "Cause of Death", policy?.CauseOfLoss ?? "N/A");
            section.AddParagraph().AddText("");

            // Life Assured Details
            section.AddParagraph().SetLineSpacing(1).AddText("Life Assured Details").SetFontSize(14).SetBold().SetUnderline();
            BuildPersonSection(section, customer?.ImagePath,
            [
                ("Name", customer?.Name ?? "N/A"),
                ("Date of Birth", customer?.DateOfBirth?.ToString("dd-MMM-yyyy") ?? "N/A"),
                ("Occupation", customer?.Occupation?.GetEnumDisplayName() ?? "N/A"),
                ("Income", customer?.Income?.GetEnumDisplayName() ?? "N/A"),
                ("Address", $"{customer?.Addressline}, {customer?.District?.Name}, {customer?.State?.Name}, {customer?.Country?.Name}"),
                ("Pincode", $"{customer?.PinCode!.Code.ToString() ?? "N/A"}")
            ]);
            section.AddParagraph().AddText("");

            // Claimant Details
            section.AddParagraph().SetLineSpacing(1).AddText("Claimant Details").SetFontSize(14).SetBold().SetUnderline();
            BuildPersonSection(section, beneficiary?.ImagePath,
            [
                ("Name", beneficiary?.Name ?? "N/A"),
                ("Relation", beneficiary?.BeneficiaryRelation?.Name ?? "N/A"),
                ("Date of Birth", beneficiary?.DateOfBirth?.ToString("dd-MMM-yyyy") ?? "N/A"),
                ("Income", beneficiary?.Income?.GetEnumDisplayName() ?? "N/A"),
                ("Address", $"{beneficiary?.Addressline}, {beneficiary?.District?.Name}, {beneficiary?.State?.Name}, {beneficiary?.Country?.Name}"),
                ("Pincode", $"{beneficiary?.PinCode!.Code.ToString() ?? "N/A"}")
            ]);

            return section;
        }

        private void BuildPersonSection(SectionBuilder section, string? imagePath, (string Label, string Value)[] fields)
        {
            if (!string.IsNullOrEmpty(imagePath))
            {
                var table = section.AddTable().SetBorder(Stroke.Solid);
                table.AddColumnPercentToTable("", 30).AddColumnPercentToTable("", 70);
                var row = table.AddRow();
                try
                {
                    var photoBytes = _imageConverter.ConvertToPngFromPath(_env, imagePath);
                    row.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center).AddParagraph().AddInlineImage(photoBytes).SetWidth(160F).SetHeight(200F);
                }
                catch
                {
                    row.AddCell().AddParagraph("No Photo").SetFontSize(10);
                }
                var detailsCell = row.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center);
                foreach (var (label, value) in fields)
                    detailsCell.AddParagraph($"{label}: {(string.IsNullOrWhiteSpace(value) ? "N/A" : value)}").SetFontSize(10);
            }
            else
            {
                var table = section.AddTable().SetBorder(Stroke.Solid);
                table.AddColumnPercentToTable("", 35).AddColumnPercentToTable("", 65);
                foreach (var (label, value) in fields)
                    AddRow(table, label, value);
            }
        }

        private static void AddRow(TableBuilder table, string label, string value)
        {
            var row = table.AddRow();
            row.AddCell().AddParagraph(label).SetFontSize(10).SetBold();
            row.AddCell().AddParagraph(string.IsNullOrWhiteSpace(value) ? "N/A" : value).SetFontSize(10);
        }
    }
}
