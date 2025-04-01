namespace risk.control.system.Models.ViewModel
{
    public class CreateClaims
    {
        public bool BulkUpload { get; set; }
        public bool UserCanCreate { get; set; }
        public bool Refresh { get; set; }
        public bool HasClaims { get; set; }
        public UploadType? Uploadtype { get; set; }
        public CREATEDBY CREATEDBY { get; set; }
        public string? FileSampleIdentifier { get; set; }
    }
}
