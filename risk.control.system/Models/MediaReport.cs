using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class MediaReport : ReportBase
    {
        public MediaType MediaType { get; set; } = MediaType.AUDIO;
        public string MediaExtension { get; set; } = "mp3";

        public string Transcript { get; set; } = "None";
        // Foreign key to LocationTemplate
        public long? LocationReportId { get; set; }  // This is the FK property
        public LocationReport? LocationReport { get; set; }  // Navigation property
        public override string ToString()
        {
            return $"Document Report:\n" +
                $"Report Type:{MediaExtension}";
        }
    }

    public enum MediaType
    {
        [Display(Name = "Audio")]
        AUDIO,

        [Display(Name = "Video")]
        VIDEO
    }

}