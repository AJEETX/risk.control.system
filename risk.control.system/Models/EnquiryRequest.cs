using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class EnquiryRequest : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QueryRequestId { get; set; }
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public byte[]? QuestionImageAttachment { get; set; }
        public string? QuestionImageFileName { get; set; }
        public string? QuestionImageFileType { get; set; }
        public string? QuestionImageFileExtension { get; set; }
        public string? Answer { get; set; }
        public string? AnswerA { get; set; }
        public string? AnswerB { get; set; }
        public string? AnswerC { get; set; }
        public string? AnswerD { get; set; }
        public string? AnswerSelected { get; set; }
        public byte[]? AnswerImageAttachment { get; set; }
        public string? AnswerImageFileName { get; set; }
        public string? AnswerImageFileType { get; set; }
        public string? AnswerImageFileExtension { get; set; }
    }
}
