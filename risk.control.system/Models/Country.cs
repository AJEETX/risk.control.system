using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models;

public class Country : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long CountryId { get; set; }

    [Display(Name = "Country name")]
    public string Name { get; set; } = default!;

    [Display(Name = "Country code")]
    [Required]
    public string Code { get; set; } = default!;

    [Display(Name = "ISD Code")]
    [Required]
    public int ISDCode { get; set; } = default!;

    public string? CurrencyName { get; set; } = "--";
    public string? CurrencyCode { get; set; } = "--";
    public string? Language { get; set; } = "--";
    public override string ToString()
    {
        return $"Country Information:\n" +
       $"- Name: {Name}\n" +
       $"- City code: {Code}";
    }
}