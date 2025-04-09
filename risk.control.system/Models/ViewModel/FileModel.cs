namespace risk.control.system.Models.ViewModel
{
    public abstract class FileModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FileType { get; set; }
        public string Extension { get; set; }
        public string Description { get; set; }
        public long? CompanyId { get; set; }
        public bool? Completed { get; set; }
        public string? Icon { get; set; } = "fas fa-sync fa-spin i-blue";
        public string? Status { get; set; } = "Processing";
        public string? Message { get; set; } = "Upload In progress";
        public byte[] ByteData { get; set; }
        public int RecordCount { get; set; } = 0;
        public List<string>? ClaimsId { get; set; } = new();
        public CREATEDBY AutoOrManual { get; set; } = CREATEDBY.MANUAL;
        public ORIGIN FileOrFtp { get; set; } = ORIGIN.FILE;
        public string UploadedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? Saved { get; set; } = false;
        public int CompanySequenceNumber { get; set; } // Company-specific sequence number
        public int UserSequenceNumber { get; set; } // Company-specific sequence number
        public bool Deleted { get; set; } = false;
    }
}