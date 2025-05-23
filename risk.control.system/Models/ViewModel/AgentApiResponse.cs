namespace risk.control.system.Models.ViewModel
{
    public class PanVerifyResponse
    {
        public string Action { get; set; }
        public string completed_at { get; set; }
        public string created_at { get; set; }
        public string group_id { get; set; }
        public string request_id { get; set; }
        public Result? result { get; set; }
        public string status { get; set; }
        public string? task_id { get; set; }
        public string? type { get; set; }
        public string? error { get; set; }
        public string? count_remain { get; set; }
    }
    public class PanRequest
    {
        public string PAN { get; set; }
    }
    public class PanResponse
    {
        public string pan { get; set; }
        public string type { get; set; }
        public int reference_id { get; set; }
        public string name_provided { get; set; }
        public string registered_name { get; set; }
        public bool valid { get; set; }
        public string message { get; set; }
        public string name_match_score { get; set; }
        public string name_match_result { get; set; }
        public string aadhaar_seeding_status { get; set; }
        public string name_pan_card { get; set; }
        public string pan_status { get; set; }
        public string aadhaar_seeding_status_desc { get; set; }
    }
    public class Result
    {
        public SourceOutput source_output { get; set; }
    }

    public class SourceOutput
    {
        public bool? aadhaar_seeding_status { get; set; }
        public string first_name { get; set; }
        public object gender { get; set; }
        public string id_number { get; set; }
        public string last_name { get; set; }
        public string? middle_name { get; set; }
        public string? name_on_card { get; set; }
        public string? source { get; set; }
        public string status { get; set; }
    }

    public class PanVerifyRequest
    {
        public string task_id { get; set; }
        public string group_id { get; set; }
        public PanNumber data { get; set; }
    }

    public class PanNumber
    {
        public string id_number { get; set; }
    }

    public class PanInValidationResponse
    {
        public string error { get; set; }
        public string message { get; set; }
        public int status { get; set; }
    }

    public class PanValidationResponse
    {
        public string @entity { get; set; }
        public string pan { get; set; }
        public string first_name { get; set; }
        public string middle_name { get; set; }
        public string last_name { get; set; }
    }

    public class FaceMatchDetail
    {
        public decimal FaceLeftCoordinate { get; set; }
        public decimal FaceTopCcordinate { get; set; }
        public string Confidence { get; set; }
    }

    public class FaceImageDetail
    {
        public string DocType { get; set; }
        public string DocumentId { get; set; }
        public string DateOfBirth { get; set; }
        public string MaskedImage { get; set; }
        public string? OcrData { get; set; }
    }

    public class MatchImage
    {
        public string Source { get; set; }
        public string Dest { get; set; }
    }

    public class MaskImage
    {
        public string Image { get; set; }
    }

    public class SubmitData
    {
        public string Email { get; set; }
        public long CaseId { get; set; }
        public string Remarks { get; set; }
    }

    public class FaceData
    {
        public IFormFile? Image { get; set; }
        public string Email { get; set; }
        public long CaseId { get; set; }
        public string? LocationName { get; set; }
        public string? ReportName { get; set; }

        public string? LocationLatLong { get; set; }
    }

    public class DocumentData
    {
        public IFormFile? Image { get; set; }
        public string Email { get; set; }
        public long CaseId { get; set; }
        public string? LocationName { get; set; }
        public string? ReportName { get; set; }
        public string LocationLatLong { get; set; }
    }

    public class MediaData
    {
        public IFormFile MediaFile { get; set; }
        public byte[] Mediabytes { get; set; }
        public string? Name { get; set; }
        public string? ClaimId { get; set; }
        public string Email { get; set; }
        public string LongLat { get; set; }
    }

    public class AudioData : MediaData
    {
    }

    public class VideoData : MediaData
    {
    }

    public class VerifyMobileRequest
    {
        public string Mobile { get; set; }
        public string Uid { get; set; }
        public bool CheckUid { get; set; } = false;
        public bool SendSMS { get; set; } = false;
    }

    public class VerifyIdRequest
    {
        public string Image { get; set; }
        public string Uid { get; set; }
        public bool VerifyId { get; set; } = false;
    }

    public class VerifyDocumentRequest
    {
        public string Image { get; set; }
        public string Uid { get; set; }
        public string Type { get; set; } = "PAN";
        public bool VerifyPan { get; set; } = false;
    }
}