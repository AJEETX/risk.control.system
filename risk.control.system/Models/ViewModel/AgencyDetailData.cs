namespace risk.control.system.Models.ViewModel
{
    public class AgencyDetailData
    {
        public string SupervisorCommentsTitle { get; set; } = "Supervisor Comments";
        public string AgentReportTitle { get; set; } = "Agent Summary report";
        public string ReportSummary { get; set; }
        public string? AssessorSummary { get; set; }
        public List<AgentQuestionAnswer> ReportSummaryDescription { get; set; } = new List<AgentQuestionAnswer>();

        public string AssessmentDescriptionTitle { get; set; } = "Assessor comments";
        public string WeatherDetail { get; set; }

        public string AddressVisitedTitle { get; set; }
        public string ExpectedAddressTitle { get; set; }
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
                     "}";
        }
    }
    public class AgentQuestionAnswer
    {
        public string? Question { get; set; }
        public string? Answer { get; set; }

    }
}