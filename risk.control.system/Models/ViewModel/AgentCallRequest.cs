using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models.ViewModel
{
    public class AgentCallRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long CaseId { get; set; }
        public DateTime StartTime { get; set; }
        public double Duration { get; set; }
        public DateTime? CallReceivedTime { get; set; }
        [NotMapped]
        public IFormFile? CallRecording { get; set; }
        public string? CallRecordingPath { get; set; }
        public string? Transcript { get; set; }

    }
}
