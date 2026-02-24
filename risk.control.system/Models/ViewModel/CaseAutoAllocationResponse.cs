namespace risk.control.system.Models.ViewModel
{
    public class CaseAutoAllocationResponse
    {
        public long Id { get; set; }
        public bool IsNew { get; set; }
        public string Amount { get; set; }
        public string PolicyId { get; set; }
        public bool AssignedToAgency { get; set; }
        public int PincodeCode { get; set; }
        public string PincodeAddress { get; set; }
        public string Document { get; set; }
        public string Customer { get; set; }
        public string Name { get; set; }
        public string Policy { get; set; }
        public bool IsUploaded { get; set; }
        public string Origin { get; set; }
        public string SubStatus { get; set; }
        public bool Ready2Assign { get; set; }
        public string Service { get; set; }
        public string Location { get; set; }
        public string Created { get; set; }
        public string ServiceType { get; set; }
        public string TimePending { get; set; }
        public string PolicyNum { get; set; }
        public string BeneficiaryPhoto { get; set; }
        public string BeneficiaryName { get; set; }
        public double TimeElapsed { get; set; }
        public string BeneficiaryFullName { get; set; }
        public string CustomerFullName { get; set; }
        public string PersonMapAddressUrl { get; set; }
    }
}