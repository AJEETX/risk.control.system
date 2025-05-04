using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Shared;
using Gehtsoft.PDFFlow.Utils;
using Gehtsoft.PDFFlow.Models.Enumerations;

using Newtonsoft.Json;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Helpers;
using risk.control.system.Data;
using Microsoft.EntityFrameworkCore;
using Hangfire;
namespace risk.control.system.Services
{
    public interface IPdfReportService
    {
        Task Run(string userEmail,long claimsInvestigationId);
        Task<string> RunReport(string userEmail, long investigationTaskId);
    }
    public class PdfReportService : IPdfReportService
    {
        private static HttpClient client = new HttpClient();
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;

        public PdfReportService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            this._context = context;
            this.webHostEnvironment = webHostEnvironment;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task Run(string userEmail,long investigationTaskId)
        {

            var (builder,filePath) = await CreateReport(userEmail,investigationTaskId);

            builder.Build(filePath);

        }
        private async Task<(DocumentBuilder builder, string filePath)> CreateReport(string userEmail, long investigationTaskId)
        {
            var imagePath = webHostEnvironment.WebRootPath;
            
            var investigation = _context.Investigations
                    .Include(c => c.CustomerDetail)
                    .Include(c => c.BeneficiaryDetail)
                    .Include(c => c.ClientCompany)
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.InvestigationReport)
                .FirstOrDefault(c => c.Id == investigationTaskId);
            
            var policy = _context.PolicyDetail
                .Include(p => p.CaseEnabler)
                .Include(p => p.CostCentre)
                .Include(p => p.InvestigationServiceType)
                .FirstOrDefault(p => p.PolicyDetailId == investigation.PolicyDetail.PolicyDetailId);

            var customer = _context.CustomerDetail
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .Include(c => c.PinCode)
                .FirstOrDefault(c => c.InvestigationTaskId == investigationTaskId);

            var beneficiary = _context.BeneficiaryDetail
                .Include(b => b.District)
                .Include(b => b.State)
                .Include(b => b.Country)
                .Include(b => b.PinCode)
                .Include(b => b.BeneficiaryRelation)
                .FirstOrDefault(b => b.InvestigationTaskId == investigationTaskId);

            var investigationReport = await _context.ReportTemplates
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

            var vendor = _context.Vendor.Include(s => s.VendorInvestigationServiceTypes).FirstOrDefault(v => v.VendorId == investigation.VendorId);
            var currentUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault(s => s.InvestigationServiceTypeId == policy.InvestigationServiceTypeId);

            //THIS SHOULD NOT HAPPEN IN PROD : demo purpose
            if (investigationServiced == null)
            {
                investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault();
            }
            //END
            var investigatService = _context.InvestigationServiceType.FirstOrDefault(i => i.InvestigationServiceTypeId == policy.InvestigationServiceTypeId);

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

            _context.VendorInvoice.Add(invoice);

            _context.SaveChanges();

            string folder = Path.Combine(webHostEnvironment.WebRootPath, "report");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var reportFilename = "report" + investigation.Id + ".pdf";

            var ReportFilePath = Path.Combine(webHostEnvironment.WebRootPath, "report", reportFilename);
            BuildInvestigationPdfReport(investigation, policy, customer, beneficiary, investigationReport, ReportFilePath);
            //return (DocumentBuilder.New(), ReportFilePath);

