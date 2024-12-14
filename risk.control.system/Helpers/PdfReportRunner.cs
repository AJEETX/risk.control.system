using Gehtsoft.PDFFlow.Builder;

using Newtonsoft.Json;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Helpers
{
    public class PdfReportRunner
    {

        public static async Task<DocumentBuilder> Run(string imagePath, ClaimsInvestigation claim)
        {
            string boardingJsonFile = CheckFile(Path.Combine("Files", "boarding-data.json"));
            string boardingJsonContent = File.ReadAllText(boardingJsonFile);
            BoardingData boardingData = JsonConvert.DeserializeObject<BoardingData>(boardingJsonContent);

            var photoMatch = claim.AgencyReport.DigitalIdReport.Similarity > 70;
            boardingData.FaceMatchStatus = photoMatch ? "YES" : "NO";

             
            string ticketJsonFile = CheckFile(Path.Combine("Files", "concert-ticket-data.json"));
            string ticketJsonContent = File.ReadAllText(ticketJsonFile);
            TicketData ticketData = JsonConvert.DeserializeObject<TicketData>(ticketJsonContent);

            ticketData.ReportTime = claim.ProcessedByAssessorTime?.ToString("dd-MMM-yyyy HH:mm:ss");
            ticketData.PolicyNum = claim.PolicyDetail.ContractNumber;
            ticketData.AgencyName = claim.Vendor.Email;
            ticketData.ClaimType = claim.PolicyDetail.ClaimType.GetEnumDisplayName();
            ticketData.InsuredAmount = claim.PolicyDetail.SumAssuredValue.ToString();
                ticketData.Reason2Verify = claim.PolicyDetail.CaseEnabler.Name.ToLower();

            string filePath = claim.ClientCompany.DocumentUrl;
            string fileName = Path.GetFileName(filePath); // "image.jpg"
            string folderPath = Path.GetDirectoryName(filePath); // "/img"
            string folderName = Path.GetFileName(folderPath);
            ticketData.InsurerLogo = Path.Combine(imagePath, folderName, fileName);
            
            filePath = claim.Vendor.DocumentUrl;
            fileName = Path.GetFileName(filePath); // "image.jpg"
            folderPath = Path.GetDirectoryName(filePath); // "/img"
            folderName = Path.GetFileName(folderPath);    // "img"
            ticketData.AgencyLogo = Path.Combine(imagePath, folderName, fileName);

            var photoIdFilename = $"photo-id-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.jpg";
            await File.WriteAllBytesAsync(Path.Combine(imagePath, "report", photoIdFilename), claim.AgencyReport.DigitalIdReport.DigitalIdImage);
            boardingData.PhotoIdPath = Path.Combine(imagePath, "report", photoIdFilename);

            var panCardFileName = $"pan-card-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.jpg";
            await File.WriteAllBytesAsync(Path.Combine(imagePath, "report", panCardFileName), claim.AgencyReport.PanIdReport.DocumentIdImage);
            boardingData.PanPhotoPath = Path.Combine(imagePath, "report", panCardFileName);

            string googlePhotoImagePath = Path.Combine(imagePath, "report", $"google-photo-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png"); 
            var photoPath = await DownloadMapImageAsync(claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationUrl, googlePhotoImagePath);
            boardingData.PhotoIdMapUrl = claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationUrl;
            boardingData.PhotoIdMapPath = photoPath;

            string googlePanImagePath = Path.Combine(imagePath, "report", $"google-pan-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            var panPath = await DownloadMapImageAsync(claim.AgencyReport.PanIdReport.DocumentIdImageLocationUrl, googlePanImagePath);
            boardingData.PanMapUrl = claim.AgencyReport.PanIdReport.DocumentIdImageLocationUrl;
            boardingData.PanMapPath = panPath;

            string personAddressUrl = string.Empty;
            string contactNumer = string.Empty;
            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
            {
                ticketData.PersonOfInterestName = claim.CustomerDetail.Name;
                ticketData.VerifyAddress = claim.CustomerDetail.Addressline + "," + claim.CustomerDetail.District.Name +"," +claim.CustomerDetail.State.Code + "," + claim.CustomerDetail.Country.Code +"," + claim.CustomerDetail.PinCode.Code;
                contactNumer = claim.CustomerDetail.ContactNumber;
                personAddressUrl = claim.CustomerDetail.CustomerLocationMap;
            }
            else
            {
                ticketData.PersonOfInterestName = claim.BeneficiaryDetail.Name;
                ticketData.VerifyAddress = claim.BeneficiaryDetail.Addressline + "," + claim.BeneficiaryDetail.District.Name + "," + claim.BeneficiaryDetail.State.Code + "," + claim.BeneficiaryDetail.Country.Code + "," + claim.BeneficiaryDetail.PinCode.Code;
                contactNumer = claim.BeneficiaryDetail.ContactNumber;
                personAddressUrl = claim.BeneficiaryDetail.BeneficiaryLocationMap;
            }
            string googlePersonAddressImagePath = Path.Combine(imagePath, "report", $"google-person-address-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            var addressPath = await DownloadMapImageAsync(personAddressUrl, googlePersonAddressImagePath);

            boardingData.PersonAddressImage = addressPath;
            boardingData.PersonName = ticketData.PersonOfInterestName;
            boardingData.Salutation = "MR/MS";
            boardingData.PersonContact = contactNumer;
            boardingData.BoardingTill = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLatTime.GetValueOrDefault();
            boardingData.PhotoIdTime = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLatTime.GetValueOrDefault();
            boardingData.WeatherData = claim.AgencyReport.DigitalIdReport.DigitalIdImageData;
            boardingData.ArrivalAirport = "";
            boardingData.ArrivalAbvr = claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationAddress;
            if(photoMatch)
            {
                boardingData.PhotoIdRemarks = $"CONFIRM";
            }
            else
            {
                boardingData.PhotoIdRemarks = $"NOT SURE";
            }
            string jsonFile = CheckFile(Path.Combine("Files", "concert-data.json"));
            string jsonContent = File.ReadAllText(jsonFile);
            ConcertData concertData = JsonConvert.DeserializeObject<ConcertData>(jsonContent);

            concertData.ReportSummary = claim.AgencyReport?.SupervisorRemarks;
            concertData.AgencyDomain = claim.Vendor.Email;
            concertData.AgencyContact = claim.Vendor.PhoneNumber;
            concertData.SupervisorEmail = claim.AgencyReport?.SupervisorEmail;
            concertData.AddressVisited = ticketData.VerifyAddress;
            concertData.WeatherDetail = claim.AgencyReport.AssessorRemarks;
            concertData.ReportSummaryDescription =new List<string> { claim.AgencyReport?.AgentRemarks };

            var ticketJsonFile1 = CheckFile(Path.Combine("Files", "bp-ticket-data.json"));
            var ticketJsonContent1 = File.ReadAllText(ticketJsonFile1);
            var ticketData1 = JsonConvert.DeserializeObject<TicketData1>(ticketJsonContent1);
            
            string ticketJsonFile0 = CheckFile(Path.Combine("Files", "bp-ticket-data1.json"));
            string ticketJsonContent0 = File.ReadAllText(ticketJsonFile0);
            TicketData1 ticketData0 = JsonConvert.DeserializeObject<TicketData1>(ticketJsonContent0);

            string boardingJsonFile0 = CheckFile(Path.Combine("Files", "boarding-data1.json"));
            string boardingJsonContent0 = File.ReadAllText(boardingJsonFile0);
            BoardingData boardingData0 = JsonConvert.DeserializeObject<BoardingData>(boardingJsonContent0);
            var panValid = claim.AgencyReport.PanIdReport.DocumentIdImageValid.GetValueOrDefault();
            boardingData0.FaceMatchStatus = panValid ? "YES" : "NO";
            boardingData0.PersonName = claim.AgencyReport.PanIdReport.DocumentIdImageData.Length > 30 ? 
                claim.AgencyReport.PanIdReport.DocumentIdImageData.Substring(0,30) + "...": 
                claim.AgencyReport.PanIdReport.DocumentIdImageData;
            boardingData0.Salutation = "PAN/CARD";
            boardingData0.PersonContact = contactNumer;
            boardingData0.BoardingTill = claim.AgencyReport.PanIdReport.DocumentIdImageLongLatTime.GetValueOrDefault();
            boardingData0.PhotoIdTime = claim.AgencyReport.PanIdReport.DocumentIdImageLongLatTime.GetValueOrDefault();
            boardingData0.WeatherData = claim.AgencyReport.PanIdReport.DocumentIdImageData;
            boardingData0.ArrivalAirport = "";
            boardingData0.ArrivalAbvr = claim.AgencyReport.PanIdReport.DocumentIdImageLocationAddress;
            boardingData0.PhotoIdRemarks = panValid ? "CONFIRM" : "NOT SURE";
            PdfReportBuilder ConcertTicketBuilder = new PdfReportBuilder();

            ConcertTicketBuilder.TicketData = ticketData;
            ConcertTicketBuilder.ConcertData = concertData;
            ConcertTicketBuilder.BoardingData = boardingData;
            ConcertTicketBuilder.BoardingData0 = boardingData0;
            ConcertTicketBuilder.TicketData1 = ticketData1;
            ConcertTicketBuilder.TicketData0 = ticketData0;

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
            using HttpClient client = new HttpClient();
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