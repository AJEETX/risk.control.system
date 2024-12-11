namespace risk.control.system.Models.ViewModel
{
    public class ConcertData
    {
        public string SupervisorCommentsTitle { get; set; }
        public string AgentReportTitle { get; set; }
        public string ReportSummary { get; set; }
        public List<string> ReportSummaryDescription { get; set; } // AI

        public string AssessmentDescriptionTitle { get; set; }
        public string WeatherDetail { get; set; }

        public string AddressVisitedTitle { get; set; }
        public string AddressVisited { get; set; }
        public string ContactAgencyTitle { get; set; }
        public string SupervisorEmail { get; set; }
        public string AgencyDomain { get; set; }
        public string AgencyContact { get; set; }
        public string ReportDisclaimer { get; set; }

        public override string ToString()
        {
            return "{" + ", SupervisorCommentsTitle=" + SupervisorCommentsTitle +
                    ", AgentReportTitle=" + AgentReportTitle +
                    ", ReportSummary=" + ReportSummary +
                    ", ReportSummaryDescription: [" + ReportSummaryDescription.ToString() + "]" +
                     "}";
        }
    }
}