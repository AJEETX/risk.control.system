using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class BsbInfo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string BSB { get; set; } = "";
        public string Bank { get; set; } = "";
        public string BankCode { get; set; } = "";
        public string Branch { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string Postcode { get; set; } = "";
    }

    public class BsbLookUp
    {
        public string BSBOwner { get; set; }
        public string OwnerName { get; set; }
        public string BSBPrefix { get; set; }
    }
}
