namespace risk.control.system.Models.ViewModel
{
    public class AgencyServiceResponse
    {
        public long VendorId { get; set; }
        public long Id { get; set; }
        public string CaseType { get; set; }
        public string ServiceType { get; set; }
        public string District { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Flag { get; set; }
        public string RawPincodes { get; set; }
        public string Pincodes { get; set; }
        public string Rate { get; set; }
        public string UpdatedBy { get; set; }
        public string Updated { get; set; }
        public bool IsUpdated { get; set; }
        public DateTime? LastModified { get; set; }
    }
}