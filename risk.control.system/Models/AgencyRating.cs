using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class AgencyRating : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AgencyRatingId { get; set; }

        public int Rate { get; set; }
        public string IpAddress { get; set; }
        public long VendorId { get; set; }

        public virtual Vendor? Vendor { get; set; }
    }
}