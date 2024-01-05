using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class PreviousClaimReport : ClaimReportBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string PreviousClaimReportId { get; set; } = Guid.NewGuid().ToString();
    }
}