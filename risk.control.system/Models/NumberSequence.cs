using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class NumberSequence : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NumberSequenceId { get; set; }

        [Required]
        public string NumberSequenceName { get; set; } = default!;

        [Required]
        public string Module { get; set; } = default!;

        [Required]
        public string Prefix { get; set; } = default!;

        public int LastNumber { get; set; }
    }
}
