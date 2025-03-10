using risk.control.system.AppConstant;

namespace risk.control.system.Models
{
    public class BaseEntity
    {
        public DateTime Created { get; set; } = DateTime.Now;
        public bool IsUpdated { get; set; } = true;
        public DateTime? Updated { get; set; }
        public string? CreatedUser { get; set; } = "--";
        public string? UpdatedBy { get; set; } = "--";
    }
}