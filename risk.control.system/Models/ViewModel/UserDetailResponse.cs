namespace risk.control.system.Models.ViewModel
{
    public class UserDetailResponse
    {
        public long Id { get; set; }
        public int? Count { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string? RawEmail { get; set; }
        public string Phone { get; set; }
        public string Photo { get; set; }
        public bool? Active { get; set; }
        public string Addressline { get; set; }
        public string? District { get; set; }
        public string? State { get; set; }
        public string? StateName { get; set; }
        public string? Country { get; set; }
        public string? Flag { get; set; }
        public string Roles { get; set; }
        public string? Role { get; set; }
        public int? Pincode { get; set; }
        public string? PincodeName { get; set; }
        public string? Updated { get; set; }
        public string? UpdatedBy { get; set; }
        public string OnlineStatus { get; set; }
        public string OnlineStatusName { get; set; }
        public string OnlineStatusIcon { get; set; }
        public bool IsUpdated { get; set; }
        public bool? AgentOnboarded { get; set; }
        public bool? Agent { get; set; }
        public DateTime LastModified { get; set; }
        public bool LoginVerified { get; set; } = false;
    }
}
