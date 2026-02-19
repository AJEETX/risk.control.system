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
        public string DocumentId { get; set; }
        public string MaskedImage { get; set; }
        public string? OcrData { get; set; }
    }

    public class SubmitData
    {
        [EmailAddress]
        public string Email { get; set; }

        public long CaseId { get; set; }
        public string Remarks { get; set; }
    }

    public class FaceData
    {
        public IFormFile? Image { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public long CaseId { get; set; }
        public string? LocationName { get; set; }
        public string? ReportName { get; set; }

        public string? LocationLatLong { get; set; }
    }

    public class DocumentData
    {
        public IFormFile? Image { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public long CaseId { get; set; }
        public string? LocationName { get; set; }
        public string? ReportName { get; set; }
        public string LocationLatLong { get; set; }
    }

    public class VerifyMobileRequest
    {
        public string Mobile { get; set; }
        public string Uid { get; set; }
        public bool SendSMSForRetry { get; set; } = false;
    }

    public class VerifyIdRequest
    {
        public string Image { get; set; }
        public string Uid { get; set; }
        public bool VerifyId { get; set; } = false;
    }
}