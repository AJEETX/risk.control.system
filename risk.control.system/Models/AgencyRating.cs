using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class AgencyRating
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string AgencyRatingId { get; set; } = Guid.NewGuid().ToString();

        public int Rate { get; set; }
        public string IpAddress { get; set; }
        public string VendorId { get; set; }

        public virtual Vendor? Vendor { get; set; }
    }
}