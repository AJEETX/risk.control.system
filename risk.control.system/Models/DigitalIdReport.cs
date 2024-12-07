using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class DigitalIdReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long DigitalIdReportId { get; set; }

        [Display(Name = "Insurer name")]
        public long? ClientCompanyId { get; set; }

        [Display(Name = "Insurer name")]
        public virtual ClientCompany? ClientCompany { get; set; }

        [Display(Name = "Digital Id Image")]
        public string? DigitalIdImagePath { get; set; }

        [Display(Name = "Digital Id Image")]
        public byte[]? DigitalIdImage { get; set; }

        [Display(Name = "Digital Id Data")]
        public string? DigitalIdImageData { get; set; } = "No Location Info...";

        [Display(Name = "Digital Id Location")]
        public string? DigitalIdImageLocationUrl { get; set; }

        [Display(Name = "Digital Id Location Address")]
        public string? DigitalIdImageLocationAddress { get; set; } = "No Address data";

        public string? DigitalIdImageMatchConfidence { get; set; } = string.Empty;
        public bool MatchExecuted { get; set; } = false;
        public float Similarity { get; set; } = 0;

        public string? DigitalIdImageLongLat { get; set; }
        public DateTime? DigitalIdImageLongLatTime { get; set; }
        public DigitalIdReportType ReportType { get; set; } = DigitalIdReportType.SINGLE_FACE;

        public override string ToString()
        {
            return $"Digital Id Information" +
                $"Valid: {MatchExecuted}";
        }
    }

    public enum DigitalIdReportType
    {
        SINGLE_FACE,
        DUAL_FACE,
        HOUSE_FRONT,
    }
}