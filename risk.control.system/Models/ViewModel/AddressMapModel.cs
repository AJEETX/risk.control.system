namespace risk.control.system.Models.ViewModel
{
    public class AddressMapModel
    {
        public string StartLatitude { get; set; }
        public string StartLongitude { get; set; }
        public string EndLatitude { get; set; }
        public string EndLongitude { get; set; }
        public string? StartLabel { get; set; } = "S";
        public string? EndLabel { get; set; } = "E";
        public string? MapHeight { get; set; } = "300";
        public string? MapWidth { get; set; } = "300";
        public string? StartColor { get; set; } = "red";
        public string? EndColor { get; set; } = "green";
    }
}
