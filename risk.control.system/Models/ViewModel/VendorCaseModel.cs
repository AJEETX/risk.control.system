namespace risk.control.system.Models.ViewModel
{
    public class VendorCaseModel
    {
        public int CaseCount { get; set; }
        public Vendor Vendor { get; set; }
    }
    public class VendorIdWithCases
    {
        public int CaseCount { get; set; }
        public long VendorId { get; set; }
    }
}