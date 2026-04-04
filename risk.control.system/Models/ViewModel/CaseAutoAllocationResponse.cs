namespace risk.control.system.Models.ViewModel
{
    public class CaseAutoAllocationResponse
    {
        public long Id { get; set; }
        public bool IsNew { get; set; }
        public string Amount { get; set; } = default!;
        public string PolicyId { get; set; } = default!;
        public bool AssignedToAgency { get; set; }
        public int PincodeCode { get; set; }
        public string PincodeAddress { get; set; } = default!;
        public string Document { get; set; } = default!;
        public string Customer { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Policy { get; set; } = default!;
        public string Origin { get; set; } = default!;
        public string SubStatus { get; set; } = default!;
        public bool Ready2Assign { get; set; }
        public string Service { get; set; } = default!;
        public DateTime Created { get; set; } = default!;
        public string ServiceType { get; set; } = default!;
        public string TimePending { get; set; } = default!;
        public string PolicyNum { get; set; } = default!;
        public string BeneficiaryPhoto { get; set; } = default!;
        public string BeneficiaryName { get; set; } = default!;
        public double TimeElapsed { get; set; }
        public string BeneficiaryFullName { get; set; } = default!;
        public string CustomerFullName { get; set; } = default!;
        public string PersonMapAddressUrl { get; set; } = default!;
    }
}