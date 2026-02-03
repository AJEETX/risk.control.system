using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using Gehtsoft.PDFFlow.Utils;
using risk.control.system.Helpers;
using risk.control.system.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace risk.control.system.Services.Report
{
    public interface IPdfGenerateCaseDetailService
    {
        SectionBuilder BuildUnderwritng(SectionBuilder section, InvestigationTask investigation, PolicyDetail policy, CustomerDetail customer, BeneficiaryDetail beneficiary);
        SectionBuilder BuildClaim(SectionBuilder section, InvestigationTask investigation, PolicyDetail policy, CustomerDetail customer, BeneficiaryDetail beneficiary);
    }
    internal class PdfGenerateCaseDetailService : IPdfGenerateCaseDetailService
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

        public SectionBuilder BuildUnderwritng(SectionBuilder section, InvestigationTask investigation, PolicyDetail policy, CustomerDetail customer, BeneficiaryDetail beneficiary)
        {
            // Title
            section.AddParagraph().SetAlignment(HorizontalAlignment.Center)
                .AddText($"{policy?.InsuranceType.GetEnumDisplayName()} Investigation Report").SetFontSize(20).SetBold();

            // Investigation Section
            section.AddParagraph().AddText($" Investigator : {investigation.Vendor.Email}").SetFontSize(16).SetBold().SetUnderline();
            section.AddParagraph().AddText($"Proposal #: {investigation?.PolicyDetail.ContractNumber}");
            section.AddParagraph().AddText($"Insurer: {investigation?.ClientCompany?.Name}");

            // Policy Section
            section.AddParagraph().AddText($"Policy Type: {policy?.InsuranceType.GetEnumDisplayName()}").SetFontSize(16).SetBold().SetUnderline();
            section.AddParagraph().AddText(" Proposal Info").SetFontSize(16).SetBold().SetUnderline();

            section.AddParagraph().AddText($"Verification Type: {policy?.InvestigationServiceType?.Name}");
            section.AddParagraph().AddText($"Assured Amount: {policy?.SumAssuredValue.ToString()}");

            // Customer Section
            section.AddParagraph().AddText(" Life Assured  Details").SetFontSize(16).SetBold().SetUnderline();
            section.AddParagraph().AddText($"Life Assured Name: {customer?.Name}");
            section.AddParagraph().AddText($"Date Of birth: {customer?.DateOfBirth.Value.ToString("dd-MMM-yyyy")}");
            section.AddParagraph().AddText($"Occupation : {customer?.Occupation.GetEnumDisplayName()}");
            section.AddParagraph().AddText($"Income : {customer?.Income.GetEnumDisplayName()}");
            section.AddParagraph().AddText($"Address: {customer?.Addressline},{customer?.District?.Name}, {customer?.State?.Name}, {customer?.Country?.Name}");

            // Beneficiary Section
            section.AddParagraph().AddText(" Beneficiary Details").SetFontSize(16).SetBold().SetUnderline();
            section.AddParagraph().AddText($"Name: {beneficiary?.Name}");
            section.AddParagraph().AddText($"Relation: {beneficiary?.BeneficiaryRelation?.Name}");
            section.AddParagraph().AddText($"Date Of birth: {beneficiary?.DateOfBirth.Value.ToString("dd-MMM-yyyy")}");
            section.AddParagraph().AddText($"Income : {beneficiary?.Income.GetEnumDisplayName()}");
            section.AddParagraph().AddText($"Address: {beneficiary?.Addressline},{beneficiary?.District?.Name}, {beneficiary?.State?.Name}, {beneficiary?.Country?.Name}");
            return section;
        }

        public SectionBuilder BuildClaim(SectionBuilder section, InvestigationTask investigation, PolicyDetail policy, CustomerDetail customer, BeneficiaryDetail beneficiary)
        {
            // Title
            section.AddParagraph().SetAlignment(HorizontalAlignment.Center).AddText($"{policy?.InsuranceType.GetEnumDisplayName()} Investigation Report").SetFontSize(20).SetBold();

            section.AddParagraph().AddText($"Company: {investigation?.ClientCompany?.Name}");
            section.AddParagraph().AddText($"Policy #: {investigation?.PolicyDetail.ContractNumber}");

            section.AddParagraph().AddText($"Case received: {policy?.DateOfIncident.ToString("dd-MMM-yyyy")}");

            // Investigation Section
            section.AddParagraph().AddText($" Investigator : {investigation.Vendor.Email}").SetFontSize(16).SetBold().SetUnderline();
            section.AddParagraph().AddText($" Report Date : {investigation.InvestigationReport.AssessorRemarksUpdated.GetValueOrDefault()}");

            // Customer Section
            section.AddParagraph().AddText(" Life Assured  Details").SetFontSize(16).SetBold().SetUnderline();
            section.AddParagraph().AddText($"Life Assured Name: {customer?.Name}");
            section.AddParagraph().AddText($"Date Of birth: {customer?.DateOfBirth.Value.ToString("dd-MMM-yyyy")}");
            section.AddParagraph().AddText($"Occupation : {customer?.Occupation.GetEnumDisplayName()}");
            section.AddParagraph().AddText($"Income : {customer?.Income.GetEnumDisplayName()}");
            section.AddParagraph().AddText($"Address: {customer?.Addressline},{customer?.District?.Name}, {customer?.State?.Name}, {customer?.Country?.Name}");

            section.AddParagraph().AddText($"Verification Type: {policy?.InvestigationServiceType?.Name}");
            section.AddParagraph().AddText($"Assured Amount: {policy?.SumAssuredValue.ToString()}");
            section.AddParagraph().AddText($"Policy Issue  date: {policy?.ContractIssueDate.ToString("dd-MMM-yyyy")}");
            section.AddParagraph().AddText($"Cause of Death: {policy?.CauseOfLoss}");

            // Beneficiary Section
            section.AddParagraph().AddText(" Claimant Details").SetFontSize(16).SetBold().SetUnderline();
            section.AddParagraph().AddText($"Name: {beneficiary?.Name}");
            section.AddParagraph().AddText($"Relation: {beneficiary?.BeneficiaryRelation?.Name}");
            section.AddParagraph().AddText($"Date Of birth: {beneficiary?.DateOfBirth.Value.ToString("dd-MMM-yyyy")}");
            section.AddParagraph().AddText($"Income : {beneficiary?.Income.GetEnumDisplayName()}");
            section.AddParagraph().AddText($"Address: {beneficiary?.Addressline},{beneficiary?.District?.Name}, {beneficiary?.State?.Name}, {beneficiary?.Country?.Name}");
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
