namespace risk.control.system.Models.ViewModel
{
    public class ActiveCaseResponse
    {
        public long Id { get; set; }
        public string PolicyNum { get; set; }
        public bool AutoAllocated { get; set; }
        public bool AssignedToAgency { get; set; }
        public bool IsNew { get; set; }
        public string PolicyId { get; set; }
        public string Policy { get; set; }
        public string Agent { get; set; }
        public string OwnerDetail { get; set; }
        public string CaseWithPerson { get; set; }
        public string Pincode { get; set; }
        public string PincodeName { get; set; }
        public string Amount { get; set; }
        public string CustomerFullName { get; set; }
        public string BeneficiaryFullName { get; set; }
        public string Name { get; set; } // HTML string for UI
        public string BeneficiaryName { get; set; } // HTML string for UI
        public string Document { get; set; }
        public string Customer { get; set; }
        public string BeneficiaryPhoto { get; set; }
        public string Service { get; set; }
        public string ServiceType { get; set; }
        public string Status { get; set; }
        public string SubStatus { get; set; }
        public string Location { get; set; }
        public string Created { get; set; }
        public string TimePending { get; set; }
        public double TimeElapsed { get; set; }
        public string PersonMapAddressUrl { get; set; }
    }
}
