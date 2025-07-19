namespace risk.control.system.Models.ViewModel
{
    public class UploadResult
    {
        public InvestigationTask InvestigationTask { get; set; }
        public List<UploadError> ErrorDetail { get; set; }
    }
    public class UploadError
    {
        public string UploadData { get; set; }
        public string Error { get; set; }
    }
}
