using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class ClaimReport
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ClaimReportId { get; set; } = Guid.NewGuid().ToString();

        public string? AgentRemarks { get; set; }

        [Display(Name = "Agent Location Image")]
        public string? AgentLocationPictureUrl { get; set; }

        [Display(Name = "Agent Location Image")]
        public byte[]? AgentLocationPicture { get; set; }

        [Display(Name = "Agent Location Image")]
        [NotMapped]
        public IFormFile? AgentLocationImage { get; set; }

        [Display(Name = "Agent Location Image")]
        public string? AgentOcrUrl { get; set; }

        [Display(Name = "Agent Location Image")]
        public byte[]? AgentOcrPicture { get; set; }

        [Display(Name = "Agent Ocr Image")]
        [NotMapped]
        public IFormFile? AgentOcrImage { get; set; }

        [Display(Name = "Agent Ocr Data")]
        public string? AgentOcrData { get; set; }

        [Display(Name = "Agent Ocr Image")]
        public string? AgentQrUrl { get; set; }

        [Display(Name = "Agent Qr Image")]
        public byte[]? AgentQrPicture { get; set; }

        [Display(Name = "Agent Qr Image")]
        [NotMapped]
        public IFormFile? AgentQrImage { get; set; }

        [Display(Name = "Agent Qr Data")]
        public string? QrData { get; set; }

        public string? LongLat { get; set; }

        [Display(Name = "Supervisor Document")]
        public byte[]? SupervisorPicture { get; set; }

        [Display(Name = "Supervisor Document")]
        [NotMapped]
        public IFormFile? SupervisorDocumentImage { get; set; }

        public string? SupervisorRemarks { get; set; }
        public SupervisorRemarkType? SupervisorRemarkType { get; set; }
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
}