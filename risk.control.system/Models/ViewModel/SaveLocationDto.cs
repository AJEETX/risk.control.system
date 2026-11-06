namespace risk.control.system.Models.ViewModel
{
    public class SaveLocationDto
    {
        public long TemplateId { get; set; }
        public long LocationId { get; set; }
        public string? LocationName { get; set; } // ✅ added
        public List<FaceDto>? FaceIds { get; set; } = new();
        public List<DocumentDto>? DocumentIds { get; set; } = new();
        public List<MediaDto>? MediaReports { get; set; } = new();
    }

    public class FaceDto
    {
        public long Id { get; set; }
        public bool Selected { get; set; }
        public bool Required { get; set; }
        public CameraCaptureMode CameraCaptureMode { get; set; }
    }
    public class DocumentDto
    {
        public long Id { get; set; }
        public bool Selected { get; set; }
        public bool Required { get; set; }
        public CameraCaptureMode CameraCaptureMode { get; set; }
        public PageCount PageCount { get; set; }
    }
    public class MediaDto
    {
        public long Id { get; set; }
        public bool Selected { get; set; }
        public bool Required { get; set; }
    }
}