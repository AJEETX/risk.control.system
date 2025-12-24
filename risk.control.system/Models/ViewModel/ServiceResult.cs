namespace risk.control.system.Models.ViewModel
{
    public sealed class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Dictionary<string, string> Errors { get; } = new();
    }
}
