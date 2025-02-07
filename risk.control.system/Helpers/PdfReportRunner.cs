using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Shared;
using Gehtsoft.PDFFlow.Utils;

using Newtonsoft.Json;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using Standard.Licensing;

namespace risk.control.system.Helpers
{
    public class PdfReportRunner
    {
        private static HttpClient client = new HttpClient();
        public static async Task<DocumentBuilder> Run(string imagePath, ClaimsInvestigation claim)
        {
            string photoIdJsonFile = CheckFile(Path.Combine("Files", "photo-id.json"));
            string photoIdJsonContent = File.ReadAllText(photoIdJsonFile);
            IdData photoIdData = JsonConvert.DeserializeObject<IdData>(photoIdJsonContent);
            photoIdData.Passenger = "Photo Id";
            var photoMatch = claim.AgencyReport.DigitalIdReport.Similarity > 70;
            photoIdData.FaceMatchStatus = photoMatch ? "YES" : "NO";
            photoIdData.MatchFont = photoMatch ? Fonts.Helvetica(16f).SetColor(Color.Green) : Fonts.Helvetica(16f).SetColor(Color.Red);
            var photoStatusImage = photoMatch ? "yes.png" : "cancel.png";
            photoIdData.StatusImagePath = Path.Combine(imagePath,"img",photoStatusImage);

            string detailReportJsonFile = CheckFile(Path.Combine("Files", "detail-report.json"));
            string detailReportJsonContent = File.ReadAllText(detailReportJsonFile);
            DetailedReport detailReport = JsonConvert.DeserializeObject<DetailedReport>(detailReportJsonContent);

            detailReport.ReportTime = claim.ProcessedByAssessorTime?.ToString("dd-MMM-yyyy HH:mm:ss");
            detailReport.PolicyNum = claim.PolicyDetail.ContractNumber;
            detailReport.AgencyName = claim.Vendor.Email;
            detailReport.ClaimType = claim.PolicyDetail.ClaimType.GetEnumDisplayName();
            detailReport.InsuredAmount = claim.PolicyDetail.SumAssuredValue.ToString();
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

            var photoIdFilename = $"photo-id-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.jpg";
            await File.WriteAllBytesAsync(Path.Combine(imagePath, "report", photoIdFilename), claim.AgencyReport.DigitalIdReport.DigitalIdImage);
            photoIdData.PhotoIdPath = Path.Combine(imagePath, "report", photoIdFilename);

            string googlePhotoImagePath = Path.Combine(imagePath, "report", $"google-photo-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            var photoPath = await DownloadMapImageAsync(claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationUrl, googlePhotoImagePath);
            photoIdData.PhotoIdMapUrl = claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationUrl;
            photoIdData.PhotoIdMapPath = photoPath;


            string personAddressUrl = string.Empty;
            string contactNumer = string.Empty;
            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
            {
                detailReport.PersonOfInterestName = claim.CustomerDetail.Name;
                detailReport.VerifyAddress = claim.CustomerDetail.Addressline + "," + claim.CustomerDetail.District.Name +"," +claim.CustomerDetail.State.Code + "," + claim.CustomerDetail.Country.Code +"," + claim.CustomerDetail.PinCode.Code;
                contactNumer = new string('*', claim.CustomerDetail.ContactNumber.Length - 4) + claim.CustomerDetail.ContactNumber.Substring(claim.CustomerDetail.ContactNumber.Length - 4); 
                personAddressUrl = claim.CustomerDetail.CustomerLocationMap;
            }
            else
            {
                detailReport.PersonOfInterestName = claim.BeneficiaryDetail.Name;
                detailReport.VerifyAddress = claim.BeneficiaryDetail.Addressline + "," + claim.BeneficiaryDetail.District.Name + "," + claim.BeneficiaryDetail.State.Code + "," + claim.BeneficiaryDetail.Country.Code + "," + claim.BeneficiaryDetail.PinCode.Code;
                contactNumer = claim.BeneficiaryDetail.ContactNumber;
                personAddressUrl = claim.BeneficiaryDetail.BeneficiaryLocationMap;
            }
            string googlePersonAddressImagePath = Path.Combine(imagePath, "report", $"google-person-address-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            var addressPath = await DownloadMapImageAsync(personAddressUrl, googlePersonAddressImagePath);

            photoIdData.PersonAddressImage = addressPath;
            photoIdData.PersonName = detailReport.PersonOfInterestName;
            photoIdData.Salutation = "MR/MS";
            photoIdData.PersonContact = contactNumer;
            photoIdData.BoardingTill = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLatTime.GetValueOrDefault();
            photoIdData.PhotoIdTime = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLatTime.GetValueOrDefault();
            photoIdData.WeatherData = claim.AgencyReport.DigitalIdReport.DigitalIdImageData;
            photoIdData.ArrivalAirport = "";
            photoIdData.ArrivalAbvr = claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationAddress;
            if(photoMatch)
            {
                photoIdData.PhotoIdRemarks = $"CONFIRM";
            }
            else
            {
                photoIdData.PhotoIdRemarks = $"NOT SURE";
            }
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
            agencyDetailData.ReportSummaryDescription =new List<string> { claim.AgencyReport?.AgentRemarks };
            

            PdfReportBuilder ConcertTicketBuilder = new PdfReportBuilder();

            ConcertTicketBuilder.DetailedReport = detailReport;
            ConcertTicketBuilder.AgencyDetailData = agencyDetailData;
            ConcertTicketBuilder.PhotoIdData = photoIdData;
            ConcertTicketBuilder.PanData = panIdData;

            return ConcertTicketBuilder.Build(imagePath);
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