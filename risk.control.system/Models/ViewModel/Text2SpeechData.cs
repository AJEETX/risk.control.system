using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models.ViewModel
{
    public class Text2SpeechData
    {
        [Required]
        public string? TextData { get; set; }

        public byte[]? TextOutputAudio { get; set; }

        public int RemainingTries { get; set; } = 5;
    }
    public class Text2Speech
    {
        public string TextData { get; set; } = "this is a test text";
    }
}