using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class EnquiryRequest : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QueryRequestId { get; set; }
        public string? MultipleQuestionText { get; set; }
        public string? DescriptiveQuestion { get; set; }
        public byte[]? QuestionImageAttachment { get; set; }
        public string? QuestionImageFileName { get; set; }
        public string? QuestionImageFileType { get; set; }
        public string? QuestionImageFileExtension { get; set; }
        public string? DescriptiveAnswer { get; set; }
        public string? AnswerA { get; set; }
        public string? AnswerB { get; set; }
        public string? AnswerC { get; set; }
        public string? AnswerD { get; set; }
        public string? AnswerSelected { get; set; }
        public byte[]? AnswerImageAttachment { get; set; }
        public string? AnswerImageFileName { get; set; }
        public string? AnswerImageFileType { get; set; }
        public string? AnswerImageFileExtension { get; set; }
        public override string ToString()
        {
            return $"EnquiryRequest: " +
                   $"MultipleQuestionText={MultipleQuestionText}, " +
                   $"DescriptiveQuestion={DescriptiveQuestion}, " +
                   $"QuestionImageAttachment={(QuestionImageAttachment != null ? $"[{QuestionImageAttachment.Length} bytes]" : "null")}, " +
                   $"QuestionImageFileName={QuestionImageFileName}, " +
                   $"QuestionImageFileType={QuestionImageFileType}, " +
                   $"QuestionImageFileExtension={QuestionImageFileExtension}, " +
                   $"DescriptiveAnswer={DescriptiveAnswer}, " +
                   $"AnswerA={AnswerA}, " +
                   $"AnswerB={AnswerB}, " +
                   $"AnswerC={AnswerC}, " +
                   $"AnswerD={AnswerD}, " +
                   $"AnswerSelected={AnswerSelected}, " +
                   $"AnswerImageAttachment={(AnswerImageAttachment != null ? $"[{AnswerImageAttachment.Length} bytes]" : "null")}, " +
                   $"AnswerImageFileName={AnswerImageFileName}, " +
                   $"AnswerImageFileType={AnswerImageFileType}, " +
                   $"AnswerImageFileExtension={AnswerImageFileExtension}";
        }

    }
}
