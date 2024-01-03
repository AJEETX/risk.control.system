using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class DigitalIdReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string DigitalIdReportId { get; set; } = Guid.NewGuid().ToString();

        [Display(Name = "Digital Id Image")]
        public string? DigitalIdImagePath { get; set; }

        [Display(Name = "Digital Id Image")]
        public byte[]? DigitalIdImage { get; set; }

        [Display(Name = "Digital Id Data")]
        public string? DigitalIdImageData { get; set; }

        [Display(Name = "Digital Id Location")]
        public string? DigitalIdImageLocationUrl { get; set; }

        [Display(Name = "Digital Id Location Address")]
        public string? DigitalIdImageLocationAddress { get; set; }

        public string? DigitalIdImageMatchConfidence { get; set; } = string.Empty;

        public string? DigitalIdImageLongLat { get; set; }
        public DateTime? DigitalIdImageLongLatTime { get; set; } = DateTime.UtcNow;
        public DigitalIdReportType ReportType { get; set; } = DigitalIdReportType.SINGLE_FACE;
        public string? ReportTemplateId { get; set; }
        public ReportTemplate? ReportTemplate { get; set; }
    }

    public enum DigitalIdReportType
    {
        SINGLE_FACE,
        DUAL_FACE,
        HOUSE_FRONT,
    }
}