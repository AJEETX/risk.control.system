using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class AgencyRating : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AgencyRatingId { get; set; }

        public int Rate { get; set; }
        public string IpAddress { get; set; } = default!;
        public long VendorId { get; set; }

        public string UserEmail { get; set; } = default!;

        public override string ToString()
        {
            return $"AgencyRating Information:\n" +
           $"- Rate: {Rate}\n" +
           $"- IpAddress: {IpAddress}";
        }
    }
}