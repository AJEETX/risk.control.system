namespace risk.control.system.Models.ViewModel
{
    public class CreateClaims
    {
        public bool BulkUpload { get; set; }
        public bool UserCanCreate { get; set; }
        public int UploadId { get; set; }
        public bool HasClaims { get; set; }
        public bool HasFileUploads { get; set; } = false;
        public UploadType? Uploadtype { get; set; }
        public CREATEDBY CREATEDBY { get; set; }
        public string? FileSampleIdentifier { get; set; }
    }
}
