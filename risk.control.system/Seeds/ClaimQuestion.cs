using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public static class ClaimQuestion
    {
        public static List<Question> QuestionsCLAIM_LA_ADDRESS()
        {
            return new List<Question>
            {
                QuestionSeed.Q("Did Life Assured (LA) had any Injury/Illness prior to commencement/revival ?", "dropdown",true, "YES, NO, UNKNOWN"),
                QuestionSeed.Q( "Duration of treatment ?", "dropdown", true, "NONE , Less Than 6 months, More Than 6 months, UNKNOWN"),
                QuestionSeed.Q( "Name of person met LA Address ?","text", true),
                QuestionSeed.Q("Any Other findings", "text")
            };
        }
        public static List<Question> QuestionsCLAIM_BUSINESS_ADDRESS()
        {
            return new List<Question>
             {
                QuestionSeed.Q("How long was Deceased person in the business ?","dropdown", true, "NONE , Less Than 6 months, More Than 6 months, UNKNOWN"),
                    QuestionSeed.Q("Nature of LA's business at the time of proposal ?", "text", true),
                    QuestionSeed.Q("Address of business premises ?", "text",true),
                QuestionSeed.Q("Name of the Business Associate met ?", "text",true),
                QuestionSeed.Q("Any Other findings", "text")
            };
        }
        public static List<Question> QuestionsCLAIM_CHEMIST_ADDRESS()
        {
            return new List<Question>
            {
                QuestionSeed.Q("Chemist Name ?", "text", true),
                QuestionSeed.Q("Chemist Address ?", "text", true),
                QuestionSeed.Q("Contact Number ?", "text", true),
                QuestionSeed.Q("Any Other findings", "text")
            };
        }
        public static List<Question> QuestionsCLAIM_EMPLOYMENT_ADDRESS()
        {
            return new List<Question>
            {
                QuestionSeed.Q("Name of the company ?", "text", true),
                QuestionSeed.Q("Company Address ?",  "text", true),
                QuestionSeed.Q("Contact Number ?", "text", true),
                QuestionSeed.Q("Whether LA suffered from  any ailment and were they aware as to where LA was being treated ?", "checkbox",false, "YES"),
                QuestionSeed.Q("Date of joining employment (service) ?", "date"),
                QuestionSeed.Q("LA's employment nature of duties (role and responsibilites) ?", "text"),
                QuestionSeed.Q("Any Other findings", "text")
            };
        }
        public static List<Question> QuestionsCLAIM_CEMETERY_ADDRESS()
        {
            return new List<Question>
            {
                QuestionSeed.Q("Name of the Person met at the cemetery ?", "text", true),
                QuestionSeed.Q("Designation of the Person met at the cemetery ?", "text", true),
                 QuestionSeed.Q("Contact Number of the Person met at the cemetery ?", "text", true),
                QuestionSeed.Q("Life Assured Cremated / Buried ?", "radio", false, "Cremated, Buried"),
                QuestionSeed.Q("Any Other findings", "text")
            };
        }
        public static List<Question> QuestionsCLAIM_POLIC_STATION()
        {
            return new List<Question>
            {
                QuestionSeed.Q("Name of the police station and Address ?", "text", true),
                QuestionSeed.Q("Name of the Sub Inspector–in-charge of the case ?", "text", true),
                QuestionSeed.Q("Date/time when the body sent for Autopsy ?", "date", true),
                QuestionSeed.Q("Any Other findings", "text")
            };
        }
        public static List<Question> QuestionsCLAIM_HOSPITAL()
        {
            return new List<Question>
            {
            QuestionSeed.Q("Name and address of the LA’s usual medical attendant/family doctor during the past 3 years. If more than one, mention all. ?","text"),
            QuestionSeed.Q("Did LA (Deceased) suffer from any illness or injury prior to the commencement revival of the policy ?", "checkbox", false, "YES"),
            QuestionSeed.Q("Full particulars and name(s) of doctors consulted.?", "text"),
            QuestionSeed.Q("LA last illness (mention the period of hospitalization, name(s) of the doctors attended and IP/OP No.?", "text", true),
            QuestionSeed.Q("When was the disease of which the LA died, first suspected or diagnosed ?", "date", true),
            QuestionSeed.Q("Date on which the last attending doctor was first consulted. (During the last illness and or before that) ?", "date", true),
            QuestionSeed.Q("Duration of last illness ?", "dropdown",true,"LESS THAN 1 WEEK, LESS THAN 1 MONTH, LESS THAN 3 MONTH, LESS THAN 6 MONTH, MORE THAN 6 MONTH, UNKNOWN"),
            QuestionSeed.Q("Medical Cause of Death ?", "dropdown",true, "NATURAL DEATH, ACCIDENT/SUDDEN DEATH, UNKNOWN"),
            QuestionSeed.Q("Did any doctor treat the LA for the same or any other ailment at any time before the commencement  revival of the policy ?", "checkbox", false, "YES"),
            QuestionSeed.Q("If YES, what was ailment ?", "text"),
            QuestionSeed.Q("If YES, for how long ?", "dropdown", false, "LESS THAN 1 WEEK, LESS THAN 1 MONTH, LESS THAN 3 MONTH, LESS THAN 6 MONTH, MORE THAN 6 MONTH, UNKNOWN"),
            QuestionSeed.Q("Name of Medical staff met ?", "text", true),
            QuestionSeed.Q("Any Other findings", "text")
            };
        }
    }
}