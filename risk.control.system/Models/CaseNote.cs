using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class CaseNote : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string SenderEmail { get; set; }
        public string Comment { get; set; }
        public CaseNote? ParentCaseNote { get; set; }
        public override string ToString()
        {
            return $"CaseNote Information:\n" +
            $"- Sender: {SenderEmail}\n" +
            $"- Comment: {Comment}";
        }
    }
}