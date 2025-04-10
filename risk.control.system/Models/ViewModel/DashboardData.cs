namespace risk.control.system.Models.ViewModel
{
    public class DashboardData
    {
        public bool AutoAllocation { get; set; } = false;
        public bool BulkUpload { get; set; } = false;
        public string FirstBlockName { get; set; }
        public int FirstBlockCount { get; set; }
        public string FirstBlock2Count { get; set; }
        public string FirstBlockUrl { get; set; } = string.Empty;

        public string SecondBlockName { get; set; }
        public int SecondBlockCount { get; set; }
        public string SecondBlock2Count { get; set; }
        public string SecondBlockUrl { get; set; } = string.Empty;
        public string SecondBBlockName { get; set; }
        public int SecondBBlockCount { get; set; }
        public string SecondBBlockCountBoth { get; set; }
        public string SecondBBlockUrl { get; set; } = string.Empty;
        public string ThirdBlockName { get; set; }
        public int ThirdBlockCount { get; set; }
        public string ThirdBlockUrl { get; set; } = string.Empty;

        public string LastBlockName { get; set; }
        public int LastBlockCount { get; set; }
        public string LastBlockUrl { get; set; } = string.Empty;

        public string BulkUploadBlockName { get; set; }
        public int BulkUploadBlockCount { get; set; }
        public int BulkUploadAssignCount { get; set; }
        public string BulkUploadBlockUrl { get; set; } = string.Empty;

        public string? FifthBlockName { get; set; }
        public int? FifthBlockCount { get; set; }
        public string? FifthBlockUrl { get; set; } = string.Empty;

        public string? SixthBlockName { get; set; }
        public int? SixthBlockCount { get; set; }
        public string? SixthBlockUrl { get; set; } = string.Empty;

        public int? UnderwritingCount { get; set; }
        public int? ApprovedUnderwritingCount { get; set; }
        public int? ApprovedClaimgCount { get; set; }
        public int? RejectedUnderwritingCount { get; set; }
        public int? RejectedClaimCount { get; set; }

    }
}
