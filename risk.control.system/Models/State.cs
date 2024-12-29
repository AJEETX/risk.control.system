using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class State : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long StateId { get; set; }

        [Display(Name = "State name")]
        public string Name { get; set; } = default!;

        [Display(Name = "State code")]
        [Required]
        public string Code { get; set; } = default!;

        [Required]
        [Display(Name = "Country name")]
        public long CountryId { get; set; } = default!;

        [Display(Name = "Country name")]
        public Country Country { get; set; } = default!;

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