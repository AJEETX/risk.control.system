using Gehtsoft.PDFFlow.Builder;

using Newtonsoft.Json;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Helpers
{
    public class PdfReportRunner
    {
        static string googleImagePath = $"google-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png";

        public static async Task<DocumentBuilder> Run(string imagePath, ClaimsInvestigation claim)
        {
            string boardingJsonFile = CheckFile(Path.Combine("Files", "boarding-data.json"));
            string boardingJsonContent = File.ReadAllText(boardingJsonFile);
            BoardingData boardingData = JsonConvert.DeserializeObject<BoardingData>(boardingJsonContent);

            var photoMatch = claim.AgencyReport.DigitalIdReport.Similarity > 70;
            boardingData.Flight = photoMatch ? "YES" : "NO";

             
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

            var photoIdPath = $"photo-id-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.jpg";
            await File.WriteAllBytesAsync(Path.Combine(imagePath, "report", photoIdPath), claim.AgencyReport.DigitalIdReport.DigitalIdImage);
            boardingData.PhotoIdPath = Path.Combine(imagePath, "report", photoIdPath);


            var panCardFileName = $"pan-card-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.jpg";
            await File.WriteAllBytesAsync(Path.Combine(imagePath, "report", panCardFileName), claim.AgencyReport.PanIdReport.DocumentIdImage);
            boardingData.PanPhotoPath = Path.Combine(imagePath, "report", panCardFileName);

            googleImagePath = Path.Combine(imagePath, "report", googleImagePath);


            var path = await DownloadMapImageAsync(claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationUrl, googleImagePath);
            boardingData.PhotoIdMapUrl = claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationUrl;
            boardingData.PhotoIdMapPath = path;

            path = await DownloadMapImageAsync(claim.AgencyReport.PanIdReport.DocumentIdImageLocationUrl, googleImagePath);
            boardingData.PanMapUrl = claim.AgencyReport.PanIdReport.DocumentIdImageLocationUrl;
            boardingData.PanMapPath = path;

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
            path = await DownloadMapImageAsync(personAddressUrl, googleImagePath);

            boardingData.PersonAddressImage = path;
            boardingData.DepartureAirport = ticketData.PersonOfInterestName;
            boardingData.DepartureAbvr = "MR/MS";
            boardingData.BoardingGate = contactNumer;
            boardingData.BoardingTill = claim.AgencyReport.DigitalIdReport.Updated.Value;
            boardingData.DepartureTime = claim.AgencyReport.DigitalIdReport.Created;
            boardingData.Arrival = claim.AgencyReport.Created;
            boardingData.ArrivalAirport = "";
            boardingData.ArrivalAbvr = claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationAddress;

            string jsonFile = CheckFile(Path.Combine("Files", "concert-data.json"));
            string jsonContent = File.ReadAllText(jsonFile);
            ConcertData concertData = JsonConvert.DeserializeObject<ConcertData>(jsonContent);

            concertData.ReportSummary = claim.AgencyReport?.AgentRemarks;
            concertData.AgencyDomain = claim.Vendor.Email;
            concertData.AgencyContact = claim.Vendor.PhoneNumber;
            concertData.SupervisorEmail = claim.AgencyReport?.SupervisorEmail;
            concertData.AddressVisited = ticketData.VerifyAddress;
            concertData.WeatherDetail = claim.AgencyReport.SupervisorRemarks;
            concertData.ReportSummaryDescription =new List<string> { claim.AgencyReport?.AssessorRemarks };

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
            boardingData0.Flight = panValid ? "YES" : "NO";
            boardingData0.DepartureAirport = claim.AgencyReport.PanIdReport.DocumentIdImageData.Length > 30 ? 
                claim.AgencyReport.PanIdReport.DocumentIdImageData.Substring(0,30) + "...": 
                claim.AgencyReport.PanIdReport.DocumentIdImageData;
            boardingData0.DepartureAbvr = "PAN/CARD";
            boardingData0.BoardingGate = contactNumer;
            boardingData0.BoardingTill = claim.AgencyReport.PanIdReport.Created;
            boardingData0.DepartureTime = claim.AgencyReport.PanIdReport.Created;
            boardingData0.Arrival = claim.AgencyReport.Created;
            boardingData0.ArrivalAirport = "";
            boardingData0.ArrivalAbvr = claim.AgencyReport.PanIdReport.DocumentIdImageLocationAddress;

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