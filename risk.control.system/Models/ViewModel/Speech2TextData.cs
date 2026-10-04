using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models.ViewModel
{
    public class Speech2TextData
    {
        [Required]
        public IFormFile? SpeechInputData { get; set; }

        public string? TextData { get; set; }
        public long? MediaId { get; set; }

        public int RemainingTries { get; set; } = 5;
    }

    public class Speech2TextRequest
    {
        public IFormFile SpeechInputData { get; set; } = default!;
    }
}