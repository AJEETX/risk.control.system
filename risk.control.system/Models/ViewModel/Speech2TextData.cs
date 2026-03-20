using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models.ViewModel
{
    public class Speech2TextData
    {
        [Required]
        public IFormFile SpeechInputData { get; set; }

        [Required]
        public string? TextData { get; set; }

        public int RemainingTries { get; set; } = 5;
    }
}