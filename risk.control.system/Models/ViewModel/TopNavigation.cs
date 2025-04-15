namespace risk.control.system.Models.ViewModel
{
    public class TopNavigation
    {
        public long? UserId { get; set; }
        public string? Email { get; set; }
        public bool CanChangePassword { get; set; }
        public string Country { get; set; }
        public string IsdCode { get; set; }
        public string CountryCode { get; set; }
        public byte[]? ProfilePicture { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        public string Language { get; set; }
        public string Notification { get; set; } = "--";
    }
}
