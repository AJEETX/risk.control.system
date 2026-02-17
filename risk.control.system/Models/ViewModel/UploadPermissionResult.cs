namespace risk.control.system.Models.ViewModel
{
    public class UploadPermissionResult
    {
        public bool IsManager { get; set; }
        public bool UserCanCreate { get; set; }
        public bool HasClaims { get; set; }
        public string FileSampleIdentifier { get; set; }
        public bool ShouldSendTrialNotification { get; set; }
        public LicenseStatus LicenseStatus { get; set; } // For the notification method
    }
}