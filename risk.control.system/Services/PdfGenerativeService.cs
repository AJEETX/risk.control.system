using risk.control.system.Helpers;
using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using Gehtsoft.PDFFlow.Models.Shared;
using Gehtsoft.PDFFlow.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Data;
using risk.control.system.Models;
using static risk.control.system.Helpers.PdfReportBuilder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using Hangfire;


namespace risk.control.system.Services
{
    public interface IPdfGenerativeService
    {
        Task<string> Generate(long investigationTaskId, string userEmail = "assessor@insurer.com");
        // Define methods for PDF generation here
    }
    public class PdfGenerativeService : IPdfGenerativeService
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

        public PdfGenerativeService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            this.context = context;
            this.webHostEnvironment = webHostEnvironment;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task<string> Generate(long investigationTaskId, string userEmail = "assessor@insurer.com")
        {
            var investigation = context.Investigations
                    .Include(c => c.CustomerDetail)
                    .Include(c => c.BeneficiaryDetail)
                    .Include(c => c.ClientCompany)
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.InvestigationReport)
                .FirstOrDefault(c => c.Id == investigationTaskId);

            var policy = context.PolicyDetail
                .Include(p => p.CaseEnabler)
                .Include(p => p.CostCentre)
                .Include(p => p.InvestigationServiceType)
                .FirstOrDefault(p => p.PolicyDetailId == investigation.PolicyDetail.PolicyDetailId);

            var customer = context.CustomerDetail
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .Include(c => c.PinCode)
                .FirstOrDefault(c => c.InvestigationTaskId == investigationTaskId);

            var beneficiary = context.BeneficiaryDetail
                .Include(b => b.District)
                .Include(b => b.State)
                .Include(b => b.Country)
                .Include(b => b.PinCode)
                .Include(b => b.BeneficiaryRelation)
                .FirstOrDefault(b => b.InvestigationTaskId == investigationTaskId);

