using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.AspNetCore.Mvc.Rendering;

namespace risk.control.system.Models
{
    // ReportTemplate myDeserializedClass = JsonConvert.DeserializeObject<ReportTemplate>(myJsonResponse);

    public class ReportTemplate : BaseEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string? Name { get; set; }
        public long? ClientCompanyId { get; set; }
        public ClientCompany? ClientCompany { get; set; }
        public InsuranceType InsuranceType { get; set; } = InsuranceType.CLAIM;
        
        public List<LocationTemplate> LocationTemplate { get; set; } = new List<LocationTemplate>();
        public bool Basetemplate { get; set; } = false;
        public long? OriginalTemplateId { get; set; }
    }

    public class LocationTemplate : BaseEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string? LocationName { get; set; }
        public long AgentId { get; set; }
        public DigitalIdReport Agent { get; set; } = new DigitalIdReport { Selected = true, ReportType = DigitalIdReportType.AGENT_FACE };
        public List<DigitalIdReport>? FaceIds { get; set; } = new List<DigitalIdReport>();
        public List<DocumentIdReport>? DocumentIds { get; set; } = new List<DocumentIdReport>();
        public List<Question>? Questions { get; set; } = new List<Question>();
    }
    public class ReportTemplateCreateViewModel
    {
        public string? Name { get; set; }
        public InsuranceType InsuranceType { get; set; } = InsuranceType.CLAIM;
        public long? ReportTemplateId { get; set; }
        public ReportTemplate? ReportTemplate { get; set; }
        public List<FaceIdCreateViewModel>? FaceIds { get; set; } = new();
        public List<DocumentIdCreateViewModel>? DocumentIds { get; set; } = new();
        public QuestionFormViewModel? Questions { get; set; } = new();
        public List<SelectListItem>? DigitalIdReportItems { get; set; }
        public List<SelectListItem>? DocumentIdReportItems { get; set; }
    }

    public class FaceIdCreateViewModel
    {
        public long? Id { get; set; }
        public DigitalIdReportType? DigitalIdReportType { get; set; }
        public string? FaceIdName { get; set; }
    }

    public class DocumentIdCreateViewModel
    {
        public long? Id { get; set; }
        public DocumentIdReportType? DocumentIdReportType { get; set; }
        public string? DocumentIdName { get; set; }
    }

}
