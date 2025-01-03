﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using risk.control.system.Helpers;

namespace risk.control.system.Models
{
    public class PolicyDetail : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PolicyDetailId { get; set; }

        [Display(Name = "Line of Business")]
        public long? LineOfBusinessId { get; set; } = default!;

        [Display(Name = "Line of Business")]
        public LineOfBusiness? LineOfBusiness { get; set; } = default!;

        [Display(Name = "Investigation type")]
        public long? InvestigationServiceTypeId { get; set; } = default!;

        [Display(Name = "Investigation type")]
        public InvestigationServiceType? InvestigationServiceType { get; set; } = default!;

        [Display(Name = "Contract number")]
        public string ContractNumber { get; set; } = default!;

        [Required]
        [Display(Name = "Case issue date")]
        [DataType(DataType.Date)]
        public DateTime ContractIssueDate { get; set; }

        [Display(Name = "Claim type")]
        public ClaimType? ClaimType { get; set; }

        [Required]
        [Display(Name = "Date of incident")]
        [DataType(DataType.Date)]
        public DateTime DateOfIncident { get; set; }

        [Required]
        [Display(Name = "Cause of loss")]
        public string CauseOfLoss { get; set; }

        [Required]
        [Display(Name = "Sum assured value")]
        [Column(TypeName = "decimal(15,2)")]
        public decimal SumAssuredValue { get; set; }

        [Required]
        [Display(Name = "Budget centre")]
        public long CostCentreId { get; set; }

        [Display(Name = "Budget centre")]
        public CostCentre? CostCentre { get; set; }

        public long? CaseEnablerId { get; set; }

        [Display(Name = "Reason To Verify")]
        public CaseEnabler? CaseEnabler { get; set; }

        [Display(Name = "Claim Document")]
        [NotMapped]
        public IFormFile? Document { get; set; }

        [Display(Name = "Claim Document")]
        public byte[]? DocumentImage { get; set; } = default!;

        [Display(Name = "Claim remarks")]
        public string? Comments { get; set; }

        public override string ToString()
        {
            return $"Claim Information:\n" +
           $"- Contract Number: {ContractNumber}\n" +
           $"- Line Of Business: {LineOfBusiness}\n" +
           $"- Investigation Service Type: {InvestigationServiceType}\n" +
           $"- Contract Issue Date: ${ContractIssueDate}\n" +
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