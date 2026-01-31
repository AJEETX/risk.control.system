namespace risk.control.system.Models.ViewModel
{
    public class AllocateVendorAgentResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? VendorAgentEmail { get; set; }
        public long VendorId { get; set; }
        public string? ContractNumber { get; set; }
    }
}