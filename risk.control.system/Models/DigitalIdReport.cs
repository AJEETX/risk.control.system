using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class DigitalIdReport : IdReportBase
    {
        public string? DigitalIdImageMatchConfidence { get; set; } = string.Empty;
        public float Similarity { get; set; } = 0;

        public DigitalIdReportType ReportType { get; set; } = DigitalIdReportType.SINGLE_FACE;

        public override string ToString()
        {
            return $"Digital Id Information: \n" +
                $"- Valid: {ValidationExecuted}";
        }
    }

    public enum DigitalIdReportType
    {
        AGENT_FACE,
        SINGLE_FACE,
        DUAL_FACE,
        HOUSE_FRONT,
    }
}