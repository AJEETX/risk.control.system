using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class BsbInfo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string BSB { get; set; } = default!;
        public string Bank { get; set; } = default!;
        public string BankCode { get; set; } = default!;
        public string Branch { get; set; } = default!;
        public string Address { get; set; } = default!;
        public string City { get; set; } = default!;
        public string State { get; set; } = default!;
        public string Postcode { get; set; } = default!;
    }

    public class BsbLookUp
    {
        public string BSBOwner { get; set; } = default!;
        public string OwnerName { get; set; } = default!;
        public string BSBPrefix { get; set; } = default!;
    }
}
