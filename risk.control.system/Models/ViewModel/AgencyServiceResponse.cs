namespace risk.control.system.Models.ViewModel
{
    public class AgencyServiceResponse
    {
        public long VendorId { get; set; }
        public long Id { get; set; }
        public string CaseType { get; set; } = default!;
        public string ServiceType { get; set; } = default!;
        public string District { get; set; } = default!;
        public string Districts { get; set; } = default!;
        public string StateCode { get; set; } = default!;
        public string State { get; set; } = default!;
        public string CountryCode { get; set; } = default!;
        public string Country { get; set; } = default!;
        public string Flag { get; set; } = default!;
        public string RawPincodes { get; set; } = default!;
        public string Pincodes { get; set; } = default!;
        public string Rate { get; set; } = default!;
        public string UpdatedBy { get; set; } = default!;
        public DateTime Updated { get; set; } = default!;
        public bool IsUpdated { get; set; }
        public DateTime? LastModified { get; set; }
    }
}