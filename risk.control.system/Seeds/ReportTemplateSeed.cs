using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public class ReportTemplateSeed
    {
        public static ReportTemplate QuestionsUNDERWRITING(ApplicationDbContext context, ClientCompany company)
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
                        IsRequired = true,
                        AgentIdReport = new AgentIdReport
                        {
                            IsRequired = true,
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName(),                                          // You can set other properties of Agent here if needed
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                        },
                        MediaReports = new List<MediaReport>
                        {
                            new MediaReport
                            {
                                IsRequired = false,
                                Selected = true,
                                ReportName = MediaType.AUDIO.GetEnumDisplayName(),
                                MediaType = MediaType.AUDIO,
                                MediaExtension = "mp3"
                            },
                            new MediaReport
                            {
                                IsRequired = false,
                                Selected = true,
                                ReportName = MediaType.VIDEO.GetEnumDisplayName(),
                                MediaType = MediaType.VIDEO,
                                MediaExtension = "mp4"
                            }
                        },

                        FaceIds = new List<DigitalIdReport>
                        {
                            new DigitalIdReport
                            {
                                IsRequired = true,
                                Selected = true,
                                ReportName = DigitalIdReportType.CUSTOMER_FACE.GetEnumDisplayName(),
                                Has2Face = false,
                                ReportType = DigitalIdReportType.CUSTOMER_FACE
                            },
                            new DigitalIdReport
                            {
                                IsRequired = false,
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
                                IsRequired = true,
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
            context.ReportTemplates.Add(template);
            return template;
        }

        public static ReportTemplate QuestionsCLAIM(ApplicationDbContext context, ClientCompany company)
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
                        LocationName = CONSTANTS.LOCATIONS.LA_ADDRESS,
                        IsRequired = true,
                        AgentIdReport = new AgentIdReport
                        {
                            IsRequired = true,
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()                                              // You can set other properties of Agent here if needed
                        },
                        MediaReports = new List<MediaReport>
                        {
                            new MediaReport
                            {
                                IsRequired = false,
                                Selected = true,
                                ReportName = MediaType.AUDIO.GetEnumDisplayName(),
                                MediaType = MediaType.AUDIO,
                                MediaExtension = "mp3"
                            },
                            new MediaReport
                            {
                                IsRequired = false,
                                Selected = true,
                                ReportName = MediaType.VIDEO.GetEnumDisplayName(),
                                MediaType = MediaType.VIDEO,
                                MediaExtension = "mp4"
                            }
                        },
                        FaceIds = new List<DigitalIdReport>
                        {
                            new DigitalIdReport
                            {
                                Selected = false,
                                ReportName = DigitalIdReportType.CUSTOMER_FACE.GetEnumDisplayName(),
                                Has2Face = false,
                                ReportType = DigitalIdReportType.CUSTOMER_FACE
                            },
                            new DigitalIdReport
                            {
                                IsRequired = true,
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
                                IsRequired = true,
                                Selected = true,
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.PAN.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.PAN
                            }
                        },
                        Questions = ClaimQuestion.QuestionsCLAIM_LA_ADDRESS()
                    },
                    new LocationTemplate
                    {
                        LocationName = CONSTANTS.LOCATIONS.HOSPITAL_ADDRESS,
                                IsRequired = false,
                        AgentIdReport = new AgentIdReport
                        {
                            IsRequired = true,
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()                                            // You can set other properties of Agent here if needed
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                IsRequired = true,
                                Selected = true,
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.MEDICAL_CERTIFICATE.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.MEDICAL_CERTIFICATE
                            }
                        },
                        Questions = ClaimQuestion.QuestionsCLAIM_HOSPITAL()
                    },

                    new LocationTemplate
                    {
                        LocationName = CONSTANTS.LOCATIONS.BUSINESS_ADDRESS,
                                IsRequired = false,
                        AgentIdReport = new AgentIdReport
                        {
                            IsRequired = true,
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()                                              // You can set other properties of Agent here if needed
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                IsRequired = true,
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
                        LocationName = CONSTANTS.LOCATIONS.CHEMIST_ADDRESS,
                                IsRequired = false,
                        AgentIdReport = new AgentIdReport
                        {
                                IsRequired = true,
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()                                              // You can set other properties of Agent here if needed
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                IsRequired = true,
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
                                IsRequired = false,
                        AgentIdReport = new AgentIdReport
                        {
                                IsRequired = true,
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()                                              // You can set other properties of Agent here if needed
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                IsRequired = true,
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
                                IsRequired = true,
                        AgentIdReport = new AgentIdReport
                        {
                                IsRequired = true,
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()                                              // You can set other properties of Agent here if needed
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                IsRequired = true,
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
                                IsRequired = false,
                        AgentIdReport = new AgentIdReport
                        {
                                IsRequired = true,
                            ReportType = DigitalIdReportType.AGENT_FACE,  // Default agent
                            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()                                            // You can set other properties of Agent here if needed
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            new DocumentIdReport
                            {
                                IsRequired = true,
                                Selected = true,
                                HasBackImage = false,
                                ReportName = DocumentIdReportType.POLICE_FIR_REPORT.GetEnumDisplayName(),
                                ReportType = DocumentIdReportType.POLICE_FIR_REPORT
                            }
                        },
                        Questions = ClaimQuestion.QuestionsCLAIM_POLIC_STATION()
                    },

                }
            };
            context.ReportTemplates.Add(template);
            return template;
        }

    }
}