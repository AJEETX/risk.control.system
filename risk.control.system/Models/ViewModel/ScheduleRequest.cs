using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models.ViewModel
{
    public class ScheduleRequest
    {
        public long CaseId { get; set; }
        public DateTime ScheduleDateTime { get; set; }
        public string? LocationLatLong { get; set; }
    }
    public class ScheduleResponse
    {
        public bool IsClaim { get; set; }
        public DateTime ScheduleRequestedDateTime { get; set; }
        public DateTime ScheduledDateTime { get; set; }
        public string ScheduleeName { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public string PolicyNumber { get; set; } = string.Empty;
        public long ScheduleeId { get; set; }
        public long AgentId { get; set; }
        public string InsurerName { get; set; } = default!;
        public bool ScheduleRequested { get; set; } = false;
        public bool ScheduleAccepted { get; set; } = false;
        public string? LocationMapUrl { get; set; }
    }
    public class ScheduleMeeting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long CaseId { get; set; }
        public DateTime ScheduleRequestedDateTime { get; set; }
        public DateTime ScheduleDateTime { get; set; }
        public bool ScheduleRequested { get; set; } = false;
        public bool ScheduleAccepted { get; set; } = false;
        public string? LocationLatLong { get; set; }
        public string? LocationMapUrl { get; set; }
    }
}
