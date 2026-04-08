using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public static class UnderwritingQuestion
    {
        public static List<Question> QuestionsUNDERWRITING_LA_ADDRESS()
        {
            var question1 = QuestionSeed.Q("Name Of Person Met (Name & Mobile No.) ?", "text", true);

            var question2 = QuestionSeed.Q("Met Person Relation With LA ?", "dropdown", true, "BROTHER, RELATIVE, COUSIN, FRIEND, UNKNOWN, SELF");
            var question3 = QuestionSeed.Q("LA Date of Birth ?", "date", true);
            var question4 = QuestionSeed.Q("Tobacco/ Alcohol or Smoking / swimming /\r\ndiving ?", "dropdown", true, "SMOKER, OCCASIONAL DRINKER, DRUG-USER, UNKNOWN");
            var question5 = QuestionSeed.Q("In case LA not healthy –Name of disease/\r\nduration/Place of treatment (hospital and Doctor\r\nname) ?", "text");
            var question6 = QuestionSeed.Q("History of medical investigation, surgery or\r\ntreatment in past or planned in near future (even\r\nif it was a minor or major) ?", "text");
            var question7 = QuestionSeed.Q("Policy Details Other Than Canara HSBC and\r\ntotal life coverage ?", "text");
            var question8 = QuestionSeed.Q("Residence Locality and type ?", "text");
            var question9 = QuestionSeed.Q("Residence Ownership ?", "dropdown", true, "RENTED, OWNED, UNKNOWN");
            var question10 = QuestionSeed.Q("Date since living at Current Residence ?", "date", true);
            var question11 = QuestionSeed.Q("Financial Status of Life Assured (Please mention\r\nyour observation basis life style etc.) ?", "dropdown", true, "LOWER CLASS,LOWER- MIDDLE CLASS,MIDDLE CLASS, UPPER CLASS, UNKNOWN");
            var question12 = QuestionSeed.Q("No. Of Family Members and their details ?", "text");
            var question13 = QuestionSeed.Q("LA’s Education Qualification ?", "dropdown", true, "PRIMARY SCHOOLING, MATRICULATION, GRADE 12  PASS, GRADUATE, UNKNOWN");
            var question14 = QuestionSeed.Q("Employment Category (Salaried /Self employed\r\netc.) ?", "dropdown", true, "SALARIED, SELF-EMPLOYED, CASUAL-CONTRACTOR, GOVT-EMPLOYED, UNKNOWN");
            var question15 = QuestionSeed.Q("Annual Income ?", "dropdown", true, "Rs. 0 - 10000, Rs. 10000 - 100000, Rs. 100000 +, UNKNOWN");
            var question16 = QuestionSeed.Q("Nominee Name and relationship with LA ?", "text");
            var question17 = QuestionSeed.Q("Vicinity Check Details – met person Name /\r\nMobile number and details confirmed ?", "text", true);
            var question18 = QuestionSeed.Q("Date and time met with Person ?", "date", true);

            return new List<Question> { question1, question2, question3, question4, question5, question6, question7, question8, question9, question10,
            question11, question12, question13, question14, question15, question16, question17, question18};
        }
    }
}