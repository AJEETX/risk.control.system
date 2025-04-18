using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class InvestigationTimeline :BaseEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public long InvestigationTaskId { get; set; }
        public InvestigationTask InvestigationTask { get; set; }

        public string SubStatus { get; set; }
        public string Status { get; set; }

        public string AssigedTo { get; set; } // UserName or Id

        public DateTime StatusChangedAt { get; set; }

        public TimeSpan? Duration { get; set; } // Time spent in the previous status
    }
}
