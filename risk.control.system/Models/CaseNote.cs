using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class CaseNote : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string SenderEmail { get; set; } = default!;

        [StringLength(50, MinimumLength = 3, ErrorMessage = "Comment must be between 3 and 50 characters.")]
        public string Comment { get; set; } = default!;

        public override string ToString()
        {
            return $"CaseNote Information:\n" +
            $"- Sender: {SenderEmail}\n" +
            $"- Comment: {Comment}";
        }
    }
}