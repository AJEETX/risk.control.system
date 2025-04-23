using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using risk.control.system.Helpers;

namespace risk.control.system.Models
{
    public class PolicyDetail : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PolicyDetailId { get; set; }

        [Display(Name = "Investigation type")]
        public long? InvestigationServiceTypeId { get; set; } = default!;

        [Display(Name = "Investigation type")]
        public InvestigationServiceType? InvestigationServiceType { get; set; } = default!;

        [Display(Name = "Case number")]
        public string ContractNumber { get; set; } = default!;

        [Display(Name = "Case issue date")]
        [DataType(DataType.Date)]
        public DateTime ContractIssueDate { get; set; }

        [Display(Name = "Claim type")]
        public ClaimType? ClaimType { get; set; } = Models.ClaimType.DEATH;
        public InsuranceType? InsuranceType { get; set; } = Models.InsuranceType.CLAIM;

        [Display(Name = "Date of incident")]
        [DataType(DataType.Date)]
        public DateTime DateOfIncident { get; set; }

        [Display(Name = "Cause of loss")]
        public string CauseOfLoss { get; set; }

        [Display(Name = "Sum assured value")]
        [Column(TypeName = "decimal(15,2)")]
        public decimal SumAssuredValue { get; set; }

        [Display(Name = "Budget centre")]
        public long? CostCentreId { get; set; }

        [Display(Name = "Budget centre")]
        public CostCentre? CostCentre { get; set; }

        public long? CaseEnablerId { get; set; }

        [Display(Name = "Reason To Verify")]
        public CaseEnabler? CaseEnabler { get; set; }

        [Display(Name = "Case Document")]
        [NotMapped]
        public IFormFile? Document { get; set; }

        [Display(Name = "Case Document")]
        public byte[]? DocumentImage { get; set; } = default!;

        [Display(Name = "Case remarks")]
        public string? Comments { get; set; }

        public override string ToString()
        {
            return $"Case Information:\n" +
           $"- Contract Number: {ContractNumber}\n" +
           $"- Investigation Service Type: {InvestigationServiceType}\n" +
           $"- Case Issue Date: ${ContractIssueDate}\n" +
           $"- Claim Type: {ClaimType.GetEnumDisplayName()}\n" +
           $"- Date Of Incident: {DateOfIncident}\n" +
           $"- Cause Of Loss: {CauseOfLoss}\n" +
           $"- Sum Assured Value: {SumAssuredValue}\n" +
           $"- Cost Centre: {CostCentre}\n" +
           $"- Case Enabler: {CaseEnabler}\n" +
           $"- Remarks: {Comments}";
        }
    }
}