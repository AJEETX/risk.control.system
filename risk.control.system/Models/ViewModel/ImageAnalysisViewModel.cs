namespace risk.control.system.Models.ViewModel
{
    public class ImageAnalysisViewModel
    {
        public string OriginalImageUrl { get; set; }
        public string ElaImageUrl { get; set; }
        public bool MetadataFlagged { get; set; }
        public List<string> AnalysisNotes { get; set; } = new List<string>();
        public double ElaScore { get; set; }

        public string ElaStatus => ElaScore > 90 ? "High Consistency" : ElaScore > 75 ? "Moderate Variance" : "Low Consistency / Likely Modified";

        public int RemainingTries { get; set; } = 5;
    }
}