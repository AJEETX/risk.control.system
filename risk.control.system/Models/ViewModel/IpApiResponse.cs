using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using CsvHelper.Configuration.Attributes;

namespace risk.control.system.Models.ViewModel
{
    public class IpApiResponse:BaseEntity
    {
        [Ignore]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long IpApiResponseId { get; set; }
        public string? status { get; set; }
        public string? continent { get; set; }
        public string? country { get; set; }
        public string? regionName { get; set; }
        public string? city { get; set; }
        public string? district { get; set; }
        public string? zip { get; set; }
        public double? lat { get; set; }
        public double? lon { get; set; }
        public string? isp { get; set; }
        public string? query { get; set; }
        public string? page { get; set; } = "dashboard";
        public string? user { get; set; }
        public bool? isAuthenticated { get; set; }
        [NotMapped]
        public string Dated { get { return Created.ToString("dd-MMM-yyyy HH:mm"); } }
    }
}