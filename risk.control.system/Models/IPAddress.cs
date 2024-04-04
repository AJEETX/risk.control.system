using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using CsvHelper.Configuration.Attributes;

namespace risk.control.system.Models
{
    public class IpAddress : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long IpAddressId { get; set; }
        [Required]
        [Display(Name = "IP Address")]
        public string Address { get; set; } = default!;
       
    }
}
