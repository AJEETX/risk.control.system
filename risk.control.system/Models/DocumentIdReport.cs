using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class DocumentIdReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long DocumentIdReportId { get; set; }

        [Display(Name = "Document Id Image")]
        public string? DocumentIdImagePath { get; set; }

        [Display(Name = "Document Id Image")]
        public byte[]? DocumentIdImage { get; set; }

        public bool? DocumentIdImageValid { get; set; } = false;
        public string? DocumentIdImageType { get; set; }

        [Display(Name = "Document Id Data")]
        public string? DocumentIdImageData { get; set; } = "No Location Info...";

        [Display(Name = "Document Id Location")]
        public string? DocumentIdImageLocationUrl { get; set; }

        public string? Distance { get; set; }
        public float? DistanceInMetres { get; set; }
        public string? Duration { get; set; }
        public int? DurationInSeconds { get; set; }

        [Display(Name = "Document Id Location Address")]
        public string? DocumentIdImageLocationAddress { get; set; } = "No Address data";

        public string? DocumentIdImageLongLat { get; set; }
        public DateTime? DocumentIdImageLongLatTime { get; set; }

        public bool ValidationExecuted { get; set; } = false;

        public DocumentIdReportType DocumentIdReportType { get; set; } = DocumentIdReportType.PAN;
        public override string ToString()
        {
            return $"Report Information: \n" +
                $"- Valid: {ValidationExecuted}";
        }
    }

    public enum DocumentIdReportType
    {
        ADHAAR,
        PAN,
        DRIVING_LICENSE,
        PASSPORT
    }
}