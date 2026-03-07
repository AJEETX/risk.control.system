using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class State : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long StateId { get; set; }

        [Display(Name = "State name")]
        [Required]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters.")]
        public string Name { get; set; } = default!;

        [Display(Name = "State code")]
        [Required]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "Code must be between 2 and 20 characters.")]
        public string Code { get; set; } = default!;

        [Display(Name = "Country name")]
        public long? CountryId { get; set; } = default!;

        [Display(Name = "Country name")]
        public Country? Country { get; set; } = default!;

        [NotMapped]
        public long SelectedCountryId { get; set; }

        public override string ToString()
        {
            return $"State Information:\n" +
           $"- Name: {Name}\n" +
           $"- City code: {Code}\n" +
           $"- Country: {Country}";
        }
    }
}