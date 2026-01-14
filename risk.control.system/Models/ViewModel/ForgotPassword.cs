namespace risk.control.system.Models.ViewModel
{
    public class ForgotPassword
    {
        public long? Id { get; set; }
        public string? Email { get; set; }
        public bool Reset { get; set; } = false;
        public string Message { get; set; }
        public string Flag { get; set; }
        public byte[]? ProfilePicture { get; set; }
        public string ProfileImage { get; set; }
    }
}