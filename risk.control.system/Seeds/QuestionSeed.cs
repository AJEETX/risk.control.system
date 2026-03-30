using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public static class QuestionSeed
    {
        public static Question Q(string text, string type, bool required = false, string options = null!)
        {
            return new Question
            {
                QuestionText = text,
                QuestionType = type,
                IsRequired = required,
                Options = options
            };
        }
    }
}
