using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class DocumentIdReport : IdReportBase
    {
        public byte[]? IdImageBack { get; set; }
        public bool HasBackImage { get; set; } = true;
        public DocumentIdReportType ReportType { get; set; } = DocumentIdReportType.PAN;
        public long? LocationTemplateId { get; set; }  // This is the FK property
        public LocationTemplate? LocationTemplate { get; set; }  // Navigation property
    }

    public enum DocumentIdReportType
    {
        ADHAAR,
        PAN,
        DRIVING_LICENSE,
        PASSPORT,
        VOTER_CARD,
        DEATH_CERTIFICATE,
        ITR,
        P_AND_L_ACCOUNT_STATEMENT,
        BIRTH_CERTIFICATE,
        MARRIAGE_CERTIFICATE,
        MEDICAL_CERTIFICATE,
        EMPLOYMENT_RECORD,
        MEDICAL_PRESCRIPTION,
        HOSPITAL_DISCHARGE_SUMMARY,
        HOSPITAL_ADMISSION_SUMMARY,
        HOSPITAL_DEATH_SUMMARY,
        HOSPITAL_TREATMENT_SUMMARY,
        POLICE_FIR_REPORT,
        POLICE_CASE_DIARY
    }
}