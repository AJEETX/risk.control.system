using risk.control.system.AppConstant;

namespace risk.control.system.Models
{
    public class BaseEntity
    {
        public DateTime Created { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Applicationsettings.INDIAN_TIME_ZONE);
        public DateTime? Updated { get; set; }
        public string? UpdatedBy { get; set; } = default!;
    }
}