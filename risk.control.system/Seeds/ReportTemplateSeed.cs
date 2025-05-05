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
                                Selected = true,
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.PAN.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.PAN
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
                                Selected = true,
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.ITR.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.ITR
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
                                Selected = true,
                                ReportName = DocumentIdReportType.PAN.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.PAN
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
                                Selected = true,
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
                                Selected = true,
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
                                Selected = true,
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