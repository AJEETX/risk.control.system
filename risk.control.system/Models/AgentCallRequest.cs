using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace risk.control.system.Models
{
    public class AgentCallRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [BindNever]
        public long Id { get; set; }
        public long CaseId { get; set; }
        public DateTime StartTime { get; set; }
        public double Duration { get; set; }
        public DateTime? CallReceivedTime { get; set; }
        [NotMapped]
        public IFormFile? CallRecording { get; set; }
        [BindNever]
        public string? CallRecordingPath { get; set; }
        public string? Transcript { get; set; }

    }
}
