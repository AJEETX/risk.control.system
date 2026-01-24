using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class ReportBase : BaseEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public bool Selected { get; set; } = false;
        public string? ReportName { get; set; }
        public string? FilePath { get; set; }
        public string? ImageExtension { get; set; }
        public string? LocationInfo { get; set; } = "No Location Info...";
        public string? LocationMapUrl { get; set; }
        public string? Distance { get; set; }
        public float? DistanceInMetres { get; set; }
        public string? Duration { get; set; }
        public int? DurationInSeconds { get; set; }
        public string? LocationAddress { get; set; } = "No Address data";
        public string? LongLat { get; set; }
        public DateTime? LongLatTime { get; set; }
        public bool ValidationExecuted { get; set; } = false;
        public bool? ImageValid { get; set; } = false;
        public bool IsRequired { get; set; } = false;
        public override string ToString()
        {
            return $"Report Information:\n" +
                $"- Name: {ReportName}\n" +
                $"- Location Info: {LocationInfo}\n" +
                $"- Location Address: {LocationAddress}\n" +
                $"- Valid: {ImageValid}";
        }
    }
}