using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models.ViewModel
{
    public class DocumentOcrData
    {
        [Required]
        public IFormFile DocumentImage { get; set; } = default!;

        public int RemainingTries { get; set; } = 5;
    }
}