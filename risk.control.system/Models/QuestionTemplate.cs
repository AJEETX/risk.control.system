namespace risk.control.system.Models
{
    public class QuestionTemplate
    {
        public string? QuestionText { get; set; }
        public string? QuestionType { get; set; } // "Text", "Radio", "Checkbox"
        public string? Options { get; set; } // comma-separated
        public bool IsRequired { get; set; } = false;
        public string? AnswerText { get; set; } // <== This will bind input value
    }
}
