using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public class ReportTemplateSeed
    {
        public static void QuestionsUNDERWRITING(ApplicationDbContext context, ClientCompany company)
        {
            var template = new ReportTemplate
            {
                Name = InsuranceType.UNDERWRITING.GetEnumDisplayName() + " TEMPLATE",
                InsuranceType = InsuranceType.UNDERWRITING,
                ClientCompanyId = company.ClientCompanyId,
                Basetemplate = true,
                LocationTemplate = new List<LocationTemplate>
                {
                    new LocationTemplate
                    {
                        LocationName = CONSTANTS.LOCATIONS.LA_ADDRESS,
                        AgentIdReport = new AgentIdReport
                        {
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName(),                                          // You can set other properties of Agent here if needed
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                        },
                        FaceIds = new List<DigitalIdReport>
                        {
                            new DigitalIdReport
                            {
                                Selected = true,
                                ReportName = DigitalIdReportType.CUSTOMER_FACE.GetEnumDisplayName(),
                                Has2Face = false,
                                ReportType = DigitalIdReportType.CUSTOMER_FACE
                            },
                            new DigitalIdReport
                            {
                                Selected = true,
                                ReportName = DigitalIdReportType.BENEFICIARY_FACE.GetEnumDisplayName(),
                                Has2Face = false,
                                ReportType = DigitalIdReportType.BENEFICIARY_FACE
                            }
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                Selected = false,
                                ReportName = DocumentIdReportType.ADHAAR.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.ADHAAR
                            },
                            new DocumentIdReport
                            {
                                Selected = false,
                                ReportName = DocumentIdReportType.DRIVING_LICENSE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.DRIVING_LICENSE
                            },
                            new DocumentIdReport
                            {
                                Selected = true,
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.PAN.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.PAN
                            },
                            new DocumentIdReport
                            {
                                Selected = false,
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.BIRTH_CERTIFICATE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.BIRTH_CERTIFICATE
                            },
                            new DocumentIdReport
                            {
                                Selected = false,
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.MEDICAL_CERTIFICATE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.MEDICAL_CERTIFICATE
                            },
                            new DocumentIdReport
                            {
                                Selected = false,
                                ReportName = DocumentIdReportType.VOTER_CARD.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.VOTER_CARD
                            }
                        },

                        Questions =   UnderwritingQuestion.QuestionsUNDERWRITING_LA_ADDRESS()
                    }
                }
            };

            // Add the new template to the database
            context.ReportTemplates.Add(template);
        }

        public static void QuestionsCLAIM(ApplicationDbContext context, ClientCompany company)
        {
            var template = new ReportTemplate
            {
                Name = InsuranceType.CLAIM.GetEnumDisplayName() + " TEMPLATE",
                InsuranceType = InsuranceType.CLAIM,
                ClientCompanyId = company.ClientCompanyId,
                Basetemplate = true,
                LocationTemplate = new List<LocationTemplate>
                {
                    new LocationTemplate
                    {
                        LocationName = CONSTANTS.LOCATIONS.HOSPITAL_ADDRESS,
                        AgentIdReport = new AgentIdReport
                        {
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()                                            // You can set other properties of Agent here if needed
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                Selected = true,
                                ReportName = DocumentIdReportType.MEDICAL_CERTIFICATE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.MEDICAL_CERTIFICATE
                            }   ,
                            new DocumentIdReport
                            {
                                Selected = true,
                                ReportName = DocumentIdReportType.HOSPITAL_DISCHARGE_SUMMARY.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.HOSPITAL_DISCHARGE_SUMMARY
                            }
                            ,
                            new DocumentIdReport
                            {
                                Selected = true,
                                ReportName = DocumentIdReportType.HOSPITAL_DEATH_SUMMARY.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.HOSPITAL_DEATH_SUMMARY
                            }
                            ,
                            new DocumentIdReport
                            {
                                Selected = true,
                                ReportName = DocumentIdReportType.HOSPITAL_TREATMENT_SUMMARY.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.HOSPITAL_TREATMENT_SUMMARY
                            }
                        },
                        Questions = ClaimQuestion.QuestionsCLAIM_HOSPITAL()
                    },

                    new LocationTemplate
                    {
                        LocationName = CONSTANTS.LOCATIONS.BUSINESS_ADDRESS,
                        AgentIdReport = new AgentIdReport
                        {
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()                                              // You can set other properties of Agent here if needed
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.ITR.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.ITR
                            }  ,
                            new DocumentIdReport
                            {
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.P_AND_L_ACCOUNT_STATEMENT.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.P_AND_L_ACCOUNT_STATEMENT
                            }
                        },
                        Questions = ClaimQuestion.QuestionsCLAIM_BUSINESS_ADDRESS()
                    },
                    new LocationTemplate
                    {
                        LocationName = CONSTANTS.LOCATIONS.LA_ADDRESS,
                        AgentIdReport = new AgentIdReport
                        {
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()                                              // You can set other properties of Agent here if needed
                        },
                        FaceIds = new List<DigitalIdReport>
                        {
                            new DigitalIdReport
                            {
                                ReportName = DigitalIdReportType.CUSTOMER_FACE.GetEnumDisplayName(),
                                ReportType = DigitalIdReportType.CUSTOMER_FACE
                            },
                            new DigitalIdReport
                            {
                                Selected = true,
                                ReportName = DigitalIdReportType.BENEFICIARY_FACE.GetEnumDisplayName(),
                                ReportType = DigitalIdReportType.BENEFICIARY_FACE
                            }
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.DEATH_CERTIFICATE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.DEATH_CERTIFICATE
                            },
                            new DocumentIdReport
                            {
                                ReportName = DocumentIdReportType.ADHAAR.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.ADHAAR
                            },
                            new DocumentIdReport
                            {
                                ReportName = DocumentIdReportType.DRIVING_LICENSE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.DRIVING_LICENSE
                            },
                            new DocumentIdReport
                            {
                                Selected = true,
                                ReportName = DocumentIdReportType.PAN.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.PAN
                            },
                            new DocumentIdReport
                            {
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.BIRTH_CERTIFICATE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.BIRTH_CERTIFICATE
                            },
                            new DocumentIdReport
                            {
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.MEDICAL_CERTIFICATE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.MEDICAL_CERTIFICATE
                            },
                            new DocumentIdReport
                            {
                                ReportName = DocumentIdReportType.VOTER_CARD.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.VOTER_CARD
                            }
                        },
                        Questions = ClaimQuestion.QuestionsCLAIM_LA_ADDRESS()
                    },
                    new LocationTemplate
                    {
                        LocationName = CONSTANTS.LOCATIONS.CHEMIST_ADDRESS,
                        AgentIdReport = new AgentIdReport
                        {
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()                                              // You can set other properties of Agent here if needed
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.MEDICAL_PRESCRIPTION.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.MEDICAL_PRESCRIPTION
                            }
                        },
                        Questions = ClaimQuestion.QuestionsCLAIM_CHEMIST_ADDRESS()
                    },
                    new LocationTemplate
                    {
                        LocationName = CONSTANTS.LOCATIONS.EMPLOYMENT_ADDRESS,
                        AgentIdReport = new AgentIdReport
                        {
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()                                              // You can set other properties of Agent here if needed
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.EMPLOYMENT_RECORD.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.EMPLOYMENT_RECORD
                            }
                        },
                        Questions = ClaimQuestion.QuestionsCLAIM_EMPLOYMENT_ADDRESS()
                    }
                    ,
                    new LocationTemplate
                    {
                        LocationName = CONSTANTS.LOCATIONS.CEMETERY_ADDRESS,
                        AgentIdReport = new AgentIdReport
                        {
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()                                              // You can set other properties of Agent here if needed
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.DEATH_CERTIFICATE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.DEATH_CERTIFICATE
                            }
                        },
                        Questions = ClaimQuestion.QuestionsCLAIM_CEMETERY_ADDRESS()
                    },
                    new LocationTemplate
                    {
                        LocationName = CONSTANTS.LOCATIONS.POLICE_ADDRESS,
                        AgentIdReport = new AgentIdReport
                        {
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()                                            // You can set other properties of Agent here if needed
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                Selected = true,
                                ReportName = DocumentIdReportType.POLICE_FIR_REPORT.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.POLICE_FIR_REPORT
                            }   ,
                            new DocumentIdReport
                            {
                                Selected = true,
                                ReportName = DocumentIdReportType.POLICE_CASE_DIARY.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.POLICE_CASE_DIARY
                            }

                        },
                        Questions = ClaimQuestion.QuestionsCLAIM_POLIC_STATION()
                    },
                    
                }
            };

            // Add the new template to the database
            context.ReportTemplates.Add(template);
        }

    }
}