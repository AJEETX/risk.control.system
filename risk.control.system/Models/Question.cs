using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string QuestionText { get; set; }

        [Required]
        public string QuestionType { get; set; } // Stores type: "text", "dropdown", "checkbox", "date", "file", "radio"

        public string? Options { get; set; } // Stores comma-separated values for dropdown/radio

        public bool IsRequired { get; set; } // New property to mark if the question is required or optional
    }
    public class QuestionFormViewModel
    {
        public List<Question> Questions { get; set; } = new List<Question>();

        // Fields for adding a new question
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public string? Options { get; set; }
        public bool IsRequired { get; set; }

        // Dictionary to hold answers with question ID as key
        public Dictionary<int, string> Answers { get; set; } = new Dictionary<int, string>();
    }

}
