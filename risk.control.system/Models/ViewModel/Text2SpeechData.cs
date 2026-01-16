namespace risk.control.system.Models.ViewModel
{
    public class Text2SpeechData
    {
        public string TextData { get; set; }
        public byte[]? TextOutputAudio { get; set; }
        public int RemainingTries { get; set; } = 5;
    }
}
