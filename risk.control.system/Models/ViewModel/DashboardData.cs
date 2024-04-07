namespace risk.control.system.Models.ViewModel
{
    public class DashboardData
    {
        public string FirstBlockName { get; set; }
        public int FirstBlockCount { get; set; }
        public string FirstBlockUrl { get; set; } = string.Empty;

        public string SecondBlockName { get; set; }
        public int SecondBlockCount { get; set; }
        public string SecondBlockUrl { get; set; } = string.Empty;
        public string ThirdBlockName { get; set; }
        public int ThirdBlockCount { get; set; }
        public string ThirdBlockUrl { get; set; } = string.Empty;

        public string LastBlockName { get; set; }
        public int LastBlockCount { get; set; }
        public string LastBlockUrl { get; set; } = string.Empty;
    }
}