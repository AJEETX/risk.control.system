namespace risk.control.system.Models.ViewModel
{
    public class DetailedReport
    {
        public string ReportTitle { get; set; }
        public string ReportQr { get; set; }
        public string AgencyNameTitle { get; set; }
        public string AgencyName { get; set; }
        public string InsurerName { get; set; }
        public string InsurerLogo { get; set; }
        public string AgencyLogo { get; set; }
        public string ClaimTypeTitle { get; set; }

        public string ReportTime { get; set; }
        public string PolicyNumTitle { get; set; }
        public string PolicyNum { get; set; }
        public string ClaimType { get; set; }
        public string ServiceTypeTitle { get; set; } = "Investigation Type";
        public string ServiceType { get; set; }
        public string InsuredAmountTitle { get; set; }
        public string InsuredAmount { get; set; }
        public string PersonOfInterestNameTitle { get; set; }
        public string PersonOfInterestName { get; set; }
        public string AgentOfInterestName { get; set; }
        public string Reason2VerifyTitle { get; set; }
        public string Reason2Verify { get; set; }
        public string VerifyAddressTitle { get; set; }
        public string VerifyAddress { get; set; }

        public override string ToString()
        {
            return "TicketData{" +
                    "AgencyName=" + AgencyName +
                     ", Policy Number=" + PolicyNum +
                    ", Claim Type=" + ClaimType +
                    ", Insured Amount=" + InsuredAmount +
                    ", Person Name=" + PersonOfInterestName +
                    ", Reason 2 Verify=" + Reason2Verify +
                    ", VerifyAddress=" + VerifyAddress +
                     "}";
        }
    }
}