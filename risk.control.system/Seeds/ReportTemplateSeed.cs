using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public static class ReportTemplateSeed
    {
        public static void UNDERWRITING(ApplicationDbContext context, ClientCompany company)
        {
            var serviceTypes = context.InvestigationServiceType.Where(i => i.InsuranceType == InsuranceType.UNDERWRITING).ToList();
            foreach (var serviceType in serviceTypes)
            {
                var template = CreateBaseTemplate(company, InsuranceType.UNDERWRITING, serviceType);

                template.LocationReport = new List<LocationReport>
                 {
                    new()
                        {
                            LocationName = CONSTANTS.LOCATIONS.LA_ADDRESS,
                            IsRequired = true,
                            AgentIdReport = CreateDefaultAgentReport(),
                            MediaReports = CreateDefaultMediaReports(true, false),
                            FaceIds = new List<FaceIdReport>
                            {
                                CreateFaceReport(DigitalIdReportType.CUSTOMER_FACE, true, true, CameraCaptureMode.Front),
                                CreateFaceReport(DigitalIdReportType.BENEFICIARY_FACE, true, false, CameraCaptureMode.Rear),
                            },
                            DocumentIds = new List<DocumentIdReport>
                            {
                                CreateDocumentReport(DocumentIdReportType.ADHAAR, true, PageCount.DOUBLE, true),
                                CreateDocumentReport(DocumentIdReportType.PAN, false, PageCount.SINGLE, false),
                                CreateDocumentReport(DocumentIdReportType.VOTER_ID, false, PageCount.DOUBLE, true),
                                CreateDocumentReport(DocumentIdReportType.DRIVING_LICENSE, false, PageCount.DOUBLE, true),
                                CreateDocumentReport(DocumentIdReportType.BIRTH_CERTIFICATE, false, PageCount.SINGLE, false),
                                CreateDocumentReport(DocumentIdReportType.EMPLOYMENT_RECORD, false, PageCount.MULTIPLE, false),
                                CreateDocumentReport(DocumentIdReportType.ITR, false, PageCount.SINGLE, false)
                            },
                            Questions = UnderwritingQuestion.QuestionsUNDERWRITING_LA_ADDRESS()
                        }
                };

                context.ReportTemplates.Add(template);
            }
        }

        public static void CLAIM(ApplicationDbContext context, ClientCompany company)
        {
            var serviceTypes = context.InvestigationServiceType.Where(i => i.InsuranceType == InsuranceType.CLAIM).ToList();
            foreach (var serviceType in serviceTypes)
            {
                var template = CreateBaseTemplate(company, InsuranceType.CLAIM, serviceType);

                template.LocationReport = new List<LocationReport>
                 {
                    new ()
                        {
                        LocationName = CONSTANTS.LOCATIONS.BENEFICIARY_ADDRESS,
                        IsRequired = true,
                        AgentIdReport = CreateDefaultAgentReport(),
                        MediaReports = CreateDefaultMediaReports(true, false),
                        FaceIds = new List<FaceIdReport>
                        {
                            CreateFaceReport(DigitalIdReportType.BENEFICIARY_FACE, true, true, CameraCaptureMode.Rear)
                        },
                        DocumentIds = new List<DocumentIdReport>
                        {
                            CreateDocumentReport(DocumentIdReportType.ADHAAR, true, PageCount.DOUBLE, true),
                            CreateDocumentReport(DocumentIdReportType.PAN, false, PageCount.SINGLE, false),
                            CreateDocumentReport(DocumentIdReportType.VOTER_ID, false, PageCount.DOUBLE, true),
                            CreateDocumentReport(DocumentIdReportType.DRIVING_LICENSE, false, PageCount.DOUBLE, true),
                            CreateDocumentReport(DocumentIdReportType.BIRTH_CERTIFICATE, false, PageCount.SINGLE, false),
                            CreateDocumentReport(DocumentIdReportType.EMPLOYMENT_RECORD, false, PageCount.MULTIPLE, false),
                            CreateDocumentReport(DocumentIdReportType.ITR, false, PageCount.SINGLE, false)
                        },
                        Questions = ClaimQuestion.QuestionsCLAIM_LA_ADDRESS()
                        },

                        CreateLocation(CONSTANTS.LOCATIONS.HOSPITAL_ADDRESS, false,
                            ClaimQuestion.QuestionsCLAIM_HOSPITAL(),
                            docType: DocumentIdReportType.MEDICAL_CERTIFICATE),

                        CreateLocation(CONSTANTS.LOCATIONS.BUSINESS_ADDRESS, false,
                            ClaimQuestion.QuestionsCLAIM_BUSINESS_ADDRESS(),
                            docType: DocumentIdReportType.ITR),

                        CreateLocation(CONSTANTS.LOCATIONS.CHEMIST_ADDRESS, false,
                            ClaimQuestion.QuestionsCLAIM_CHEMIST_ADDRESS(),
                            docType: DocumentIdReportType.MEDICAL_PRESCRIPTION),

                        CreateLocation(CONSTANTS.LOCATIONS.EMPLOYMENT_ADDRESS, false,
                            ClaimQuestion.QuestionsCLAIM_EMPLOYMENT_ADDRESS(),
                            docType: DocumentIdReportType.EMPLOYMENT_RECORD),

                        CreateLocation(CONSTANTS.LOCATIONS.CEMETERY_ADDRESS, true,
                            ClaimQuestion.QuestionsCLAIM_CEMETERY_ADDRESS(),
                            docType: DocumentIdReportType.DEATH_CERTIFICATE),

                        CreateLocation(CONSTANTS.LOCATIONS.POLICE_ADDRESS, false,
                            ClaimQuestion.QuestionsCLAIM_POLIC_STATION(),
                            docType: DocumentIdReportType.POLICE_FIR_REPORT)
                };

                context.ReportTemplates.Add(template);
            }
        }

        #region Helpers

        private static ReportTemplate CreateBaseTemplate(ClientCompany company, InsuranceType type, InvestigationServiceType serviceType) => new()
        {
            Name = type.GetEnumDisplayName().ToLower(),
            InsuranceType = type,
            InvestigationServiceTypeId = serviceType.InvestigationServiceTypeId,
            ClientCompanyId = company.ClientCompanyId,
            IsActive = true,
            Basetemplate = true
        };

        private static LocationReport CreateLocation(string name, bool isReq, List<Question> questions, DocumentIdReportType docType, List<FaceIdReport> faceIds = null!) => new()
        {
            LocationName = name,
            IsRequired = isReq,
            AgentIdReport = CreateDefaultAgentReport(),
            MediaReports = CreateDefaultMediaReports(true, false), // Claims usually default media to false/selected
            DocumentIds = new List<DocumentIdReport> { CreateDocumentReport(docType, true) },
            FaceIds = faceIds ?? new List<FaceIdReport>(),
            Questions = questions
        };

        private static AgentIdReport CreateDefaultAgentReport() => new()
        {
            Selected = true,
            IsRequired = true,
            ReportType = DigitalIdReportType.AGENT_FACE,
            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName(),
            CameraCaptureMode = CameraCaptureMode.Front
        };

        private static List<MediaReport> CreateDefaultMediaReports(bool selected, bool isRequired) => new()
        {
            new() { Selected = selected, IsRequired = isRequired, ReportName = MediaType.AUDIO.GetEnumDisplayName(), MediaType = MediaType.AUDIO, MediaExtension = "mp3", CameraCaptureMode = CameraCaptureMode.None },
            new() { Selected = selected, IsRequired = isRequired, ReportName = MediaType.VIDEO.GetEnumDisplayName(), MediaType = MediaType.VIDEO, MediaExtension = "mp4", CameraCaptureMode = CameraCaptureMode.Front }
        };

        private static FaceIdReport CreateFaceReport(DigitalIdReportType type, bool selected, bool isRequired, CameraCaptureMode cameraCapture = CameraCaptureMode.Front) => new()
        {
            Selected = selected,
            IsRequired = isRequired,
            ReportType = type,
            ReportName = type.GetEnumDisplayName(),
            Has2Face = false,
            CameraCaptureMode = cameraCapture
        };

        private static DocumentIdReport CreateDocumentReport(DocumentIdReportType type, bool required, PageCount pages = PageCount.SINGLE, bool hasBackImage = false, CameraCaptureMode cameraCapture = CameraCaptureMode.Rear) => new()
        {
            Selected = true,
            IsRequired = required,
            ReportType = type,
            ReportName = type.GetEnumDisplayName(),
            PageCount = pages,
            HasBackImage = hasBackImage,
            CameraCaptureMode = cameraCapture
        };

        #endregion
    }
}