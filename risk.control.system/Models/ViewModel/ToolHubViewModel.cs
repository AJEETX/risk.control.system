namespace risk.control.system.Models.ViewModel
{
    public class ToolHubViewModel
    {
        public int FaceMatchRemaining { get; set; }
        public int OcrRemaining { get; set; }
        public int PdfRemaining { get; set; }
        public int DocumentAnalysisRemaining { get; set; }
        public int MaxLimit { get; set; } = 5;
    }
}
