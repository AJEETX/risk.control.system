using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public class ClaimQuestion
    {
        public static List<Question> QuestionsCLAIM_LA_ADDRESS()
        {
            return new List<Question>
            {
                new Question
            {
                QuestionText = "Injury/Illness prior to commencement/revival ?",
                QuestionType = "dropdown",
                Options = "YES, NO",
                IsRequired = true
            },
                new Question
            {
                QuestionText = "Duration of treatment ?",
                QuestionType = "dropdown",
                Options = "0 , Less Than 6 months, More Than 6 months",
                IsRequired = true
            },
                new Question
            {
                QuestionText = "Name of person met LA Address",
                QuestionType = "text",
                IsRequired = true
            },
                new Question
            {
                QuestionText = "Date and time of meeting with LA address",
                QuestionType = "date",
                IsRequired = true
            }
            };
        }
        public static List<Question> QuestionsCLAIM_BUSINESS_ADDRESS()
        {
            return new List<Question>
             {
                new Question
                {
                    QuestionText = "How long was Deceased person in the\r\nbusiness? ?",
                    QuestionType = "dropdown",
                    Options = "0 , Less Than 6 months, More Than 6 months",
                    IsRequired = true
                },
                    new Question
                {
                    QuestionText = "Nature of Insured’s business at the time\r\nof proposal?",
                    QuestionType = "text",
                    IsRequired = true
                },

                new Question
                {
                    QuestionText = "Address of business premises",
                    QuestionType = "text",
                    IsRequired = true
                }
                ,
                new Question
                {
                    QuestionText = "Name of the Business Associate met",
                    QuestionType = "text",
                    IsRequired = true
                },
                new Question
                {
                    QuestionText = "Date and time of meeting with Business Associate ",
                    QuestionType = "date",
                    IsRequired = true
                }
            };
        }
        public static List<Question> QuestionsCLAIM_CHEMIST_ADDRESS()
        {
            return new List<Question>
            {
                new Question
            {
                QuestionText = "Name ?",
                QuestionType = "text",
                IsRequired = true
            },
                new Question
            {
                QuestionText = "Address",
                QuestionType = "text",
                IsRequired = true
            },
                new Question
            {
                QuestionText = "Contact Number",
                QuestionType = "text",
                IsRequired = true
            }
            };
        }
        public static List<Question> QuestionsCLAIM_EMPLOYMENT_ADDRESS()
        {
            return new List<Question>
            {
                new Question
            {
                QuestionText = "Name of the company ?",
                QuestionType = "text",
                IsRequired = true
            },
                new Question
            {
                QuestionText = "Address",
                QuestionType = "text",
                IsRequired = true
            },
                new Question
            {
                QuestionText = "Contact Number",
                QuestionType = "text",
                IsRequired = true
            }
                ,
                new Question
            {
                QuestionText = "whether LA suffered from\r\n&ltany ailment&gt; and were they\r\naware as to where he/ she\r\nwas being treated?",
                QuestionType = "checkbox",
                Options = "YES",

            }   ,
                new Question
            {
                QuestionText = "Date of joining service",
                QuestionType = "date",
            } ,
                new Question
            {
                QuestionText = "Nature of duties",
                QuestionType = "text",
            }
            };
        }
        public static List<Question> QuestionsCLAIM_CEMETERY_ADDRESS()
        {
            return new List<Question>
            {
                new Question
            {
                QuestionText = "Name of the Person met at the cemetery ?",
                QuestionType = "text",
                IsRequired = true
            },
                new Question
            {
                QuestionText = "Designation of the Person met at the cemetery",
                QuestionType = "text",
                IsRequired = true
            },
                 new Question
            {
                QuestionText = "Contact of the Person met at the cemetery",
                QuestionType = "text",
                IsRequired = true
            }
                ,
                new Question
            {
                QuestionText = "whether Life Assured Cremated / Buried?",
                QuestionType = "radio",
                Options = "Cremated, Buried",
                }
            };
        }
        public static List<Question> QuestionsCLAIM_POLIC_STATION()
        {
            return new List<Question>
                        {
                            new Question
                            {
                                QuestionText = "Name of the police station and\r\nAddress?",
                                QuestionType = "text",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Name of the Sub Inspector–in-charge\r\nof the case",
                                QuestionType = "text",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Date/time when the body sent for PM)?",
                                QuestionType = "date",
                                IsRequired = true
                            }
                        };
        }
        public static List<Question> QuestionsCLAIM_HOSPITAL()
        {
            return new List<Question>
                        {
                new Question
                            {
                                QuestionText = "Establish the name and address of the\r\ninsured’s usual medical attendant / family\r\ndoctor during the past 3 years. If more than\r\none, mention all. ?",
                                QuestionType = "text",
                            },
                new Question
                            {
                                QuestionText = "Did the deceased suffer from any illness\r\nor injury prior to the commencement /revival\r\nof the policy? If so give full particulars and\r\nname(s) of doctors consulted. ?",
                                QuestionType = "text",
                            },
                            new Question
                            {
                                QuestionText = "For his/ her last illness (mention the\r\nperiod of hospitalization, name(s) of the doctors attended and IP/OP No.) ?",
                                QuestionType = "text",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "When was the disease of which the\r\ninsured person died, first suspected or\r\ndiagnosed.(Collect Initial and Follow up\r\nconsultation notes) ?",
                                QuestionType = "text",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Date on which the last attending doctor\r\nwas first consulted. (During the last illness\r\nand or before that) ?",
                                QuestionType = "date",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Duration of last illness ?",
                                QuestionType = "dropdown",
                                Options = "LESS THAN 1 WEEK, LESS THAN 1 MONTH, LESS THAN 3 MONTH, LESS THAN 6 MONTH, MORE THAN 6 MONTH",
                                IsRequired = true
                            },
                             new Question
                            {
                                QuestionText = "Medical Cause of Death ?",
                                QuestionType = "text",
                                IsRequired = true
                            },
                              new Question
                            {
                                QuestionText = "Whether any doctor has treated the\r\ninsured for the same or any other ailment at\r\nany time before the commencement / revival\r\nof the policy and if so, for what ailment and\r\nfor how long? ?",
                                QuestionType = "text",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Name of Medical staff met ?",
                                QuestionType = "text",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Date time of Medical staff  met ?",
                                QuestionType = "date",
                                IsRequired = true
                            }
                        };
        }
    }
}