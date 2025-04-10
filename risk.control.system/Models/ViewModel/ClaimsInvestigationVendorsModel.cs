﻿namespace risk.control.system.Models.ViewModel
{
    public class ClaimsInvestigationVendorsModel
    {
        public ClaimsInvestigation ClaimsInvestigation { get; set; }
        public BeneficiaryDetail Location { get; set; }
        public AgencyReport AgencyReport { get; set; }
        public List<VendorCaseModel> Vendors { get; set; }
        public AssessorRemarkType AssessorRemarkType { get; set; }
        public bool TrialVersion { get; set; }

        public string? ReportAiSummary { get; set; }
        public bool FromEditPage { get; set; } = false;
    }

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