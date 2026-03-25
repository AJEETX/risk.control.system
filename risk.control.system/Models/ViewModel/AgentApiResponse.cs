using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models.ViewModel
{
    public class PanRequest
    {
        public string PAN { get; set; } = default!;
    }

    public class PanResponse
    {
        public bool valid { get; set; }
    }

    public class FaceImageDetail
    {
        public string DocType { get; set; } = default!;
        public string DocumentId { get; set; } = default!;
        public string MaskedImage { get; set; } = default!;
        public string? OcrData { get; set; }
    }

    public class SubmitData
    {
        [EmailAddress]
        public string Email { get; set; } = default!;

        public long CaseId { get; set; }
        public string Remarks { get; set; } = default!;
    }

    public class FaceData
    {
        public IFormFile? Image { get; set; }

        [EmailAddress]
        public string Email { get; set; } = default!;

        public long CaseId { get; set; }
        public string? LocationName { get; set; }
        public string? ReportName { get; set; }

        public string? LocationLatLong { get; set; }
    }

    public class DocumentData
    {
        public IFormFile? Image { get; set; }

        [EmailAddress]
        public string Email { get; set; } = default!;

        public long CaseId { get; set; }
        public string? LocationName { get; set; }
        public string? ReportName { get; set; }
        public string LocationLatLong { get; set; } = default!;
    }

    public class VerifyMobileRequest
    {
        public string Mobile { get; set; } = default!;
        public string Uid { get; set; } = default!;
        public bool SendSMSForRetry { get; set; } = false;
    }

    public class VerifyIdRequest
    {
        public string Image { get; set; } = default!;
        public string Uid { get; set; } = default!;
        public bool VerifyId { get; set; } = false;
    }
}