            string detailReportJsonFile = CheckFile(Path.Combine("Files", "detail-report.json"));
            string detailReportJsonContent = File.ReadAllText(detailReportJsonFile);
            DetailedReport detailReport = JsonConvert.DeserializeObject<DetailedReport>(detailReportJsonContent);
            detailReport.ReportTime = investigation.ProcessedByAssessorTime?.ToString("dd-MMM-yyyy HH:mm:ss");
            detailReport.PolicyNum = investigation.PolicyDetail.ContractNumber;
            detailReport.ServiceType = investigation.PolicyDetail.InvestigationServiceType.Name;
            detailReport.AgencyName = investigation.Vendor.Email;
            detailReport.ClaimType = investigation.PolicyDetail.InsuranceType.GetEnumDisplayName();
            var currency = Extensions.GetCultureByCountry(investigation.Vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
            detailReport.InsuredAmount = $"{currency} {investigation.PolicyDetail.SumAssuredValue.ToString()}";
            detailReport.Reason2Verify = investigation.PolicyDetail.CaseEnabler.Name.ToLower();

            string filePath = investigation.ClientCompany.DocumentUrl;
            string fileName = Path.GetFileName(filePath); // "image.jpg"
            string folderPath = Path.GetDirectoryName(filePath); // "/img"
            string folderName = Path.GetFileName(folderPath);
            detailReport.InsurerLogo = Path.Combine(imagePath, folderName, fileName);
            filePath = investigation.Vendor.DocumentUrl;
            fileName = Path.GetFileName(filePath); // "image.jpg"
            folderPath = Path.GetDirectoryName(filePath); // "/img"
            folderName = Path.GetFileName(folderPath);    // "img"
            detailReport.AgencyLogo = Path.Combine(imagePath, folderName, fileName);

            string contactNumer = string.Empty;
            IdData photoIdData = null;

            (photoIdData, detailReport, contactNumer) = await SetFaceIdReport(imagePath, investigation, detailReport);
            var agentIdData = await SetAgentIdReport(imagePath, investigation, detailReport);

            

            string agencyDetailFile = CheckFile(Path.Combine("Files", "agency-detail.json"));
            string agencyDetailJsonContent = File.ReadAllText(agencyDetailFile);
            AgencyDetailData agencyDetailData = JsonConvert.DeserializeObject<AgencyDetailData>(agencyDetailJsonContent);

            agencyDetailData.ReportSummary = investigation.InvestigationReport?.SupervisorRemarks;
            agencyDetailData.AgencyDomain = investigation.Vendor.Email;
            agencyDetailData.AgencyContact = investigation.Vendor.PhoneNumber;
            agencyDetailData.SupervisorEmail = investigation.InvestigationReport?.SupervisorEmail;
            agencyDetailData.AddressVisited = detailReport.VerifyAddress;
            agencyDetailData.WeatherDetail = investigation.InvestigationReport.AssessorRemarks;
            agencyDetailData.AssessorSummary = investigation.InvestigationReport?.AgentRemarks;

            var queationAmswers = investigation.InvestigationReport.CaseQuestionnaire.Questions;

            var qAns = new List<AgentQuestionAnswer>();
            foreach (var question in queationAmswers)
            {
                qAns.Add(new AgentQuestionAnswer
                {
                    Question = question.QuestionText,
                    Answer = question.AnswerText
                });
            }
            
            qAns.Add(new AgentQuestionAnswer
            {
                Question = "Remarks",
                Answer = investigation.InvestigationReport.AgentRemarks 
            });

            qAns.Add(new AgentQuestionAnswer
            {
                Question = "Edited Remarks",
                Answer = investigation.InvestigationReport.AgentRemarksEdit
            });

            agencyDetailData.ReportSummaryDescription = qAns;


            PdfReportBuilder ConcertTicketBuilder = new PdfReportBuilder();

            ConcertTicketBuilder.DetailedReport = detailReport;
            ConcertTicketBuilder.AgencyDetailData = agencyDetailData;
            ConcertTicketBuilder.PhotoIdData = photoIdData;
            ConcertTicketBuilder.AgentIdData = agentIdData;
            ConcertTicketBuilder.PanData = await GetPanData(imagePath,investigation) ;

            investigation.InvestigationReport.PdfReportFilePath = ReportFilePath;

            var saveCount = await _context.SaveChangesAsync();

            return (ConcertTicketBuilder.Build(imagePath), ReportFilePath);

        }

