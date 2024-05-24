using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class GlobalSettings : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long GlobalSettingsId { get; set; }

        public long? ClientCompanyId { get; set; }
        public ClientCompany ClientCompany { get; set; }
        public long? VendorId { get; set; }
        public Vendor? Vendor { get; set; }
        public bool ShowHeaderAsImage { get; set; } = false;

    }
}
