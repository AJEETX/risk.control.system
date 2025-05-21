using System;
using System.ComponentModel.DataAnnotations;

public class UploadCase
{
    [Required]
    [Display(Name = "Insurance Type")]
    public string InsuranceType { get; set; } = "CLAIM";

    [Required]
    [Display(Name = "Case ID")]
    public string CaseId { get; set; }

    [Display(Name = "Service Type")]
    public string? ServiceType { get; set; }

    [Display(Name = "Reason")]
    public string? Reason { get; set; } = "DEMO";

    [Required]
    [Display(Name = "Assured Amount")]
    [DataType(DataType.Currency)]
    public string Amount { get; set; }

    [Required]
    [Display(Name = "Issue Date")]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    public string IssueDate { get; set; }

    [Required]
    [Display(Name = "Incident Date")]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    public string IncidentDate { get; set; }

    [Display(Name = "Cause of Incident")]
    public string? Cause { get; set; }

    [Display(Name = "Department")]
    public string? Department { get; set; }

    [Required]
    [Display(Name = "Customer Name")]
    [StringLength(100)]
    public string CustomerName { get; set; }

    [Display(Name = "Customer Type")]
    public string? CustomerType { get; set; }

    [Display(Name = "Gender")]
    public string? Gender { get; set; }

    [Required]
    [Display(Name = "Customer Date of Birth")]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    public string CustomerDob { get; set; }

    [Required]
    [Display(Name = "Customer Contact")]
    [Phone]
    public string CustomerContact { get; set; }

    [Display(Name = "Education")]
    public string? Education { get; set; }

    [Display(Name = "Occupation")]
    public string? Occupation { get; set; }

    [Display(Name = "Income")]
    [DataType(DataType.Currency)]
    public string? Income { get; set; }

    [Required]
    [Display(Name = "Customer Address Line")]
    public string CustomerAddressLine { get; set; }

    [Required]
    [Display(Name = "Customer District Name")]
    public string CustomerDistrictName { get; set; }
    [Required]
    [Display(Name = "Customer Pincode")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Pincode must be 6 digits")]
    public string CustomerPincode { get; set; }

    [Required]
    [Display(Name = "Beneficiary Name")]
    [StringLength(100)]
    public string BeneficiaryName { get; set; }

    [Display(Name = "Relation to Customer")]
    public string? Relation { get; set; }

    [Required]
    [Display(Name = "Beneficiary Date of Birth")]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    public string BeneficiaryDob { get; set; }

    [Display(Name = "Beneficiary Income")]
    [DataType(DataType.Currency)]
    public string? BeneficiaryIncome { get; set; }

    [Required]
    [Display(Name = "Beneficiary Contact")]
    [Phone]
    public string BeneficiaryContact { get; set; }

    [Required]
    [Display(Name = "Beneficiary Address Line")]
    public string BeneficiaryAddressLine { get; set; }

    [Required]
    [Display(Name = "Beneficiary District Name")]
    public string BeneficiaryDistrictName { get; set; }
    [Required]
    [Display(Name = "Beneficiary Pincode")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Pincode must be 6 digits")]
    public string BeneficiaryPincode { get; set; }
}
