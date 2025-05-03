using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class DigitalIdReport : IdReportBase
    {
        public string? MatchConfidence { get; set; } = string.Empty;
        public float Similarity { get; set; } = 0;
        public bool Has2Face { get; set; } = false;
        public DigitalIdReportType ReportType { get; set; }
        // Foreign key to LocationTemplate
        public long? LocationTemplateId { get; set; }  // This is the FK property
        public LocationTemplate? LocationTemplate { get; set; }  // Navigation property

    }

    public enum DigitalIdReportType
    {
        AGENT_FACE,
        SINGLE_FACE,
        CUSTOMER_FACE,
        BENEFICIARY_FACE,
        DUAL_FACE,
        HOUSE_FRONT,
    }
}