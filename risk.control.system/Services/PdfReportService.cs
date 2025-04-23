using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Shared;
using Gehtsoft.PDFFlow.Utils;

using Newtonsoft.Json;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Helpers;

using Standard.Licensing;
using risk.control.system.Data;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using Microsoft.AspNetCore.Hosting;
namespace risk.control.system.Services
{
    public interface IPdfReportService
    {
        Task Run(string userEmail,long claimsInvestigationId);
    }
    public class PdfReportService : IPdfReportService
    {
        private const string CLAIMS = "claims";
        private const string UNDERWRITING = "underwriting";
        private static HttpClient client = new HttpClient();
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;

        public PdfReportService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            this._context = context;
            this.webHostEnvironment = webHostEnvironment;
        }

        public async Task Run(string userEmail,long claimsInvestigationId)
        {

            var (builder,filePath) = await CreateReport(userEmail,claimsInvestigationId);

            builder.Build(filePath);

        }
        private async Task<(DocumentBuilder builder, string filePath)> CreateReport(string userEmail, long claimsInvestigationId)
        {
            var imagePath = webHostEnvironment.WebRootPath;
            //var approvedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
            //           i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            //var rejectedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
            //           i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);
            
            var claim = _context.Investigations
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.District)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.State)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.Country)
                    .Include(c => c.CustomerDetail)
                    .ThenInclude(c => c.PinCode)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.District)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.State)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.Country)
                    .Include(c => c.BeneficiaryDetail)
                    .ThenInclude(c => c.PinCode)
                    .Include(c => c.ClientCompany)
                    .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CaseEnabler)
                .Include(r => r.InvestigationReport)
                .ThenInclude(r => r.DigitalIdReport)
                .Include(r => r.InvestigationReport)
                .ThenInclude(r => r.PanIdReport)
                .Include(r => r.InvestigationReport)
                .ThenInclude(r => r.AgentIdReport)
                .Include(r => r.InvestigationReport)
                .ThenInclude(r => r.CaseQuestionnaire)
                .ThenInclude(r => r.Questions)
                .FirstOrDefault(c => c.Id == claimsInvestigationId);

            //create invoice

            var vendor = _context.Vendor.Include(s => s.VendorInvestigationServiceTypes).FirstOrDefault(v => v.VendorId == claim.VendorId);
            var currentUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault(s => s.InvestigationServiceTypeId == claim.PolicyDetail.InvestigationServiceTypeId);

            //THIS SHOULD NOT HAPPEN IN PROD : demo purpose
            if (investigationServiced == null)
            {
                investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault();
            }
            //END
            var investigatService = _context.InvestigationServiceType.FirstOrDefault(i => i.InvestigationServiceTypeId == claim.PolicyDetail.InvestigationServiceTypeId);

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
                InvestigationReportId = claim.InvestigationReport?.Id,
                SubTotal = investigationServiced.Price,
                TaxAmount = investigationServiced.Price * (1 / 10),
                InvestigationServiceType = investigatService,
                ClaimId = claimsInvestigationId
            };

            _context.VendorInvoice.Add(invoice);

            string folder = Path.Combine(webHostEnvironment.WebRootPath, "report");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var reportFilename = "report" + claim.Id + ".pdf";

            var ReportFilePath = Path.Combine(webHostEnvironment.WebRootPath, "report", reportFilename);

            string detailReportJsonFile = CheckFile(Path.Combine("Files", "detail-report.json"));
            string detailReportJsonContent = File.ReadAllText(detailReportJsonFile);
            DetailedReport detailReport = JsonConvert.DeserializeObject<DetailedReport>(detailReportJsonContent);
            detailReport.ReportTime = claim.ProcessedByAssessorTime?.ToString("dd-MMM-yyyy HH:mm:ss");
            detailReport.PolicyNum = claim.PolicyDetail.ContractNumber;
            detailReport.ServiceType = claim.PolicyDetail.InvestigationServiceType.Name;
            detailReport.AgencyName = claim.Vendor.Email;
            detailReport.ClaimType = claim.PolicyDetail.InsuranceType.GetEnumDisplayName();
            var currency = Extensions.GetCultureByCountry(claim.Vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;
            detailReport.InsuredAmount = $"{currency} {claim.PolicyDetail.SumAssuredValue.ToString()}";
            detailReport.Reason2Verify = claim.PolicyDetail.CaseEnabler.Name.ToLower();

            string filePath = claim.ClientCompany.DocumentUrl;
            string fileName = Path.GetFileName(filePath); // "image.jpg"
            string folderPath = Path.GetDirectoryName(filePath); // "/img"
            string folderName = Path.GetFileName(folderPath);
            detailReport.InsurerLogo = Path.Combine(imagePath, folderName, fileName);
            filePath = claim.Vendor.DocumentUrl;
            fileName = Path.GetFileName(filePath); // "image.jpg"
            folderPath = Path.GetDirectoryName(filePath); // "/img"
            folderName = Path.GetFileName(folderPath);    // "img"
            detailReport.AgencyLogo = Path.Combine(imagePath, folderName, fileName);

            string contactNumer = string.Empty;
            IdData photoIdData = null;

            (photoIdData, detailReport, contactNumer) = await SetFaceIdReport(imagePath, claim, detailReport);
            var agentIdData = await SetAgentIdReport(imagePath, claim, detailReport);

            string panIdDataFile = CheckFile(Path.Combine("Files", "pan-id.json"));
            string panIdDataJsonContent = File.ReadAllText(panIdDataFile);
            IdData panIdData = JsonConvert.DeserializeObject<IdData>(panIdDataJsonContent);

            panIdData.Passenger = "Document Id";
            var panValid = claim.InvestigationReport.PanIdReport.DocumentIdImageValid.GetValueOrDefault();
            panIdData.FaceMatchStatus = panValid ? "YES" : "NO";
            panIdData.PersonName = claim.InvestigationReport.PanIdReport.DocumentIdImageData.Length > 30 ?
                claim.InvestigationReport.PanIdReport.DocumentIdImageData.Substring(0, 30) + "..." :
                claim.InvestigationReport.PanIdReport.DocumentIdImageData;
            panIdData.Salutation = "PAN/CARD";
            panIdData.PersonContact = contactNumer;
            panIdData.BoardingTill = claim.InvestigationReport.PanIdReport.DocumentIdImageLongLatTime.GetValueOrDefault();
            panIdData.PhotoIdTime = claim.InvestigationReport.PanIdReport.DocumentIdImageLongLatTime.GetValueOrDefault();
            panIdData.WeatherData = claim.InvestigationReport.PanIdReport.DocumentIdImageData;
            panIdData.ArrivalAirport = "";
            panIdData.ArrivalAbvr = claim.InvestigationReport.PanIdReport.DocumentIdImageLocationAddress;
            panIdData.PhotoIdRemarks = panValid ? "CONFIRM" : "NOT SURE";
            panIdData.MatchFont = panValid ? Fonts.Helvetica(16f).SetColor(Color.Green) : Fonts.Helvetica(16f).SetColor(Color.Red);
            panIdData.StatusImagePath = panValid ? Path.Combine(imagePath, "img", "yes.png") : Path.Combine(imagePath, "img", "cancel.png");

            var panCardFileName = $"pan-card-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.jpg";
            await File.WriteAllBytesAsync(Path.Combine(imagePath, "report", panCardFileName), claim.InvestigationReport.PanIdReport.DocumentIdImage);
            panIdData.PanPhotoPath = Path.Combine(imagePath, "report", panCardFileName);

            string googlePanImagePath = Path.Combine(imagePath, "report", $"google-pan-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            var panPath = await DownloadMapImageAsync(claim.InvestigationReport.PanIdReport.DocumentIdImageLocationUrl, googlePanImagePath);
            panIdData.PanMapUrl = claim.InvestigationReport.PanIdReport.DocumentIdImageLocationUrl;
            panIdData.PanMapPath = panPath;


            string agencyDetailFile = CheckFile(Path.Combine("Files", "agency-detail.json"));
            string agencyDetailJsonContent = File.ReadAllText(agencyDetailFile);
            AgencyDetailData agencyDetailData = JsonConvert.DeserializeObject<AgencyDetailData>(agencyDetailJsonContent);

            agencyDetailData.ReportSummary = claim.InvestigationReport?.SupervisorRemarks;
            agencyDetailData.AgencyDomain = claim.Vendor.Email;
            agencyDetailData.AgencyContact = claim.Vendor.PhoneNumber;
            agencyDetailData.SupervisorEmail = claim.InvestigationReport?.SupervisorEmail;
            agencyDetailData.AddressVisited = detailReport.VerifyAddress;
            agencyDetailData.WeatherDetail = claim.InvestigationReport.AssessorRemarks;
            agencyDetailData.AssessorSummary = claim.InvestigationReport?.AgentRemarks;

            var queationAmswers = claim.InvestigationReport.CaseQuestionnaire.Questions;

            var qAns = new List<AgentQuestionAnswer>();
            foreach (var question in queationAmswers)
            {
                qAns.Add(new AgentQuestionAnswer
                {
                    Question = question.QuestionText,
                    Answer = question.AnswerText
                });
            }

            agencyDetailData.ReportSummaryDescription = qAns;


            PdfReportBuilder ConcertTicketBuilder = new PdfReportBuilder();

            ConcertTicketBuilder.DetailedReport = detailReport;
            ConcertTicketBuilder.AgencyDetailData = agencyDetailData;
            ConcertTicketBuilder.PhotoIdData = photoIdData;
            ConcertTicketBuilder.AgentIdData = agentIdData;
            ConcertTicketBuilder.PanData = panIdData;

            claim.InvestigationReport.PdfReportFilePath = ReportFilePath;

            var saveCount = await _context.SaveChangesAsync();

            return (ConcertTicketBuilder.Build(imagePath), ReportFilePath);

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
            await File.WriteAllBytesAsync(Path.Combine(imagePath, "report", photoIdFilename), claim.InvestigationReport.AgentIdReport.DigitalIdImage);
            photoIdData.PhotoIdPath = Path.Combine(imagePath, "report", photoIdFilename);

            string googlePhotoImagePath = Path.Combine(imagePath, "report", $"google-agent-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            var photoPath = await DownloadMapImageAsync(claim.InvestigationReport.AgentIdReport.DigitalIdImageLocationUrl, googlePhotoImagePath);
            photoIdData.PhotoIdMapUrl = claim.InvestigationReport.AgentIdReport.DigitalIdImageLocationUrl;
            photoIdData.PhotoIdMapPath = photoPath;
            photoIdData.PersonAddressImage = addressPath;
            photoIdData.PersonName = claim.InvestigationReport.AgentEmail;
            photoIdData.Salutation = "MR/MS";
            photoIdData.PersonContact = contactNumer;
            photoIdData.BoardingTill = claim.InvestigationReport.AgentIdReport.DigitalIdImageLongLatTime.GetValueOrDefault();
            photoIdData.PhotoIdTime = claim.InvestigationReport.AgentIdReport.DigitalIdImageLongLatTime.GetValueOrDefault();
            photoIdData.WeatherData = claim.InvestigationReport.AgentIdReport.DigitalIdImageData;
            photoIdData.ArrivalAirport = "";
            photoIdData.ArrivalAbvr = claim.InvestigationReport.AgentIdReport.DigitalIdImageLocationAddress;
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
            await File.WriteAllBytesAsync(Path.Combine(imagePath, "report", photoIdFilename), claim.InvestigationReport.DigitalIdReport.DigitalIdImage);
            photoIdData.PhotoIdPath = Path.Combine(imagePath, "report", photoIdFilename);

            string googlePhotoImagePath = Path.Combine(imagePath, "report", $"google-photo-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            var photoPath = await DownloadMapImageAsync(claim.InvestigationReport.DigitalIdReport.DigitalIdImageLocationUrl, googlePhotoImagePath);
            photoIdData.PhotoIdMapUrl = claim.InvestigationReport.DigitalIdReport.DigitalIdImageLocationUrl;
            photoIdData.PhotoIdMapPath = photoPath;
            photoIdData.PersonAddressImage = addressPath;
            photoIdData.PersonName = detailReport.PersonOfInterestName;
            photoIdData.Salutation = "MR/MS";
            photoIdData.PersonContact = contactNumer;
            photoIdData.BoardingTill = claim.InvestigationReport.DigitalIdReport.DigitalIdImageLongLatTime.GetValueOrDefault();
            photoIdData.PhotoIdTime = claim.InvestigationReport.DigitalIdReport.DigitalIdImageLongLatTime.GetValueOrDefault();
            photoIdData.WeatherData = claim.InvestigationReport.DigitalIdReport.DigitalIdImageData;
            photoIdData.ArrivalAirport = "";
            photoIdData.ArrivalAbvr = claim.InvestigationReport.DigitalIdReport.DigitalIdImageLocationAddress;
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
    }
}
