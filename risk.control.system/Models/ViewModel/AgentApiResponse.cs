using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models.ViewModel
{
    public class PanRequest
    {
        public string PAN { get; set; }
    }

    public class PanResponse
    {
        public bool valid { get; set; }
    }

    public class FaceImageDetail
    {
        public string DocType { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "CaseId must be a positive number.")]
        public string DocumentId { get; set; }

        public string MaskedImage { get; set; }
        public string? OcrData { get; set; }
    }

    public class SubmitData
    {
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "CaseId must be a positive number.")]
        public long CaseId { get; set; }

        [Required]
        public string Remarks { get; set; }
    }

    public class FaceData
    {
        [Required]
        public IFormFile Image { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "CaseId must be a positive number.")]
        public long CaseId { get; set; }

        [Required]
        public string LocationName { get; set; }

        [Required]
        public string ReportName { get; set; }

        [Required]
        public string LocationLatLong { get; set; }
    }

    public class DocumentData
    {
        [Required]
        public IFormFile Image { get; set; }

        [EmailAddress]
        [Required]
        public string Email { get; set; }

        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "CaseId must be a positive number.")]
        public long CaseId { get; set; }

        [Required]
        public string LocationName { get; set; }

        [Required]
        public string ReportName { get; set; }

        [Required]
        public string LocationLatLong { get; set; }
    }

    public class VerifyMobileRequest
    {
        [Required]
        [StringLength(11, MinimumLength = 9, ErrorMessage = "Mobile number must be between 9 and 10 digits.")]
        public string Mobile { get; set; }

        [Required]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "UID number must be 4 digits.")]
        public string Uid { get; set; }

        public bool SendSMSForRetry { get; set; } = false;
    }

    public class VerifyIdRequest
    {
        [Required]
        public string Image { get; set; }

        [StringLength(4, MinimumLength = 4, ErrorMessage = "UID number must be 4 digits.")]
        [Required]
        public string Uid { get; set; }

        public bool VerifyId { get; set; } = false;
    }
}