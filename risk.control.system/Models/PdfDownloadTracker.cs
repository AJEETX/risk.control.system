using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class PdfDownloadTracker : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long ReportId { get; set; }
        public string UserEmail { get; set; }
        public int DownloadCount { get; set; } = 0;
        public DateTime LastDownloaded { get; set; }
    }
}
