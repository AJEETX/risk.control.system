namespace risk.control.system.Models.ViewModel
{
    public class FaceMatchData
    {
        public IFormFile OriginalFaceImage { get; set; }
        public IFormFile MatchFaceImage { get; set; }
        public int RemainingTries { get; set; } = 5;
    }
}
