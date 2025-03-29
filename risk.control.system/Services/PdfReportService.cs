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
        Task Run(string userEmail,string claimsInvestigationId);
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

        public async Task Run(string userEmail,string claimsInvestigationId)
        {

            var (builder,filePath) = await CreateReport(userEmail,claimsInvestigationId);

            builder.Build(filePath);

        }
        private async Task<(DocumentBuilder builder, string filePath)> CreateReport(string userEmail, string claimsInvestigationId)
        {
            var imagePath = webHostEnvironment.WebRootPath;
            //var approvedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
            //           i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            //var rejectedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
            //           i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);
            
            var claim = _context.ClaimsInvestigation
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
                .Include(r => r.AgencyReport)
                .ThenInclude(r => r.DigitalIdReport)
                .Include(r => r.AgencyReport)
                .ThenInclude(r => r.PanIdReport)
                .Include(r => r.AgencyReport)
                .ThenInclude(r => r.AgentIdReport)
                .Include(r => r.AgencyReport)
                .ThenInclude(r => r.ReportQuestionaire)
                .FirstOrDefault(c => c.ClaimsInvestigationId == claimsInvestigationId);

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
                AgencyReportId = claim.AgencyReport?.AgencyReportId,
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

            var reportFilename = "report" + claim.ClaimsInvestigationId + ".pdf";

            var ReportFilePath = Path.Combine(webHostEnvironment.WebRootPath, "report", reportFilename);

            string detailReportJsonFile = CheckFile(Path.Combine("Files", "detail-report.json"));
            string detailReportJsonContent = File.ReadAllText(detailReportJsonFile);
            DetailedReport detailReport = JsonConvert.DeserializeObject<DetailedReport>(detailReportJsonContent);
            detailReport.ReportTime = claim.ProcessedByAssessorTime?.ToString("dd-MMM-yyyy HH:mm:ss");
            detailReport.PolicyNum = claim.PolicyDetail.ContractNumber;
            detailReport.ServiceType = claim.PolicyDetail.InvestigationServiceType.Name;
            detailReport.AgencyName = claim.Vendor.Email;
            detailReport.ClaimType = claim.PolicyDetail.LineOfBusiness.Name;
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
            var panValid = claim.AgencyReport.PanIdReport.DocumentIdImageValid.GetValueOrDefault();
            panIdData.FaceMatchStatus = panValid ? "YES" : "NO";
            panIdData.PersonName = claim.AgencyReport.PanIdReport.DocumentIdImageData.Length > 30 ?
                claim.AgencyReport.PanIdReport.DocumentIdImageData.Substring(0, 30) + "..." :
                claim.AgencyReport.PanIdReport.DocumentIdImageData;
            panIdData.Salutation = "PAN/CARD";
            panIdData.PersonContact = contactNumer;
            panIdData.BoardingTill = claim.AgencyReport.PanIdReport.DocumentIdImageLongLatTime.GetValueOrDefault();
            panIdData.PhotoIdTime = claim.AgencyReport.PanIdReport.DocumentIdImageLongLatTime.GetValueOrDefault();
            panIdData.WeatherData = claim.AgencyReport.PanIdReport.DocumentIdImageData;
            panIdData.ArrivalAirport = "";
            panIdData.ArrivalAbvr = claim.AgencyReport.PanIdReport.DocumentIdImageLocationAddress;
            panIdData.PhotoIdRemarks = panValid ? "CONFIRM" : "NOT SURE";
            panIdData.MatchFont = panValid ? Fonts.Helvetica(16f).SetColor(Color.Green) : Fonts.Helvetica(16f).SetColor(Color.Red);
            panIdData.StatusImagePath = panValid ? Path.Combine(imagePath, "img", "yes.png") : Path.Combine(imagePath, "img", "cancel.png");

            var panCardFileName = $"pan-card-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.jpg";
            await File.WriteAllBytesAsync(Path.Combine(imagePath, "report", panCardFileName), claim.AgencyReport.PanIdReport.DocumentIdImage);
            panIdData.PanPhotoPath = Path.Combine(imagePath, "report", panCardFileName);

            string googlePanImagePath = Path.Combine(imagePath, "report", $"google-pan-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            var panPath = await DownloadMapImageAsync(claim.AgencyReport.PanIdReport.DocumentIdImageLocationUrl, googlePanImagePath);
            panIdData.PanMapUrl = claim.AgencyReport.PanIdReport.DocumentIdImageLocationUrl;
            panIdData.PanMapPath = panPath;


            string agencyDetailFile = CheckFile(Path.Combine("Files", "agency-detail.json"));
            string agencyDetailJsonContent = File.ReadAllText(agencyDetailFile);
            AgencyDetailData agencyDetailData = JsonConvert.DeserializeObject<AgencyDetailData>(agencyDetailJsonContent);

            agencyDetailData.ReportSummary = claim.AgencyReport?.SupervisorRemarks;
            agencyDetailData.AgencyDomain = claim.Vendor.Email;
            agencyDetailData.AgencyContact = claim.Vendor.PhoneNumber;
            agencyDetailData.SupervisorEmail = claim.AgencyReport?.SupervisorEmail;
            agencyDetailData.AddressVisited = detailReport.VerifyAddress;
            agencyDetailData.WeatherDetail = claim.AgencyReport.AssessorRemarks;
            agencyDetailData.AssessorSummary = claim.AgencyReport?.AgentRemarks;

            agencyDetailData.ReportSummaryDescription = new List<AgentQuestionAnswer> {
                new AgentQuestionAnswer
                {
                    Question =  claim.AgencyReport?.ReportQuestionaire.Question1,
                    Answer = claim.AgencyReport?.ReportQuestionaire.Answer1
                },
                new AgentQuestionAnswer
                {
                    Question =  claim.AgencyReport?.ReportQuestionaire.Question2,
                    Answer = claim.AgencyReport?.ReportQuestionaire.Answer2
                },
                new AgentQuestionAnswer
                {
                    Question =  claim.AgencyReport?.ReportQuestionaire.Question3,
                    Answer = claim.AgencyReport?.ReportQuestionaire.Answer3
                },
                new AgentQuestionAnswer
                {
                    Question =  claim.AgencyReport?.ReportQuestionaire.Question4,
                    Answer = claim.AgencyReport?.ReportQuestionaire.Answer4
                }};


            PdfReportBuilder ConcertTicketBuilder = new PdfReportBuilder();

            ConcertTicketBuilder.DetailedReport = detailReport;
            ConcertTicketBuilder.AgencyDetailData = agencyDetailData;
            ConcertTicketBuilder.PhotoIdData = photoIdData;
            ConcertTicketBuilder.AgentIdData = agentIdData;
            ConcertTicketBuilder.PanData = panIdData;

            claim.AgencyReport.PdfReportFilePath = ReportFilePath;

            var saveCount = await _context.SaveChangesAsync();

            return (ConcertTicketBuilder.Build(imagePath), ReportFilePath);

        }

        private static async Task<IdData> SetAgentIdReport(string imagePath, ClaimsInvestigation claim, DetailedReport detailReport)
        {
            string photoIdJsonFile = CheckFile(Path.Combine("Files", "photo-id.json"));
            string photoIdJsonContent = File.ReadAllText(photoIdJsonFile);
            IdData photoIdData = JsonConvert.DeserializeObject<IdData>(photoIdJsonContent);

            photoIdData.Passenger = "Agent Id";
            var photoMatch = claim.AgencyReport.AgentIdReport.Similarity > 70;
            photoIdData.FaceMatchStatus = photoMatch ? "YES" : "NO";
            photoIdData.MatchFont = photoMatch ? Fonts.Helvetica(16f).SetColor(Color.Green) : Fonts.Helvetica(16f).SetColor(Color.Red);
            var photoStatusImage = photoMatch ? "yes.png" : "cancel.png";
            photoIdData.StatusImagePath = Path.Combine(imagePath, "img", photoStatusImage);

            string personAddressUrl = string.Empty;
            string contactNumer = string.Empty;
            detailReport.AgentOfInterestName = claim.AgencyReport.AgentEmail;
            contactNumer = string.Empty;
            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
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
            await File.WriteAllBytesAsync(Path.Combine(imagePath, "report", photoIdFilename), claim.AgencyReport.AgentIdReport.DigitalIdImage);
            photoIdData.PhotoIdPath = Path.Combine(imagePath, "report", photoIdFilename);

            string googlePhotoImagePath = Path.Combine(imagePath, "report", $"google-agent-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            var photoPath = await DownloadMapImageAsync(claim.AgencyReport.AgentIdReport.DigitalIdImageLocationUrl, googlePhotoImagePath);
            photoIdData.PhotoIdMapUrl = claim.AgencyReport.AgentIdReport.DigitalIdImageLocationUrl;
            photoIdData.PhotoIdMapPath = photoPath;
            photoIdData.PersonAddressImage = addressPath;
            photoIdData.PersonName = claim.AgencyReport.AgentEmail;
            photoIdData.Salutation = "MR/MS";
            photoIdData.PersonContact = contactNumer;
            photoIdData.BoardingTill = claim.AgencyReport.AgentIdReport.DigitalIdImageLongLatTime.GetValueOrDefault();
            photoIdData.PhotoIdTime = claim.AgencyReport.AgentIdReport.DigitalIdImageLongLatTime.GetValueOrDefault();
            photoIdData.WeatherData = claim.AgencyReport.AgentIdReport.DigitalIdImageData;
            photoIdData.ArrivalAirport = "";
            photoIdData.ArrivalAbvr = claim.AgencyReport.AgentIdReport.DigitalIdImageLocationAddress;
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
        private static async Task<(IdData, DetailedReport, string)> SetFaceIdReport(string imagePath, ClaimsInvestigation claim, DetailedReport detailReport)
        {
            string photoIdJsonFile = CheckFile(Path.Combine("Files", "photo-id.json"));
            string photoIdJsonContent = File.ReadAllText(photoIdJsonFile);
            IdData photoIdData = JsonConvert.DeserializeObject<IdData>(photoIdJsonContent);

            photoIdData.Passenger = "Photo Id";
            var photoMatch = claim.AgencyReport.DigitalIdReport.Similarity > 70;
            photoIdData.FaceMatchStatus = photoMatch ? "YES" : "NO";
            photoIdData.MatchFont = photoMatch ? Fonts.Helvetica(16f).SetColor(Color.Green) : Fonts.Helvetica(16f).SetColor(Color.Red);
            var photoStatusImage = photoMatch ? "yes.png" : "cancel.png";
            photoIdData.StatusImagePath = Path.Combine(imagePath, "img", photoStatusImage);

            string personAddressUrl = string.Empty;
            string contactNumer = string.Empty;
            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
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
            await File.WriteAllBytesAsync(Path.Combine(imagePath, "report", photoIdFilename), claim.AgencyReport.DigitalIdReport.DigitalIdImage);
            photoIdData.PhotoIdPath = Path.Combine(imagePath, "report", photoIdFilename);

            string googlePhotoImagePath = Path.Combine(imagePath, "report", $"google-photo-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            var photoPath = await DownloadMapImageAsync(claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationUrl, googlePhotoImagePath);
            photoIdData.PhotoIdMapUrl = claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationUrl;
            photoIdData.PhotoIdMapPath = photoPath;
            photoIdData.PersonAddressImage = addressPath;
            photoIdData.PersonName = detailReport.PersonOfInterestName;
            photoIdData.Salutation = "MR/MS";
            photoIdData.PersonContact = contactNumer;
            photoIdData.BoardingTill = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLatTime.GetValueOrDefault();
            photoIdData.PhotoIdTime = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLatTime.GetValueOrDefault();
            photoIdData.WeatherData = claim.AgencyReport.DigitalIdReport.DigitalIdImageData;
            photoIdData.ArrivalAirport = "";
            photoIdData.ArrivalAbvr = claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationAddress;
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
