namespace risk.control.system.Models.ViewModel
{
    public class DocumentOcrData
    {
        public IFormFile DocumentImage { get; set; }
        public int RemainingTries { get; set; } = 5;
    }
}
