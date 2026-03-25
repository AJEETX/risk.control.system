using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models.ViewModel
{
    public class AgentData
    {
        public long Id { get; set; }
        public string Photo { get; set; } = default!;
        [EmailAddress]
        public string Email { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string Addressline { get; set; } = default!;
        public bool Active { get; set; }
        public string Roles { get; set; } = default!;
        public string Country { get; set; } = default!;
        public string? Flag { get; set; }
        public int Count { get; set; }
        public string UpdateBy { get; set; } = default!;
        public string Role { get; set; } = default!;
        public bool AgentOnboarded { get; set; }
        public string RawEmail { get; set; } = default!;
        public string? PersonMapAddressUrl { get; set; }
        public string? MapDetails { get; set; }
        public int PinCode { get; set; }
        public string Distance { get; set; } = default!;
        public float DistanceInMetres { get; set; }
        public string Duration { get; set; } = default!;
        public int DurationInSeconds { get; set; }
        public string? AddressLocationInfo { get; set; }
    }
}
