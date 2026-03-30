using Microsoft.AspNetCore.Mvc.Rendering;

namespace risk.control.system.Models.ViewModel
{
    public class ReportTemplateCreateViewModel
    {
        public string? Name { get; set; }
        public InsuranceType InsuranceType { get; set; } = InsuranceType.CLAIM;
        public long? ReportTemplateId { get; set; }
        public List<FaceIdCreateViewModel>? FaceIds { get; set; } = new();
        public List<DocumentIdCreateViewModel>? DocumentIds { get; set; } = new();
        public QuestionFormViewModel? Questions { get; set; } = new();
        public List<SelectListItem>? DigitalIdReportItems { get; set; }
        public List<SelectListItem>? DocumentIdReportItems { get; set; }
    }
}
