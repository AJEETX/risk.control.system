namespace risk.control.system.Models.ViewModel
{
    public class CreateClaims
    {
        public bool UploadAndAssign { get; set; } = false;// Checkbox value
        public bool UserCanCreate { get; set; }
        public bool HasClaims { get; set; }
        public bool IsManager { get; set; }
        public bool HasFileUploads { get; set; } = false;
        public CREATEDBY CREATEDBY { get; set; }
        public string? FileSampleIdentifier { get; set; }
    }
}