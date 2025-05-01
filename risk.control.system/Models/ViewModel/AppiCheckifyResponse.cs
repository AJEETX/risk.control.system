namespace risk.control.system.Models.ViewModel
{
    public class AppiCheckifyResponse
    {
        public long BeneficiaryId { get; set; }
        public string? LocationImage { get; set; }
        public string? LocationLongLat { get; set; }
        public DateTime? LocationTime { get; set; } = DateTime.Now;
        public string? OcrImage { get; set; }
        public string? OcrLongLat { get; set; }
        public DateTime? OcrTime { get; set; } = DateTime.Now;
        public string? FacePercent { get; set; }
        public bool? Valid { get; set; }
        public bool? Registered { get; set; }
    }
}