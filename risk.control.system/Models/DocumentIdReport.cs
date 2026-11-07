using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using risk.control.system.Helpers;

namespace risk.control.system.Models
{
    public class DocumentIdReport : BaseEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public CameraCaptureMode CameraCaptureMode { get; set; } = CameraCaptureMode.Front;
        public bool Selected { get; set; } = false;
        public string? ReportName { get; set; }
        public bool HasBackImage { get; set; } = true;
        public List<DocumentFile> DocumentFiles { get; set; } = new List<DocumentFile>();
        public DocumentIdReportType ReportType { get; set; } = DocumentIdReportType.PAN;
        public string? LocationInfo { get; set; }
        public string? LocationMapUrl { get; set; }
        public string? Distance { get; set; }
        public float? DistanceInMetres { get; set; }
        public string? Duration { get; set; }
        public int? DurationInSeconds { get; set; }
        public string? LocationAddress { get; set; }
        public string? LongLat { get; set; }
        public DateTime? LongLatTime { get; set; }
        public bool ValidationExecuted { get; set; } = false;
        public bool? ImageValid { get; set; } = false;
        public bool IsRequired { get; set; } = false;
        public PageCount PageCount { get; set; } = PageCount.SINGLE;
        public long? LocationReportId { get; set; }  // This is the FK property
        public LocationReport? LocationReport { get; set; }  // Navigation property

        public override string ToString()
        {
            return $"Document Report:\n" +
                $"Document Has Back Image:{HasBackImage}\n" +
                $"Report Type:{ReportType.GetEnumDisplayName()}";
        }
    }
    public class DocumentFile
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public int PageNumber { get; set; }
        public string? FilePath { get; set; }
        public string? OriginalFilePath { get; set; }
        public string? OcrText { get; set; }
    }
    public enum PageCount
    {
        [Display(Name = "Single")]
        SINGLE,
        [Display(Name = "Double")]
        DOUBLE,
        [Display(Name = "Multiple")]
        MULTIPLE
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

        [Display(Name = "Voter_ID")]
        VOTER_ID,

        [Display(Name = "Death_Certificate")]
        DEATH_CERTIFICATE,

        [Display(Name = "Income_Tax_Return")]
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