using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public class UnderwritingQuestion
    {
        public static List<Question> QuestionsUNDERWRITING_LA_ADDRESS()
        {
            var question1 = new Question
            {
                QuestionText = "Name Of Person Met (Name & Mobile No.)",
                QuestionType = "text",
                IsRequired = true
            };


            var question2 = new Question
            {
                QuestionText = "Met Person Relation With LA",
                QuestionType = "dropdown",
                Options = "BROTHER, RELATIVE, COUSIN, FRIEND, UNKNOWN, SELF",
                IsRequired = true
            };
            var question3 = new Question
            {
                QuestionText = "LA Date of Birth",
                QuestionType = "date",
                IsRequired = true
            }; ;
            var question4 = new Question
            {
                QuestionText = "Tobacco/ Alcohol or Smoking / swimming /\r\ndiving",
                QuestionType = "dropdown",
                Options = "SMOKER, OCCASIONAL DRINKER, DRUG-USER, UNKNOWN",
                IsRequired = true
            };

            var question5 = new Question
            {
                QuestionText = "In case LA not healthy –Name of disease/\r\nduration/Place of treatment (hospital and Doctor\r\nname)",
                QuestionType = "text",
            };

            var question6 = new Question
            {
                QuestionText = "History of medical investigation, surgery or\r\ntreatment in past or planned in near future (even\r\nif it was a minor or major))",
                QuestionType = "text",
            };
            var question7 = new Question
            {
                QuestionText = "Policy Details Other Than Canara HSBC and\r\ntotal life coverage",
                QuestionType = "text",
            };
            var question8 = new Question
            {
                QuestionText = "Residence Locality and type",
                QuestionType = "text",
            };

            var question9 = new Question
            {
                QuestionText = "Residence Ownership",
                QuestionType = "dropdown",
                Options = "RENTED, OWNED, UNKNOWN",
                IsRequired = true
            };
            var question10 = new Question
            {
                QuestionText = "Date since living at Current Residence",
                QuestionType = "date",
                IsRequired = true
            };
            var question11 = new Question
            {
                QuestionText = "Financial Status of Life Assured (Please mention\r\nyour observation basis life style etc.)",
                QuestionType = "dropdown",
                Options = "LOWER CLASS,LOWER- MIDDLE CLASS,MIDDLE CLASS, UPPER CLASS, UNKNOWN",
                IsRequired = true
            };
            var question12 = new Question
            {
                QuestionText = "No. Of Family Members and their details",
                QuestionType = "text",
            };
            var question13 = new Question
            {
                QuestionText = "LA’s Education Qualification",
                QuestionType = "dropdown",
                Options = "PRIMARY SCHOOLING, MATRICULATION, GRADE 12  PASS, GRADUATE, UNKNOWN",
                IsRequired = true
            };

            var question14 = new Question
            {
                QuestionText = "Employment Category (Salaried /Self employed\r\netc.)",
                QuestionType = "dropdown",
                Options = "SALARIED, SELF-EMPLOYED, CASUAL-CONTRACTOR, GOVT-EMPLOYED, UNKNOWN",
                IsRequired = true
            };
            var question15 = new Question
            {
                QuestionText = "Annual Income",
                QuestionType = "dropdown",
                Options = "Rs. 0 - 10000, Rs. 10000 - 100000, Rs. 100000 +, UNKNOWN",
                IsRequired = true
            };
            var question16 = new Question
            {
                QuestionText = "Nominee Name and relationship with LA",
                QuestionType = "text",
            };
            var question17 = new Question
            {
                QuestionText = "Vicinity Check Details – met person Name /\r\nMobile number and details confirmed",
                QuestionType = "text",
                IsRequired = true
            };
            var question18 = new Question
            {
                QuestionText = "Date and time met with Person",
                QuestionType = "date",
                IsRequired = true
            };

            return new List<Question> { question1, question2, question3, question4, question5, question6, question7, question8, question9, question10,
            question11, question12, question13, question14, question15, question16, question17, question18};
        }
    }
}