using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models.ViewModel
{
    public class FaceMatchData
    {
        [Required]
        public IFormFile OriginalFaceImage { get; set; }

        [Required]
        public IFormFile MatchFaceImage { get; set; }

        public int RemainingTries { get; set; } = 5;
    }
}