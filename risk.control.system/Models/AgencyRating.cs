using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Google.Rpc;

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

        public string UserEmail { get; set; }
        public virtual Vendor? Vendor { get; set; }
        public override string ToString()
        {
            return $"AgencyRating Information:\n" +
           $"- Rate: {Rate}\n" +
           $"- IpAddress: {IpAddress}";
        }
    }
}