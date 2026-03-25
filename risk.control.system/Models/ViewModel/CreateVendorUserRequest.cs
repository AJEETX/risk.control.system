namespace risk.control.system.Models.ViewModel
{
    public class CreateVendorUserRequest
    {
        public ApplicationUser User { get; set; } = default!;
        public string EmailSuffix { get; set; } = default!;
        public string CreatedBy { get; set; } = default!;
    }
    public class EditVendorUserRequest
    {
        public string UserId { get; set; } = default!;
        public ApplicationUser Model { get; set; } = default!;
        public string UpdatedBy { get; set; } = default!;
    }

}
