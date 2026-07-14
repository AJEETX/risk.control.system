using System.ComponentModel.DataAnnotations;

using risk.control.system.Helpers;

namespace risk.control.system.Models
{
    public class DocumentIdReport : ReportBase
    {
        public bool HasBackImage { get; set; } = true;
        public DocumentIdReportType ReportType { get; set; } = DocumentIdReportType.PAN;
        public long? LocationReportId { get; set; }  // This is the FK property
        public LocationReport? LocationReport { get; set; }  // Navigation property

        public override string ToString()
        {
            return $"Document Report:\n" +
                $"Has Back Image:{HasBackImage}\n" +
                $"Report Type:{ReportType.GetEnumDisplayName()}";
        }
    }

    public enum DocumentIdReportType
    {
        [Display(Name = "Aadhaar_Card")]
        ADHAAR,

        [Display(Name = "PAN_Card")]
        PAN,

        [Display(Name = "Driving_License")]
        DRIVING_LICENSE,

        [Display(Name = "Passport")]
        PASSPORT,

        [Display(Name = "Voter_Card")]
        VOTER_CARD,

        [Display(Name = "Death_Certificate")]
        DEATH_CERTIFICATE,

        [Display(Name = "Income_Tax_Return_(ITR)")]
        ITR,

        [Display(Name = "P&L_Account_Statement")]
        P_AND_L_ACCOUNT_STATEMENT,

        [Display(Name = "Birth_Certificate")]
        BIRTH_CERTIFICATE,

        [Display(Name = "Marriage_Certificate")]
        MARRIAGE_CERTIFICATE,

        [Display(Name = "Medical_Certificate")]
        MEDICAL_CERTIFICATE,

        [Display(Name = "Employment_Record")]
        EMPLOYMENT_RECORD,

        [Display(Name = "Medical_Prescription")]
        MEDICAL_PRESCRIPTION,

        [Display(Name = "Hospital_Discharge_Summary")]
        HOSPITAL_DISCHARGE_SUMMARY,

        [Display(Name = "Hospital_Admission_Summary")]
        HOSPITAL_ADMISSION_SUMMARY,

        [Display(Name = "Hospital_Treatment_Summary")]
        HOSPITAL_TREATMENT_SUMMARY,

        [Display(Name = "Police_FIR_Report")]
        POLICE_FIR_REPORT,

        [Display(Name = "Police_Case_Diary")]
        POLICE_CASE_DIARY
    }
}