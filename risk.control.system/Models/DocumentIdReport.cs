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
        [Display(Name = "Aadhaar Card")]
        ADHAAR,

        [Display(Name = "PAN Card")]
        PAN,

        [Display(Name = "Driving License")]
        DRIVING_LICENSE,

        [Display(Name = "Passport")]
        PASSPORT,

        [Display(Name = "Voter Card")]
        VOTER_CARD,

        [Display(Name = "Death Certificate")]
        DEATH_CERTIFICATE,

        [Display(Name = "Income Tax Return (ITR)")]
        ITR,

        [Display(Name = "P&L Account Statement")]
        P_AND_L_ACCOUNT_STATEMENT,

        [Display(Name = "Birth Certificate")]
        BIRTH_CERTIFICATE,

        [Display(Name = "Marriage Certificate")]
        MARRIAGE_CERTIFICATE,

        [Display(Name = "Medical Certificate")]
        MEDICAL_CERTIFICATE,

        [Display(Name = "Employment Record")]
        EMPLOYMENT_RECORD,

        [Display(Name = "Medical Prescription")]
        MEDICAL_PRESCRIPTION,

        [Display(Name = "Hospital Discharge Summary")]
        HOSPITAL_DISCHARGE_SUMMARY,

        [Display(Name = "Hospital Admission Summary")]
        HOSPITAL_ADMISSION_SUMMARY,

        [Display(Name = "Hospital Death Summary")]
        HOSPITAL_DEATH_SUMMARY,

        [Display(Name = "Hospital Treatment Summary")]
        HOSPITAL_TREATMENT_SUMMARY,

        [Display(Name = "Police FIR Report")]
        POLICE_FIR_REPORT,

        [Display(Name = "Police Case Diary")]
        POLICE_CASE_DIARY
    }

}