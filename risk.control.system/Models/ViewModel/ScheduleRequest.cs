using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models.ViewModel
{
    public class ScheduleRequest
    {
        public long CaseId { get; set; }
        public DateTime ScheduleDateTime { get; set; }
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
    }
}
