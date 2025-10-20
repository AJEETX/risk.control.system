using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class Question : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string QuestionText { get; set; }
        public string? AnswerText { get; set; }
        public string? QuestionType { get; set; } // Stores type: "text", "dropdown", "checkbox", "date", "file", "radio"
        public string? Options { get; set; } // Stores comma-separated values for dropdown/radio
        public bool IsRequired { get; set; } // New property to mark if the question is required or optional
        public long? ReportTemplateId { get; set; }
        public ReportTemplate? ReportTemplate { get; set; }
        public override string ToString()
        {
            return $"Question Detail:\n" +
                $"Question: {QuestionText}\n" +
                $"Question Type: {QuestionType}\n";
        }
    }
    public class QuestionFormViewModel
    {
        public InsuranceType InsuranceType { get; set; }
        public List<Question>? Questions { get; set; } = new List<Question>();
        public string? QuestionText { get; set; }
        public string? QuestionType { get; set; }
        public string? Options { get; set; }
        public bool IsRequired { get; set; }
        public Dictionary<int, string>? Answers { get; set; } = new Dictionary<int, string>();
    }
}
