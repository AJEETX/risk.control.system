namespace risk.control.system.Models.ViewModel
{
    public class AgentData
    {
        public long Id { get; set; }
        public string Photo { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Addressline { get; set; }
        public bool Active { get; set; }
        public string Roles { get; set; }
        public string Country { get; set; }
        public string? Flag { get; set; }
        public int Count { get; set; }
        public string UpdateBy { get; set; }
        public string Role { get; set; }
        public bool AgentOnboarded { get; set; }
        public string RawEmail { get; set; }
        public string? PersonMapAddressUrl { get; set; }
        public string? MapDetails { get; set; }
        public string PinCode { get; set; }
        public string Distance { get; set; }
        public float DistanceInMetres { get; set; }
        public string Duration { get; set; }
        public int DurationInSeconds { get; set; }
        public string? AddressLocationInfo { get; set; }
    }
}
