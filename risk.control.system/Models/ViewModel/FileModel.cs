using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models.ViewModel
{
    public class FileModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FileType { get; set; }
        public string Extension { get; set; }
        public string Description { get; set; }
        public long? CompanyId { get; set; }
        public bool? Completed { get; set; }
        public string? Icon { get; set; } = "fas fa-sync fa-spin i-grey";
        public string? Status { get; set; } = "Processing";
        public string? Message { get; set; } = "Upload In progress";
        public byte[]? ErrorByteData { get; set; }
        public int RecordCount { get; set; } = 0;
        public List<CaseListModel>? CaseIds { get; set; } = new();
        public CREATEDBY AutoOrManual { get; set; } = CREATEDBY.MANUAL;
        public ORIGIN FileOrFtp { get; set; } = ORIGIN.FILE;
        public string UploadedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
        public int CompanySequenceNumber { get; set; } // Company-specific sequence number
        public int UserSequenceNumber { get; set; } // Company-specific sequence number
        public bool Deleted { get; set; } = false;
        public bool DirectAssign { get; set; } = false;
    }
    public class CaseListModel
    {
        [Key] // Add this
        public int Id { get; set; }

        public long CaseId { get; set; }

        // Foreign key back to the file
        public int FileModelId { get; set; }
    }
}
