using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class IdReportBase: BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public byte[]? IdImage { get; set; }

        public string? IdImageData { get; set; } = "No Location Info...";

        public string? IdImageLocationUrl { get; set; }

        public string? Distance { get; set; }
        public float? DistanceInMetres { get; set; }
        public string? Duration { get; set; }
        public int? DurationInSeconds { get; set; }

        public string? IdImageLocationAddress { get; set; } = "No Address data";
        public string? IdImageLongLat { get; set; }
        public DateTime? IdImageLongLatTime { get; set; }
        public bool ValidationExecuted { get; set; } = false;
    }
}