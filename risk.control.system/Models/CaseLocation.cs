using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class CaseLocation : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CaseLocationId { get; set; } 
        [Display(Name = "State")]
        public string? StateId { get; set; } = default!;
        public State? State { get; set; } = default!;

        [Display(Name = "District")]
        public string? DistrictId { get; set; } = default!;
        public District? District { get; set; } = default!;
 
        [NotMapped]
        [Display(Name = "Choose Multiple Pincodes")]
        public List<string> SelectedMultiPincodeId { get; set; } = new List<string> { }!;
        [Display(Name = "verify pincodes")]
        public List<VerifyPinCode> PincodeServices { get; set; } = default!;

        public string ClaimsInvestigationId { get; set; }
        public ClaimsInvestigation ClaimsInvestigation { get; set; } = default!;

    }
}
