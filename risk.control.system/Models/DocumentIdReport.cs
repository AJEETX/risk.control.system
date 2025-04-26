using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class DocumentIdReport : IdReportBase
    {
        public bool? DocumentIdImageValid { get; set; } = false;
        public string? DocumentIdImageType { get; set; }

        public DocumentIdReportType DocumentIdReportType { get; set; } = DocumentIdReportType.PAN;
        public override string ToString()
        {
            return $"Report Information: \n" +
                $"- Valid: {ValidationExecuted}";
        }
    }

    public enum DocumentIdReportType
    {
        ADHAAR,
        PAN,
        DRIVING_LICENSE,
        PASSPORT
    }
}