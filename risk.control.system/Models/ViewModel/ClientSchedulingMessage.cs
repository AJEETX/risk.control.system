namespace risk.control.system.Models.ViewModel
{
    public class ClientSchedulingMessage
    {
        public string ClaimId { get; set; }
        public string LongLat { get; set; }
        public string Time { get; set; }
        public string? BaseUrl { get; set; }
    }
}