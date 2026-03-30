using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public static class ReportTemplateSeed
    {
        public static ReportTemplate UNDERWRITING(ApplicationDbContext context, ClientCompany company)
        {
            var template = CreateBaseTemplate(company, InsuranceType.UNDERWRITING);

            template.LocationReport = new List<LocationReport>
            {
                new()
                {
                    LocationName = CONSTANTS.LOCATIONS.LA_ADDRESS,
                    IsRequired = true,
                    AgentIdReport = CreateDefaultAgentReport(),
                    MediaReports = CreateDefaultMediaReports(true, true),
                    FaceIds = new List<FaceIdReport>
                    {
                        CreateFaceReport(DigitalIdReportType.CUSTOMER_FACE, true, true),
                        CreateFaceReport(DigitalIdReportType.BENEFICIARY_FACE, true, false)
                    },
                    DocumentIds = new List<DocumentIdReport>
                    {
                        CreateDocumentReport(DocumentIdReportType.PAN, true)
                    },
                    Questions = UnderwritingQuestion.QuestionsUNDERWRITING_LA_ADDRESS()
                }
            };

            context.ReportTemplates.Add(template);
            return template;
        }

        public static ReportTemplate CLAIM(ApplicationDbContext context, ClientCompany company)
        {
            var template = CreateBaseTemplate(company, InsuranceType.CLAIM);

            template.LocationReport = new List<LocationReport>
            {
                CreateLocation(CONSTANTS.LOCATIONS.BENEFICIARY_ADDRESS, true,
                    ClaimQuestion.QuestionsCLAIM_LA_ADDRESS(),
                    faceIds: new() {
                        CreateFaceReport(DigitalIdReportType.CUSTOMER_FACE, false, false),
                        CreateFaceReport(DigitalIdReportType.BENEFICIARY_FACE, true, true)
                    },
                    docType: DocumentIdReportType.PAN),

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
            return template;
        }

        #region Helpers

        private static ReportTemplate CreateBaseTemplate(ClientCompany company, InsuranceType type) => new()
        {
            Name = type.GetEnumDisplayName().ToLower(),
            InsuranceType = type,
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
            ReportName = DigitalIdReportType.AGENT_FACE.GetEnumDisplayName()
        };

        private static List<MediaReport> CreateDefaultMediaReports(bool selected, bool isRequired) => new()
        {
            new() { Selected = selected, IsRequired = isRequired, ReportName = MediaType.AUDIO.GetEnumDisplayName(), MediaType = MediaType.AUDIO, MediaExtension = "mp3" },
            new() { Selected = selected, IsRequired = isRequired, ReportName = MediaType.VIDEO.GetEnumDisplayName(), MediaType = MediaType.VIDEO, MediaExtension = "mp4" }
        };

        private static FaceIdReport CreateFaceReport(DigitalIdReportType type, bool selected, bool isRequired) => new()
        {
            Selected = selected,
            IsRequired = isRequired,
            ReportType = type,
            ReportName = type.GetEnumDisplayName(),
            Has2Face = false
        };

        private static DocumentIdReport CreateDocumentReport(DocumentIdReportType type, bool required) => new()
        {
            Selected = true,
            IsRequired = required,
            ReportType = type,
            ReportName = type.GetEnumDisplayName(),
            HasBackImage = false
        };

        #endregion
    }
}