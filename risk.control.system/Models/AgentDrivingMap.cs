using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models;

public class AgentDrivingMap : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AgentDrivingMapId { get; set; }
    public long AgentId { get; set; }

    [Display(Name = "Distance")]
    public string? Distance { get; set; } = default!;
    public float? DistanceInMetres { get; set; } = default!;

    [Display(Name = "Duration")]
    [Required]
    public string? Duration { get; set; } = default!;
    public int? DurationInSeconds { get; set; } = default!;

    [Display(Name = "Address")]
    public string DrivingMap { get; set; }

    public string ClaimsInvestigationId { get; set; }

}