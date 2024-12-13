using Gehtsoft.PDFFlow.Builder;

using Newtonsoft.Json;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Helpers
{
    public class PdfReportRunner
    {
        public static DocumentBuilder Run(string imagePath, ClaimsInvestigation claim)
        {
            string boardingJsonFile = CheckFile(Path.Combine("Files", "boarding-data.json"));
            string boardingJsonContent = File.ReadAllText(boardingJsonFile);
            BoardingData boardingData = JsonConvert.DeserializeObject<BoardingData>(boardingJsonContent);

            var photoMatch = claim.AgencyReport.DigitalIdReport.Similarity > 70;
            boardingData.Flight = photoMatch ? "YES" : "NO";
            

            string ticketJsonFile = CheckFile(Path.Combine("Files", "concert-ticket-data.json"));
            string ticketJsonContent = File.ReadAllText(ticketJsonFile);
            TicketData ticketData = JsonConvert.DeserializeObject<TicketData>(ticketJsonContent);

            ticketData.PolicyNum = claim.PolicyDetail.ContractNumber;
            ticketData.AgencyName = claim.Vendor.Email;
            ticketData.ClaimType = claim.PolicyDetail.ClaimType.GetEnumDisplayName();
            ticketData.InsuredAmount = claim.PolicyDetail.SumAssuredValue.ToString();
                ticketData.Reason2Verify = claim.PolicyDetail.CaseEnabler.Name.ToLower();

            string filePath = claim.ClientCompany.DocumentUrl;

            // Get the file name
            string fileName = Path.GetFileName(filePath); // "image.jpg"

            // Get the folder path
            string folderPath = Path.GetDirectoryName(filePath); // "/img"
            string folderName = Path.GetFileName(folderPath);

            ticketData.InsurerLogo = Path.Combine(imagePath, folderName, fileName);

            filePath = claim.Vendor.DocumentUrl;

            // Get the file name
            fileName = Path.GetFileName(filePath); // "image.jpg"

            // Get the folder path
            folderPath = Path.GetDirectoryName(filePath); // "/img"
            folderName = Path.GetFileName(folderPath);    // "img"

            ticketData.AgencyLogo = Path.Combine(imagePath, folderName, fileName);

            string contactNumer = string.Empty;
            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
            {
                ticketData.PersonOfInterestName = claim.CustomerDetail.Name;
                ticketData.VerifyAddress = claim.CustomerDetail.Addressline + "," + claim.CustomerDetail.District.Name +"," +claim.CustomerDetail.State.Code + "," + claim.CustomerDetail.Country.Code +"," + claim.CustomerDetail.PinCode.Code;
                contactNumer = claim.CustomerDetail.ContactNumber;
            }
            else
            {
                ticketData.PersonOfInterestName = claim.BeneficiaryDetail.Name;
                ticketData.VerifyAddress = claim.BeneficiaryDetail.Addressline + "," + claim.BeneficiaryDetail.District.Name + "," + claim.BeneficiaryDetail.State.Code + "," + claim.BeneficiaryDetail.Country.Code + "," + claim.BeneficiaryDetail.PinCode.Code;
                contactNumer = claim.BeneficiaryDetail.ContactNumber;
            }

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
    }
}