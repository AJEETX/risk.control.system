using System.ComponentModel.DataAnnotations;

using risk.control.system.Helpers;

namespace risk.control.system.Models
{
    public class FaceIdReport : ReportBase
    {
        public string? MatchConfidence { get; set; } = string.Empty;
        public float Similarity { get; set; } = 0;
        public bool Has2Face { get; set; } = false;
        public DigitalIdReportType ReportType { get; set; }
        public long? LocationReportId { get; set; }  // This is the FK property
        public LocationReport? LocationReport { get; set; }  // Navigation property
        public override string ToString()
        {
            return $"Face Report:\n" +
                $"Report Type:{ReportType.GetEnumDisplayName()}";
        }
    }

    public enum DigitalIdReportType
    {
        [Display(Name = "Agent Face")]
        AGENT_FACE,

        [Display(Name = "Single Face")]
        SINGLE_FACE,

        [Display(Name = "Customer Face")]
        CUSTOMER_FACE,

        [Display(Name = "Beneficiary Face")]
        BENEFICIARY_FACE,

        [Display(Name = "Dual Face")]
        DUAL_FACE,

        [Display(Name = "House Front")]
        HOUSE_FRONT,
    }

}