namespace risk.control.system.Models.ViewModel
{
    public class ImageAnalysisViewModel
    {
        public string OriginalImageUrl { get; set; }
        public string ElaImageUrl { get; set; }
        public bool MetadataFlagged { get; set; }
        public List<string> AnalysisNotes { get; set; } = new List<string>();
        public int RemainingTries { get; set; } = 5;

    }
}
