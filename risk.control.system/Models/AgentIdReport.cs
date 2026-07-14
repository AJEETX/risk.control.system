using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class AgentIdReport : ReportBase
    {
        public string? DigitalIdImageMatchConfidence { get; set; } = string.Empty;
        public float Similarity { get; set; } = 0;
        public bool Has2Face { get; set; } = false;
        public DigitalIdReportType ReportType { get; set; }
        public FaceAnalysisResult? FaceResult { get; set; }

        override public string ToString()
        {
            return $"AgentIdReport: " +
                   $"Report Type={ReportType}, " +
                   $"Image Match Confidence={DigitalIdImageMatchConfidence}, " +
                   $"Similarity={Similarity}, " +
                   $"Has front and back Face={Has2Face}";
        }
    }

    public class FaceAnalysisResult
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public int TotalFacesDetected { get; set; }
        public List<FaceDetailModel> Faces { get; set; } = new List<FaceDetailModel>();
    }
    public class FaceDetailModel
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public int FaceNumber { get; set; }
        public string AgeRange { get; set; } = default!;
        public string Gender { get; set; } = default!;
        public float GenderConfidence { get; set; } = default!;
        public string PrimaryEmotion { get; set; } = default!;
        public float EmotionConfidence { get; set; }
        public bool IsSmiling { get; set; }
        public bool HasBeard { get; set; }
        public bool IsWearingGlasses { get; set; }
    }
}