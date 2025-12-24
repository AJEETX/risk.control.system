using System.ComponentModel.DataAnnotations;
namespace risk.control.system.Models.ViewModel
{

    public class CreateCaseViewModel
    {
        [Required]
        public PolicyDetailDto PolicyDetail { get; set; } = new();

        [Required]
        [Display(Name = "Case Document")]
        public IFormFile? Document { get; set; }
    }

    public class EditPolicyDto
    {
        public long Id { get; set; }   // InvestigationTask Id

        public PolicyDetailDto PolicyDetail { get; set; }

        // Only used if user uploads a new file
        public IFormFile? Document { get; set; }

        // Display-only: existing path for showing the image
        public string? ExistingDocumentPath { get; set; }
    }

    public class PolicyDetailDto
    {
        [Required]
        [StringLength(20)]
        [Display(Name = "Case number")]
        public string ContractNumber { get; set; } = string.Empty;

        public InsuranceType? InsuranceType { get; set; } = Models.InsuranceType.CLAIM;

        [Required]
        [Display(Name = "Investigation type")]
        public long InvestigationServiceTypeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Case issue date")]
        public DateTime ContractIssueDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of incident")]
        public DateTime DateOfIncident { get; set; }

        [Required]
        [StringLength(70)]
        [Display(Name = "Cause of loss")]
        public string CauseOfLoss { get; set; } = string.Empty;

        [Required]
        [Range(100, double.MaxValue)]
        [Display(Name = "Sum assured value")]
        public decimal SumAssuredValue { get; set; }

        [Required]
        [Display(Name = "Budget centre")]
        public long CostCentreId { get; set; }

        [Required]
        [Display(Name = "Reason To Verify")]
        public long CaseEnablerId { get; set; }
    }
}
