using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public static class ClaimQuestion
    {
        public static List<Question> QuestionsCLAIM_LA_ADDRESS()
        {
            return new List<Question>
            {
                new Question
                {
                    QuestionText = "Did Life Assured (LA) had any Injury/Illness prior to commencement/revival ?",
                    QuestionType = "dropdown",
                    Options = "YES, NO, UNKNOWN",
                    IsRequired = true
                },
                new Question
                {
                    QuestionText = "Duration of treatment ?",
                    QuestionType = "dropdown",
                    Options = "NONE , Less Than 6 months, More Than 6 months, UNKNOWN",
                    IsRequired = true
                },
                new Question
                {
                    QuestionText = "Name of person met LA Address ?",
                    QuestionType = "text",
                    IsRequired = true
                },
                new Question
                {
                    QuestionText = "Any Other findings",
                    QuestionType = "text",
                    IsRequired = false
                }
            };
        }
        public static List<Question> QuestionsCLAIM_BUSINESS_ADDRESS()
        {
            return new List<Question>
             {
                new Question
                {
                    QuestionText = "How long was Deceased person in the business ?",
                    QuestionType = "dropdown",
                    Options = "NONE , Less Than 6 months, More Than 6 months, UNKNOWN",
                    IsRequired = true
                },
                    new Question
                {
                    QuestionText = "Nature of LA's business at the time of proposal ?",
                    QuestionType = "text",
                    IsRequired = true
                },

                new Question
                {
                    QuestionText = "Address of business premises ?",
                    QuestionType = "text",
                    IsRequired = true
                }
                ,
                new Question
                {
                    QuestionText = "Name of the Business Associate met ?",
                    QuestionType = "text",
                    IsRequired = true
                },
                new Question
                {
                    QuestionText = "Any Other findings",
                    QuestionType = "text",
                    IsRequired = false
                }
            };
        }
        public static List<Question> QuestionsCLAIM_CHEMIST_ADDRESS()
        {
            return new List<Question>
            {
                new Question
            {
                QuestionText = "Chemist Name ?",
                QuestionType = "text",
                IsRequired = true
            },
                new Question
            {
                QuestionText = "Chemist Address ?",
                QuestionType = "text",
                IsRequired = true
            },
                new Question
            {
                QuestionText = "Contact Number ?",
                QuestionType = "text",
                IsRequired = true
            } ,
                new Question
                {
                    QuestionText = "Any Other findings",
                    QuestionType = "text",
                    IsRequired = false
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
                QuestionText = "Company Address ?",
                QuestionType = "text",
                IsRequired = true
            },
                new Question
            {
                QuestionText = "Contact Number ?",
                QuestionType = "text",
                IsRequired = true
            }
                ,
                new Question
            {
                QuestionText = "whether LA suffered from  any ailment and were they aware as to where LA was being treated ?",
                QuestionType = "checkbox",
                Options = "YES",

            }   ,
                new Question
            {
                QuestionText = "Date of joining employment (service) ?",
                QuestionType = "date",
            } ,
                new Question
            {
                QuestionText = "LA's employment nature of duties (role and responsibilites) ?",
                QuestionType = "text",
            } ,
                new Question
                {
                    QuestionText = "Any Other findings",
                    QuestionType = "text",
                    IsRequired = false
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
                QuestionText = "Designation of the Person met at the cemetery ?",
                QuestionType = "text",
                IsRequired = true
            },
                 new Question
            {
                QuestionText = "Contact Number of the Person met at the cemetery ?",
                QuestionType = "text",
                IsRequired = true
            }
                ,
                new Question
            {
                QuestionText = "Life Assured Cremated / Buried ?",
                QuestionType = "radio",
                Options = "Cremated, Buried",
                },
                new Question
                {
                    QuestionText = "Any Other findings",
                    QuestionType = "text",
                    IsRequired = false
                }
            };
        }
        public static List<Question> QuestionsCLAIM_POLIC_STATION()
        {
            return new List<Question>
                        {
                            new Question
                            {
                                QuestionText = "Name of the police station and Address ?",
                                QuestionType = "text",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Name of the Sub Inspector–in-charge of the case ?",
                                QuestionType = "text",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Date/time when the body sent for Autopsy ?",
                                QuestionType = "date",
                                IsRequired = true
                            } ,
                            new Question
                            {
                                QuestionText = "Any Other findings",
                                QuestionType = "text",
                                IsRequired = false
                            }
                        };
        }
        public static List<Question> QuestionsCLAIM_HOSPITAL()
        {
            return new List<Question>
                        {
                            new Question
                            {
                                QuestionText = "Name and address of the LA’s usual medical attendant/family doctor during the past 3 years. If more than one, mention all. ?",
                                QuestionType = "text",
                            },
                            new Question
                            {
                                QuestionText = "Did LA (Deceased) suffer from any illness or injury prior to the commencement revival of the policy ?",
                                QuestionType = "checkbox",
                                Options = "YES",
                            },
                             new Question
                            {
                                QuestionText = "Full particulars and name(s) of doctors consulted.?",
                                QuestionType = "text",
                            },
                            new Question
                            {
                                QuestionText = "LA last illness (mention the period of hospitalization, name(s) of the doctors attended and IP/OP No.?",
                                QuestionType = "text",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "When was the disease of which the LA died, first suspected or diagnosed ?",
                                QuestionType = "date",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Date on which the last attending doctor was first consulted. (During the last illness and or before that) ?",
                                QuestionType = "date",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Duration of last illness ?",
                                QuestionType = "dropdown",
                                Options = "LESS THAN 1 WEEK, LESS THAN 1 MONTH, LESS THAN 3 MONTH, LESS THAN 6 MONTH, MORE THAN 6 MONTH, UNKNOWN",
                                IsRequired = true
                            },
                             new Question
                            {
                                QuestionText = "Medical Cause of Death ?",
                                QuestionType = "dropdown",
                                Options = "NATURAL DEATH, ACCIDENT/SUDDEN DEATH, UNKNOWN",
                                IsRequired = true
                            },
                              new Question
                            {
                                QuestionText = "Did any doctor treat the LA for the same or any other ailment at any time before the commencement  revival of the policy ?",
                                QuestionType = "checkbox",
                                Options = "YES",
                                IsRequired = false
                            },
                               new Question
                            {
                                QuestionText = "If YES, what was ailment ?",
                                QuestionType = "text",
                                IsRequired = false
                            },
                               new Question
                            {
                                QuestionText = "If YES, for how long ?",
                                QuestionType = "dropdown",
                                Options = "LESS THAN 1 WEEK, LESS THAN 1 MONTH, LESS THAN 3 MONTH, LESS THAN 6 MONTH, MORE THAN 6 MONTH, UNKNOWN",
                                IsRequired = false
                            },
                            new Question
                            {
                                QuestionText = "Name of Medical staff met ?",
                                QuestionType = "text",
                                IsRequired = true
                            },
                            new Question
                            {
                                QuestionText = "Any Other findings",
                                QuestionType = "text",
                                IsRequired = false
                            }
                        };
        }
    }
}