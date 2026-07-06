using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models.ViewModel
{
    public class ItrViewModel
    {
        // Form Input
        [Required(ErrorMessage = "Please select an ITR PDF file to verify.")]
        public IFormFile PdfFile { get; set; } = null!;

        // Process Flags
        public bool IsProcessed { get; set; } = false;
        public string? ErrorMessage { get; set; }

        // Service Result Captures
        public bool IsSignatureValid { get; set; }
        public bool IsMetadataClean { get; set; }

        // Calculated Helper Properties
        public bool IsDocumentAuthentic => IsSignatureValid && IsMetadataClean;
        public int RemainingTries { get; set; } = 5;

    }
}
