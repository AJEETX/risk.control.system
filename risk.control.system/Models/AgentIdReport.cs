namespace risk.control.system.Models
{
    public class AgentIdReport : ReportBase
    {
        public string? DigitalIdImageMatchConfidence { get; set; } = string.Empty;
        public float Similarity { get; set; } = 0;
        public bool Has2Face { get; set; } = false;
        public DigitalIdReportType ReportType { get; set; }

        override public string ToString()
        {
            return $"AgentIdReport: " +
                   $"Report Type={ReportType}, " +
                   $"Image Match Confidence={DigitalIdImageMatchConfidence}, " +
                   $"Similarity={Similarity}, " +
                   $"Has front and back Face={Has2Face}";
        }
    }
}