using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class ReportTemplate : BaseEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string? Name { get; set; }
        public long? ClientCompanyId { get; set; }
        public ClientCompany? ClientCompany { get; set; }
        public InsuranceType InsuranceType { get; set; } = InsuranceType.CLAIM;
        public List<LocationReport> LocationReport { get; set; } = new List<LocationReport>();
        public bool Basetemplate { get; set; } = false;
        public bool IsActive { get; set; } = false;
        public long? OriginalTemplateId { get; set; }
        public bool IsDeleted { get; set; } = false;

        [NotMapped]
        public long CaseId { get; set; }

        public override string ToString()
        {
            return $"Report: \n" +
                $"Location Report: {LocationReport.ToString()}";
        }
    }
}