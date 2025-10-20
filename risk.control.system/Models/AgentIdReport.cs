namespace risk.control.system.Models
{
    public class AgentIdReport : ReportBase
    {
        public string? DigitalIdImageMatchConfidence { get; set; } = string.Empty;
        public float Similarity { get; set; } = 0;
        public bool Has2Face { get; set; } = false;
        public DigitalIdReportType ReportType { get; set; }

    }
}