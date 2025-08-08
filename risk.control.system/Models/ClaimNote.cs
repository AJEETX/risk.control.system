//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace risk.control.system.Models
//{
//    public class ClaimNote : BaseEntity
//    {
//        [Key]
//        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
//        public long ClaimNoteId { get; set; }

//        public string Sender { get; set; }
//        public string Comment { get; set; }
//        public ClaimNote? ParentClaimNote { get; set; }
//        public override string ToString()
//        {
//            return $"ClaimNote Information:\n" +
//            $"- Sender: {Sender}\n" +
//            $"- Comment: {Comment}";
//        }
//    }
//}