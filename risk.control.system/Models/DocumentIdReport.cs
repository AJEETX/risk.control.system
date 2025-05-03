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
        BIRTH_CERTIFICATE,
        MARRIAGE_CERTIFICATE,
        MEDICAL_CERTIFICATE,
        POLICE_REPORT
    }
}