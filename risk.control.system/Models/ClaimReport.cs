using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class ClaimReport
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ClaimReportId { get; set; } = Guid.NewGuid().ToString();

        public string? VendorId { get; set; }
        public Vendor? Vendor { get; set; }
        public string? AgentEmail { get; set; }

        public DateTime? AgentRemarksUpdated { get; set; }
        public string? AgentRemarks { get; set; }
        public string? Question1 { get; set; }
        public string? Question2 { get; set; }
        public string? Question3 { get; set; }
        public string? Question4 { get; set; }
        public string? Question5 { get; set; }

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

        public string? DigitalIdLongLat { get; set; }
        public DateTime? DigitalIdLongLatTime { get; set; } = DateTime.UtcNow;

        public string? DigitalIdImageMatchConfidence { get; set; } = string.Empty;

        [Display(Name = "Document Id Image")]
        public string? DocumentIdImagePath { get; set; }

        [Display(Name = "Document Id Image")]
        public byte[]? DocumentIdImage { get; set; }

        public bool? DocumentIdImageValid { get; set; } = false;
        public string? DocumentIdImageType { get; set; }

        [Display(Name = "Document Id Data")]
        public string? DocumentIdImageData { get; set; }

        [Display(Name = "Document Id Location")]
        public string? DocumentIdImageLocationUrl { get; set; }

        [Display(Name = "Document Id Location Address")]
        public string? DocumentIdImageLocationAddress { get; set; }

        public string? DocumentIdImageLongLat { get; set; }
        public DateTime? DocumentIdImageLongLatTime { get; set; } = DateTime.UtcNow;

        public DateTime? SupervisorRemarksUpdated { get; set; }
        public string? SupervisorEmail { get; set; }
        public string? SupervisorRemarks { get; set; }
        public SupervisorRemarkType? SupervisorRemarkType { get; set; }
        public DateTime? AssessorRemarksUpdated { get; set; }
        public string? AssessorEmail { get; set; }
        public string? AssessorRemarks { get; set; }
        public AssessorRemarkType? AssessorRemarkType { get; set; }

        public long CaseLocationId { get; set; }
        public CaseLocation CaseLocation { get; set; }
    }

    public enum SupervisorRemarkType
    {
        OK,
        REVIEW
    }

    public enum AssessorRemarkType
    {
        OK,
        REVIEW,
    }

    public enum OcrImageType
    {
        PAN,
        ADHAAR,
        DL
    }
}