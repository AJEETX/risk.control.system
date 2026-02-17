using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;
using risk.control.system.Helpers;

namespace risk.control.system.Models
{
    public class CustomerDetail : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CustomerDetailId { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Date of birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        public Gender? Gender { get; set; }

        [Required]
        [Display(Name = "Contact number")]
        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Address line")]
        public string Addressline { get; set; }

        [Display(Name = "PinCode")]
        public long? PinCodeId { get; set; }

        [Display(Name = "PinCode")]
        public PinCode? PinCode { get; set; }

        [Display(Name = "State")]
        public long? StateId { get; set; }

        [Display(Name = "State")]
        public State? State { get; set; }

        [Display(Name = "Country")]
        public long? CountryId { get; set; }

        [Display(Name = "Country")]
        public Country? Country { get; set; }

        [Display(Name = "District")]
        public long? DistrictId { get; set; }

        [Display(Name = "District")]
        public District? District { get; set; }

        [Required]
        public long? InvestigationTaskId { get; set; }

        [Required]
        [Display(Name = "Annual Income")]
        public Income? Income { get; set; }

        [Required]
        [Display(Name = "Occupation")]
        public Occupation? Occupation { get; set; }

        [Required]
        [Display(Name = "Education")]
        public Education? Education { get; set; }

        public string? ImagePath { get; set; }
        public string? ProfilePictureExtension { get; set; }

        public string? Description { get; set; }
        public string? CustomerLocationMap { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }

        public string? AddressLocationInfo { get; set; }

        #region NOT MAPPED PROPERTIES

        [NotMapped]
        public IFormFile? ProfileImage { get; set; }

        [NotMapped]
        public long SelectedPincodeId { get; set; }

        [NotMapped]
        public long SelectedDistrictId { get; set; }

        [NotMapped]
        public long SelectedStateId { get; set; }

        [NotMapped]
        public long SelectedCountryId { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? GenderList { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? IncomeList { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? EducationList { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? OccupationList { get; set; }

        [NotMapped]
        public string? CurrencySymbol { get; set; }

        #endregion NOT MAPPED PROPERTIES

        public override string ToString()
        {
            return $"Customer Information:\n" +
           $"- Name: {Name}\n" +
           $"- Date of birth: {DateOfBirth}\n" +
           $"- Gender: ${Gender.GetEnumDisplayName()}\n" +
           $"- Address line: {Addressline}\n" +
           $"- City: {District}\n" +
           $"- State: {State}\n" +
           $"- Country: {Country}\n" +
           $"- Contact Number: {PhoneNumber}\n" +
           $"- Income: {Income.GetEnumDisplayName()}\n" +
           $"- Occupation: {Occupation.GetEnumDisplayName()}\n" +
           $"- Education: {Education.GetEnumDisplayName()}\n";
        }
    }
}