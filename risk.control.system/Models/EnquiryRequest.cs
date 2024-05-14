using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class EnquiryRequest : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QueryRequestId { get; set; }
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public byte[]? QuestionAttachment { get; set; }
        public string? QuestionFileName { get; set; }
        public string? QuestionFileType { get; set; }
        public string? QuestionFileExtension { get; set; }
        public string? Answer { get; set; }
        public string? AnswerA { get; set; }
        public string? AnswerB { get; set; }
        public string? AnswerC { get; set; }
        public string? AnswerD { get; set; }
        public string? AnswerSelected { get; set; }
        public byte[]? AnswerAttachment { get; set; }
        public string? AnswerFileName { get; set; }
        public string? AnswerFileType { get; set; }
        public string? AnswerFileExtension { get; set; }
        public long? AgencyReportId { get; set; }
        public AgencyReport? AgencyReport { get; set; }
        public string? RequesterEmail { get; set; }
    }

}
