using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class MediaReport : IdReportBase
    {
        public MediaType MediaType { get; set; } = MediaType.AUDIO;
        public string MediaExtension { get; set; } = "mp3";
        // Foreign key to LocationTemplate
        public long? LocationTemplateId { get; set; }  // This is the FK property
        public LocationTemplate? LocationTemplate { get; set; }  // Navigation property

    }

    public enum MediaType
    {
        [Display(Name = "Audio")]
        AUDIO,

        [Display(Name = "Video")]
        VIDEO
    }

}