        private static async Task<IdData> GetPanData(string imagePath, InvestigationTask investigation)
        {
            string panIdDataFile = CheckFile(Path.Combine("Files", "pan-id.json"));
            string panIdDataJsonContent = File.ReadAllText(panIdDataFile);
            IdData panIdData = JsonConvert.DeserializeObject<IdData>(panIdDataJsonContent);

            panIdData.Passenger = "Document Id";
            var panValid = investigation.InvestigationReport.PanIdReport.IdImageValid.GetValueOrDefault();
            panIdData.FaceMatchStatus = panValid ? "YES" : "NO";
            panIdData.PersonName = investigation.InvestigationReport.PanIdReport.IdImageData.Length > 30 ?
                investigation.InvestigationReport.PanIdReport.IdImageData.Substring(0, 30) + "..." :
                investigation.InvestigationReport.PanIdReport.IdImageData;
            panIdData.Salutation = "PAN/CARD";
            string contactNumer = string.Empty;
            if (investigation.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                contactNumer = investigation.CustomerDetail.ContactNumber;
            }
            else
            {
                contactNumer = investigation.BeneficiaryDetail.ContactNumber;
            }
            panIdData.PersonContact = contactNumer;
            panIdData.BoardingTill = investigation.InvestigationReport.PanIdReport.IdImageLongLatTime.GetValueOrDefault();
            panIdData.PhotoIdTime = investigation.InvestigationReport.PanIdReport.IdImageLongLatTime.GetValueOrDefault();
            panIdData.WeatherData = investigation.InvestigationReport.PanIdReport.IdImageData;
            panIdData.ArrivalAirport = "";
            panIdData.ArrivalAbvr = investigation.InvestigationReport.PanIdReport.IdImageLocationAddress;
            panIdData.PhotoIdRemarks = panValid ? "CONFIRM" : "NOT SURE";
            panIdData.MatchFont = panValid ? Fonts.Helvetica(16f).SetColor(Color.Green) : Fonts.Helvetica(16f).SetColor(Color.Red);
            panIdData.StatusImagePath = panValid ? Path.Combine(imagePath, "img", "yes.png") : Path.Combine(imagePath, "img", "cancel.png");

            var panCardFileName = $"pan-card-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.jpg";
            await File.WriteAllBytesAsync(Path.Combine(imagePath, "report", panCardFileName), investigation.InvestigationReport.PanIdReport.IdImage);
            panIdData.PanPhotoPath = Path.Combine(imagePath, "report", panCardFileName);

            string googlePanImagePath = Path.Combine(imagePath, "report", $"google-pan-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            var panPath = await DownloadMapImageAsync(investigation.InvestigationReport.PanIdReport.IdImageLocationUrl, googlePanImagePath);
            panIdData.PanMapUrl = investigation.InvestigationReport.PanIdReport.IdImageLocationUrl;
            panIdData.PanMapPath = panPath;
            return panIdData;
        }
        private static async Task<IdData> SetAgentIdReport(string imagePath, InvestigationTask claim, DetailedReport detailReport)
        {
            string photoIdJsonFile = CheckFile(Path.Combine("Files", "photo-id.json"));
            string photoIdJsonContent = File.ReadAllText(photoIdJsonFile);
            IdData photoIdData = JsonConvert.DeserializeObject<IdData>(photoIdJsonContent);

            photoIdData.Passenger = "Agent Id";
            var photoMatch = claim.InvestigationReport.AgentIdReport.Similarity > 70;
            photoIdData.FaceMatchStatus = photoMatch ? "YES" : "NO";
            photoIdData.MatchFont = photoMatch ? Fonts.Helvetica(16f).SetColor(Color.Green) : Fonts.Helvetica(16f).SetColor(Color.Red);
            var photoStatusImage = photoMatch ? "yes.png" : "cancel.png";
            photoIdData.StatusImagePath = Path.Combine(imagePath, "img", photoStatusImage);

            string personAddressUrl = string.Empty;
            string contactNumer = string.Empty;
            detailReport.AgentOfInterestName = claim.InvestigationReport.AgentEmail;
            contactNumer = string.Empty;
            if (claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                detailReport.VerifyAddress = claim.CustomerDetail.Addressline + ", " + claim.CustomerDetail.District.Name + ", " + claim.CustomerDetail.State.Name + ", (" + claim.CustomerDetail.Country.Code + "), " + claim.CustomerDetail.PinCode.Code;
                contactNumer = claim.CustomerDetail.ContactNumber;
                personAddressUrl = claim.CustomerDetail.CustomerLocationMap;
            }
            else
            {
                detailReport.VerifyAddress = claim.BeneficiaryDetail.Addressline + ", " + claim.BeneficiaryDetail.District.Name + ", " + claim.BeneficiaryDetail.State.Name + ", (" + claim.BeneficiaryDetail.Country.Code + "), " + claim.BeneficiaryDetail.PinCode.Code;
                contactNumer = claim.BeneficiaryDetail.ContactNumber;
                personAddressUrl = claim.BeneficiaryDetail.BeneficiaryLocationMap;
            }

            string googlePersonAddressImagePath = Path.Combine(imagePath, "report", $"google-agent-address-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            var addressPath = await DownloadMapImageAsync(personAddressUrl, googlePersonAddressImagePath);

            var photoIdFilename = $"agent-id-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.jpg";
            await File.WriteAllBytesAsync(Path.Combine(imagePath, "report", photoIdFilename), claim.InvestigationReport.AgentIdReport.IdImage);
            photoIdData.PhotoIdPath = Path.Combine(imagePath, "report", photoIdFilename);

            string googlePhotoImagePath = Path.Combine(imagePath, "report", $"google-agent-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            var photoPath = await DownloadMapImageAsync(claim.InvestigationReport.AgentIdReport.IdImageLocationUrl, googlePhotoImagePath);
            photoIdData.PhotoIdMapUrl = claim.InvestigationReport.AgentIdReport.IdImageLocationUrl;
            photoIdData.PhotoIdMapPath = photoPath;
            photoIdData.PersonAddressImage = addressPath;
            photoIdData.PersonName = claim.InvestigationReport.AgentEmail;
            photoIdData.Salutation = "MR/MS";
            photoIdData.PersonContact = contactNumer;
            photoIdData.BoardingTill = claim.InvestigationReport.AgentIdReport.IdImageLongLatTime.GetValueOrDefault();
            photoIdData.PhotoIdTime = claim.InvestigationReport.AgentIdReport.IdImageLongLatTime.GetValueOrDefault();
            photoIdData.WeatherData = claim.InvestigationReport.AgentIdReport.IdImageData;
            photoIdData.ArrivalAirport = "";
            photoIdData.ArrivalAbvr = claim.InvestigationReport.AgentIdReport.IdImageLocationAddress;
            if (photoMatch)
            {
                photoIdData.PhotoIdRemarks = $"CONFIRM";
            }
            else
            {
                photoIdData.PhotoIdRemarks = $"NOT SURE";
            }
            return photoIdData;
        }
        private static async Task<(IdData, DetailedReport, string)> SetFaceIdReport(string imagePath, InvestigationTask claim, DetailedReport detailReport)
        {
            string photoIdJsonFile = CheckFile(Path.Combine("Files", "photo-id.json"));
            string photoIdJsonContent = File.ReadAllText(photoIdJsonFile);
            IdData photoIdData = JsonConvert.DeserializeObject<IdData>(photoIdJsonContent);

            photoIdData.Passenger = "Photo Id";
            var photoMatch = claim.InvestigationReport.DigitalIdReport.Similarity > 70;
            photoIdData.FaceMatchStatus = photoMatch ? "YES" : "NO";
            photoIdData.MatchFont = photoMatch ? Fonts.Helvetica(16f).SetColor(Color.Green) : Fonts.Helvetica(16f).SetColor(Color.Red);
            var photoStatusImage = photoMatch ? "yes.png" : "cancel.png";
            photoIdData.StatusImagePath = Path.Combine(imagePath, "img", photoStatusImage);

            string personAddressUrl = string.Empty;
            string contactNumer = string.Empty;
            if (claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                contactNumer = new string('*', claim.CustomerDetail.ContactNumber.Length - 4) + claim.CustomerDetail.ContactNumber.Substring(claim.CustomerDetail.ContactNumber.Length - 4);
                personAddressUrl = claim.CustomerDetail.CustomerLocationMap;
            }
            else
            {
                contactNumer = claim.BeneficiaryDetail.ContactNumber;
                personAddressUrl = claim.BeneficiaryDetail.BeneficiaryLocationMap;
            }
            string googlePersonAddressImagePath = Path.Combine(imagePath, "report", $"google-person-address-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            var addressPath = await DownloadMapImageAsync(personAddressUrl, googlePersonAddressImagePath);

            var photoIdFilename = $"photo-id-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.jpg";
            await File.WriteAllBytesAsync(Path.Combine(imagePath, "report", photoIdFilename), claim.InvestigationReport.DigitalIdReport.IdImage);
            photoIdData.PhotoIdPath = Path.Combine(imagePath, "report", photoIdFilename);

            string googlePhotoImagePath = Path.Combine(imagePath, "report", $"google-photo-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            var photoPath = await DownloadMapImageAsync(claim.InvestigationReport.DigitalIdReport.IdImageLocationUrl, googlePhotoImagePath);
            photoIdData.PhotoIdMapUrl = claim.InvestigationReport.DigitalIdReport.IdImageLocationUrl;
            photoIdData.PhotoIdMapPath = photoPath;
            photoIdData.PersonAddressImage = addressPath;
            photoIdData.PersonName = detailReport.PersonOfInterestName;
            photoIdData.Salutation = "MR/MS";
            photoIdData.PersonContact = contactNumer;
            photoIdData.BoardingTill = claim.InvestigationReport.DigitalIdReport.IdImageLongLatTime.GetValueOrDefault();
            photoIdData.PhotoIdTime = claim.InvestigationReport.DigitalIdReport.IdImageLongLatTime.GetValueOrDefault();
            photoIdData.WeatherData = claim.InvestigationReport.DigitalIdReport.IdImageData;
            photoIdData.ArrivalAirport = "";
            photoIdData.ArrivalAbvr = claim.InvestigationReport.DigitalIdReport.IdImageLocationAddress;
            if (photoMatch)
            {
                photoIdData.PhotoIdRemarks = $"CONFIRM";
            }
            else
            {
                photoIdData.PhotoIdRemarks = $"NOT SURE";
            }
            return (photoIdData, detailReport, contactNumer);
        }
        private static string CheckFile(string file)
        {
            if (!File.Exists(file))
            {
                throw new IOException("File not found: " + Path.GetFullPath(file));
            }
            return file;
        }
        static async Task<string> DownloadMapImageAsync(string url, string outputFilePath)
        {
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(outputFilePath, imageBytes);
                return outputFilePath;
            }
            else
            {
                throw new Exception($"Failed to download map image. Status: {response.StatusCode}");
            }
        }
        public void BuildInvestigationPdfReport(InvestigationTask investigation,PolicyDetail policy, CustomerDetail customer,BeneficiaryDetail beneficiary
            ,ReportTemplate investigationReport, string ReportFilePath)
        {
            // Create document
            DocumentBuilder builder = DocumentBuilder.New();
            SectionBuilder section = builder.AddSection();

            // Title
            section.AddParagraph().SetAlignment(HorizontalAlignment.Center)
                .AddText("Investigation Report").SetFontSize(20).SetBold();

            // Investigation Section
            section.AddParagraph().AddText(" Investigation Details").SetFontSize(16).SetBold().SetUnderline();
            section.AddParagraph().AddText($" Investigator : {investigation.Vendor.Email}").SetFontSize(16).SetBold().SetUnderline();
            section.AddParagraph().AddText($"Investigation ID: {investigation?.PolicyDetail.ContractNumber}");
            section.AddParagraph().AddText($"Client Company: {investigation?.ClientCompany?.Name}");

            // Policy Section
            section.AddParagraph().AddText(" Policy Details").SetFontSize(16).SetBold().SetUnderline();
            section.AddParagraph().AddText($"Policy Number: {policy?.ContractNumber}");
            section.AddParagraph().AddText($"Service Type: {policy?.InvestigationServiceType?.Name}");

            // Customer Section
            section.AddParagraph().AddText(" Customer Details").SetFontSize(16).SetBold().SetUnderline();
            section.AddParagraph().AddText($"Name: {customer?.Name}");
            section.AddParagraph().AddText($"Address: {customer?.Addressline},{customer?.District?.Name}, {customer?.State?.Name}, {customer?.Country?.Name}");

            // Beneficiary Section
            section.AddParagraph().AddText(" Beneficiary Details").SetFontSize(16).SetBold().SetUnderline();
            section.AddParagraph().AddText($"Name: {beneficiary?.Name}");
            section.AddParagraph().AddText($"Relation: {beneficiary?.BeneficiaryRelation?.Name}");
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
                    section.AddParagraph().AddText("Agent ID Report").SetBold();

                    // Create the table
                    var table = section.AddTable().SetBorder(Stroke.Solid);

                    //// Define header row with 5 columns
                    //var headerRow = table.AddRow();
                    //headerRow.AddCell().AddParagraph().AddText("Report Name");
                    //headerRow.AddCell().AddParagraph().AddText("ID Name");
                    //headerRow.AddCell().AddParagraph().AddText("Valid?");
                    //headerRow.AddCell().AddParagraph().AddText("Location");
                    //headerRow.AddCell().AddParagraph().AddText("Image");

                    //// Create the data row
                    //var dataRow = table.AddRow();
                    //dataRow.AddCell().AddParagraph().AddText(loc.AgentIdReport.ReportName ?? "N/A");
                    //dataRow.AddCell().AddParagraph().AddText(loc.AgentIdReport.IdName ?? "N/A");
                    //dataRow.AddCell().AddParagraph().AddText(loc.AgentIdReport.IdImageValid == true ? "Yes" : "No");
                    //dataRow.AddCell().AddParagraph().AddText(loc.AgentIdReport.IdImageLocationAddress ?? "N/A");

                    //// Image column - handle null safely
                    //var imageCell = dataRow.AddCell();
                    //var para = imageCell.AddParagraph();

                    //if (loc.AgentIdReport.IdImage != null)
                    //{
                    //    para.AddInlineImage(loc.AgentIdReport.IdImage);
                    //}
                    //else
                    //{
                    //    para.AddText("No Image");
                    //}
                }



                // =================== FACE IDs ====================
                if (loc.FaceIds?.Any() == true)
                {
                    section.AddParagraph().AddText("Face ID Reports").SetBold();
                    //var table = section.AddTable().SetBorder(Stroke.Solid);

                    //var header = table.AddRow();
                    //header.AddCell().AddParagraph().AddText("Report Name");
                    //header.AddCell().AddParagraph().AddText("Similarity");
                    //header.AddCell().AddParagraph().AddText("Valid");
                    //header.AddCell().AddParagraph().AddText("Image");

                    //foreach (var face in loc.FaceIds.Where(f => f.Selected))
                    //{
                    //    var row = table.AddRow();
                    //    row.AddCell().AddParagraph().AddText(face.ReportName);
                    //    row.AddCell().AddParagraph().AddText(face.Similarity.ToString("F2"));
                    //    row.AddCell().AddParagraph().AddText(face.IdImageValid == true ? "Yes" : "No");

                    //    if (face.IdImage != null)
                    //    {
                    //        row.AddCell().AddParagraph().AddInlineImage(face.IdImage);
                    //        row.AddCell().AddParagraph().AddText(face.IdImageLocationAddress);
                    //    }
                    //    else
                    //        row.AddCell().AddParagraph().AddText("No Image");
                    //}
                }

                // =================== DOCUMENT IDs ====================
                if (loc.DocumentIds?.Any() == true)
                {
                    section.AddParagraph().AddText("Document ID Reports").SetBold();
                    //var table = section.AddTable().SetBorder(Stroke.Solid);

                    //var header = table.AddRow();
                    //header.AddCell().AddParagraph().AddText("Document");
                    //header.AddCell().AddParagraph().AddText("Valid");
                    //header.AddCell().AddParagraph().AddText("Location");
                    //header.AddCell().AddParagraph().AddText("Image");

                    //foreach (var doc in loc.DocumentIds.Where(d => d.Selected))
                    //{
                    //    var row = table.AddRow();
                    //    row.AddCell().AddParagraph().AddText(doc.ReportName);
                    //    row.AddCell().AddParagraph().AddText(doc.IdImageValid == true ? "Yes" : "No");
                    //    row.AddCell().AddParagraph().AddText(doc.IdImageLocationAddress);

                    //    if (doc.IdImage != null)
                    //    {
                    //        row.AddCell().AddParagraph().AddInlineImage(doc.IdImage);
                    //        row.AddCell().AddParagraph().AddText(doc.IdImageLocationAddress);
                    //    }
                    //    else
                    //        row.AddCell().AddParagraph().AddText("No Image");
                    //}
                }

                // =================== QUESTIONS ====================
                if (loc.Questions?.Any() == true)
                {
                    section.AddParagraph().AddText("Questions & Answers").SetBold();
                    var table = section.AddTable().SetBorder(Stroke.Solid);

                    var header = table.AddRow();
                    header.AddCell().AddParagraph().AddText("Question");
                    header.AddCell().AddParagraph().AddText("Answer");

                    foreach (var question in loc.Questions)
                    {
                        var row = table.AddRow();
                        row.AddCell().AddParagraph().AddText(question.QuestionText);
                        row.AddCell().AddParagraph().AddText(question.AnswerText);
                    }
                }

                section.AddParagraph().AddText("..");
            }


            // Footer
            section.AddParagraph().AddText($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}").SetItalic().SetFontSize(10);

            builder.Build(ReportFilePath);
            investigation.InvestigationReport.PdfReportFilePath = ReportFilePath;

            _context.Investigations.Update(investigation);
            _context.SaveChanges();
            //using var stream = new MemoryStream();
            //builder.Build(stream);
            //return stream.ToArray();
        }

        public async Task<string> RunReport(string userEmail, long investigationTaskId)
        {
            var investigation = _context.Investigations
                    .Include(c => c.CustomerDetail)
                    .Include(c => c.BeneficiaryDetail)
                    .Include(c => c.ClientCompany)
                    .Include(c => c.PolicyDetail)
                    .Include(c => c.InvestigationReport)
                .FirstOrDefault(c => c.Id == investigationTaskId);

            var policy = _context.PolicyDetail
                .Include(p => p.CaseEnabler)
                .Include(p => p.CostCentre)
                .Include(p => p.InvestigationServiceType)
                .FirstOrDefault(p => p.PolicyDetailId == investigation.PolicyDetail.PolicyDetailId);

            var customer = _context.CustomerDetail
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .Include(c => c.PinCode)
                .FirstOrDefault(c => c.InvestigationTaskId == investigationTaskId);

            var beneficiary = _context.BeneficiaryDetail
                .Include(b => b.District)
                .Include(b => b.State)
                .Include(b => b.Country)
                .Include(b => b.PinCode)
                .Include(b => b.BeneficiaryRelation)
                .FirstOrDefault(b => b.InvestigationTaskId == investigationTaskId);

            var investigationReport = await _context.ReportTemplates
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

            var vendor = _context.Vendor.Include(s => s.VendorInvestigationServiceTypes).FirstOrDefault(v => v.VendorId == investigation.VendorId);
            var currentUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault(s => s.InvestigationServiceTypeId == policy.InvestigationServiceTypeId);

            //THIS SHOULD NOT HAPPEN IN PROD : demo purpose
            if (investigationServiced == null)
            {
                investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault();
            }
            //END
            var investigatService = _context.InvestigationServiceType.FirstOrDefault(i => i.InvestigationServiceTypeId == policy.InvestigationServiceTypeId);

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

            _context.VendorInvoice.Add(invoice);

            _context.SaveChanges();

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
    }
}
