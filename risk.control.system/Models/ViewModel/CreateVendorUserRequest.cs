namespace risk.control.system.Models.ViewModel
{
    public class CreateVendorUserRequest
    {
        public VendorApplicationUser User { get; set; }
        public string EmailSuffix { get; set; }
        public string CreatedBy { get; set; }
    }
    public class EditVendorUserRequest
    {
        public string UserId { get; set; }
        public VendorApplicationUser Model { get; set; }
        public string UpdatedBy { get; set; }
    }

}
