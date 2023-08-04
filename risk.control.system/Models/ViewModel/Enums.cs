using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models.ViewModel
{
    public enum Domain
    {
        [Display(Name = ".com")]
        com,

        [Display(Name = ".co.in")]
        coin,

        [Display(Name = ".in")]
        _in
    }
}