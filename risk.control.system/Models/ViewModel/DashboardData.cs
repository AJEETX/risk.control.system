namespace risk.control.system.Models.ViewModel
{
    public class DashboardData
    {
        public bool AutoAllocation { get; set; } = false;
        public bool BulkUpload { get; set; } = false;
        public string FirstBlockName { get; set; }
        public int FirstBlockCount { get; set; }
        public string FirstBlockUrl { get; set; } = string.Empty;

        public string SecondBlockName { get; set; }
        public int SecondBlockCount { get; set; }
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
        public string BulkUploadBlockUrl { get; set; } = string.Empty;
    }
}