            var investigationReport = await context.ReportTemplates
                .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.AgentIdReport)
               .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.FaceIds)
               .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.DocumentIds)
               .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.Questions)
                   .FirstOrDefaultAsync(q => q.Id == investigation.ReportTemplateId);


            //create invoice

            var vendor = context.Vendor.Include(s => s.VendorInvestigationServiceTypes).FirstOrDefault(v => v.VendorId == investigation.VendorId);
            var currentUser = context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault(s => s.InvestigationServiceTypeId == policy.InvestigationServiceTypeId);

            //THIS SHOULD NOT HAPPEN IN PROD : demo purpose
            if (investigationServiced == null)
            {
                investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault();
            }
            //END
            var investigatService = context.InvestigationServiceType.FirstOrDefault(i => i.InvestigationServiceTypeId == policy.InvestigationServiceTypeId);

            var invoice = new VendorInvoice
            {
                ClientCompanyId = currentUser.ClientCompany.ClientCompanyId,
                GrandTotal = investigationServiced.Price + investigationServiced.Price * (1 / 10),
                NoteToRecipient = "Auto generated Invoice",
                Updated = DateTime.Now,
                Vendor = vendor,
                ClientCompany = currentUser.ClientCompany,
                UpdatedBy = userEmail,
                VendorId = vendor.VendorId,
                InvestigationReportId = investigation.InvestigationReport?.Id,
                SubTotal = investigationServiced.Price,
                TaxAmount = investigationServiced.Price * (1 / 10),
                InvestigationServiceType = investigatService,
                ClaimId = investigationTaskId
            };

            context.VendorInvoice.Add(invoice);
            context.SaveChanges();

            string folder = Path.Combine(webHostEnvironment.WebRootPath, "report");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var reportFilename = "report" + investigation.Id + ".pdf";

            var ReportFilePath = Path.Combine(webHostEnvironment.WebRootPath, "report", reportFilename);
            BuildInvestigationPdfReport(investigation, policy, customer, beneficiary, investigationReport, ReportFilePath);


            //notifyService.Success($"Policy {claim.PolicyDetail.ContractNumber} Report download success !!!");
            return reportFilename;
        }
        public void BuildInvestigationPdfReport(InvestigationTask investigation, PolicyDetail policy, CustomerDetail customer, BeneficiaryDetail beneficiary
            , ReportTemplate investigationReport, string ReportFilePath)
        {
            // Create document
            DocumentBuilder builder = DocumentBuilder.New();
            SectionBuilder section = builder.AddSection();
            section.SetOrientation(PageOrientation.Landscape);

            // Title
            section.AddParagraph().SetAlignment(HorizontalAlignment.Center)
                .AddText("Investigation Report").SetFontSize(20).SetBold();

            // Investigation Section
            section.AddParagraph().AddText($" Investigator : {investigation.Vendor.Email}").SetFontSize(16).SetBold().SetUnderline();
            section.AddParagraph().AddText($"Case ID: {investigation?.PolicyDetail.ContractNumber}");
            section.AddParagraph().AddText($"Insurer: {investigation?.ClientCompany?.Name}");

            // Policy Section
            section.AddParagraph().AddText($"Policy Type: {policy?.InsuranceType.GetEnumDisplayName()}").SetFontSize(16).SetBold().SetUnderline();

            section.AddParagraph().AddText(" Policy Details").SetFontSize(16).SetBold().SetUnderline();
            section.AddParagraph().AddText($"Policy Number: {policy?.ContractNumber}");
            section.AddParagraph().AddText($"Service Type: {policy?.InvestigationServiceType?.Name}");
            section.AddParagraph().AddText($"Assured Amount: {policy?.SumAssuredValue.ToString()}");
            section.AddParagraph().AddText($"Policy Issue  date: {policy?.ContractIssueDate.ToString("dd-MMM-yyyy")}");
            section.AddParagraph().AddText($"Incident date: {policy?.DateOfIncident.ToString("dd-MMM-yyyy")}");

            // Customer Section
            section.AddParagraph().AddText(" Customer Details").SetFontSize(16).SetBold().SetUnderline();
            section.AddParagraph().AddText($"Name: {customer?.Name}");
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

            // Investigation Report Section
            section.AddParagraph().AddText("Investigation Report").SetFontSize(16).SetBold().SetUnderline();

            foreach (var loc in investigationReport.LocationTemplate)
            {
                section.AddParagraph().AddText($"Location Verified: {loc.LocationName}").SetBold();
                section.AddParagraph().AddText($"Agent: {loc.AgentEmail}");

                // =================== AGENT ID REPORT ====================
                if (loc.AgentIdReport != null)
                {
                    section.AddParagraph().AddText("Agent IDify").SetBold();

                    // Create a simple table without any styling to check the issue
                    var tableBuilder = section.AddTable();  // No border for now, just to test
                    tableBuilder
                        .SetBorder(Stroke.None)
                        .AddColumnPercentToTable("Email", 20)
                        .AddColumnPercentToTable("ID Photo", 20)
                        .AddColumnPercentToTable("Address", 20)
                        .AddColumnPercentToTable("Location Info", 20)
                        .AddColumnPercentToTable("Match", 20);

                    var rowBuilder = tableBuilder.AddRow();
                    rowBuilder.AddCell().AddParagraph(loc.AgentEmail).SetFont(FNT9);
                    if (loc.AgentIdReport.IdImage != null)
                    {
                        try
                        {
                            var pngBytes = ConvertToPng(loc.AgentIdReport.IdImage);
                            rowBuilder.AddCell().AddParagraph().AddInlineImage(pngBytes);
                        }
                        catch (Exception ex)
                        {
                            rowBuilder.AddCell().AddParagraph().AddText("Invalid image");
                            Console.WriteLine("Image conversion error: " + ex.Message);
                        }
                    }
                    else
                    {
                        rowBuilder.AddCell().AddParagraph().AddText("No Image");
                    }
                    rowBuilder.AddCell().AddParagraph(loc.AgentIdReport.IdImageLocationAddress).SetFont(FNT9);
                    rowBuilder.AddCell().AddParagraph(loc.AgentIdReport.IdImageData).SetFont(FNT9);
                    rowBuilder.AddCell().AddParagraph().AddText(loc.AgentIdReport.IdImageValid == true ? "Yes" : "No");

                }

                // =================== FACE IDs ====================
                if (loc.FaceIds?.Any() == true)
                {
                    section.AddParagraph().AddText("Face ID Reports").SetBold();

                    // Create a simple table without any styling to check the issue
                    var tableBuilder = section.AddTable();  // No border for now, just to test
                    tableBuilder
                        .SetBorder(Stroke.None)
                        .AddColumnPercentToTable("Report Name", 20)
                        .AddColumnPercentToTable("ID Photo", 20)
                        .AddColumnPercentToTable("Address", 20)
                        .AddColumnPercentToTable("Location Info", 20)
                        .AddColumnPercentToTable("Match", 20);

                    foreach (var face in loc.FaceIds.Where(f => f.Selected))
                    {

                        var rowBuilder = tableBuilder.AddRow();
                        rowBuilder.AddCell().AddParagraph().AddText(face.ReportName);

                        if (face.IdImage != null)
                        {
                            try
                            {
                                var pngBytes = ConvertToPng(face.IdImage);
                                rowBuilder.AddCell().AddParagraph().AddInlineImage(pngBytes);
                            }
                            catch (Exception ex)
                            {
                                rowBuilder.AddCell().AddParagraph().AddText("Invalid image");
                                Console.WriteLine("Image conversion error: " + ex.Message);
                            }
                        }
                        else
                        {
                            rowBuilder.AddCell().AddParagraph().AddText("No Image");
                        }
                        rowBuilder.AddCell().AddParagraph().AddText(face.IdImageLocationAddress);

                        rowBuilder.AddCell().AddParagraph().AddText(face.IdImageData);
                        rowBuilder.AddCell().AddParagraph().AddText(face.IdImageValid == true ? "Yes" : "No");
                    }
                }

                //// =================== DOCUMENT IDs ====================
                if (loc.DocumentIds?.Any() == true)
                {
                    section.AddParagraph().AddText("Document ID Reports").SetBold();
                    // Create a simple table without any styling to check the issue
                    var tableBuilder = section.AddTable();  // No border for now, just to test
                    tableBuilder
                        .SetBorder(Stroke.None)
                        .AddColumnPercentToTable("Report Name", 20)
                        .AddColumnPercentToTable("ID Photo", 20)
                        .AddColumnPercentToTable("Address", 20)
                        .AddColumnPercentToTable("Location Info", 20)
                        .AddColumnPercentToTable("Match", 20);

                    foreach (var face in loc.DocumentIds.Where(f => f.Selected))
                    {
                        var rowBuilder = tableBuilder.AddRow();
                        rowBuilder.AddCell().AddParagraph().AddText(face.ReportName);

                        if (face.IdImage != null)
                        {
                            try
                            {
                                var pngBytes = ConvertToPng(face.IdImage);
                                rowBuilder.AddCell().AddParagraph().AddInlineImage(pngBytes);
                            }
                            catch (Exception ex)
                            {
                                rowBuilder.AddCell().AddParagraph().AddText("Invalid image");
                                Console.WriteLine("Image conversion error: " + ex.Message);
                            }
                        }
                        else
                        {
                            rowBuilder.AddCell().AddParagraph().AddText("No Image");
                        }
                        rowBuilder.AddCell().AddParagraph().AddText(face.IdImageLocationAddress);

                        rowBuilder.AddCell().AddParagraph().AddText(face.IdImageData);
                        rowBuilder.AddCell().AddParagraph().AddText(face.IdImageValid == true ? "Yes" : "No");
                    }

                    section.AddParagraph().AddText("..");
                }

                // =================== QUESTIONS ====================
                if (loc.Questions?.Any() == true)
                {
                    section.AddParagraph().AddText("Questions & Answers").SetBold();

                    var tableBuilder = section.AddTable();  // No border for now, just to test
                    tableBuilder
                        .SetBorder(Stroke.None)
                        .AddColumnPercentToTable("Question", 50)
                        .AddColumnPercentToTable("Answer", 50);


                    foreach (var question in loc.Questions)
                    {
                        var rowBuilder = tableBuilder.AddRow();
                        rowBuilder.AddCell().AddParagraph().AddText(question.QuestionText);
                        rowBuilder.AddCell().AddParagraph().AddText(question.AnswerText);
                    }
                }
            }

            //add agent remarks
            section.AddParagraph().AddText(" Agent remarks").SetFontSize(16).SetBold().SetUnderline();

            section.AddParagraph().AddText($"{investigation.InvestigationReport.AgentRemarks}");

            //add agent edit remarks
            section.AddParagraph().AddText(" Agent edited remarks").SetFontSize(16).SetBold().SetUnderline();

            section.AddParagraph().AddText($"{investigation.InvestigationReport.AgentRemarksEdit}");

            //add supervisor remarks
            section.AddParagraph().AddText(" Supervisor remarks").SetFontSize(16).SetBold().SetUnderline();

            section.AddParagraph().AddText($"{investigation.InvestigationReport.SupervisorRemarks}");


            //add assessor remarks
            section.AddParagraph().AddText(" Assessor remarks").SetFontSize(16).SetBold().SetUnderline();

            section.AddParagraph().AddText($"{investigation.InvestigationReport.AssessorRemarks}");

            //add status
            section.AddParagraph().AddText(" Status").SetFontSize(16).SetBold().SetUnderline();

            section.AddParagraph().AddText($"{investigation.SubStatus}");

            // Footer
            section.AddParagraph().AddText($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}").SetItalic().SetFontSize(10);

            builder.Build(ReportFilePath);
            investigation.InvestigationReport.PdfReportFilePath = ReportFilePath;

            context.Investigations.Update(investigation);
            context.SaveChanges();
            
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
