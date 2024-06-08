using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Helpers;

namespace risk.control.system.Controllers
{
    public class ImageController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ImageController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> VerifyImage(IFormFile imageFile)
        {
            var isTampered = await IsImageTamperedAsync(imageFile);
            if (isTampered)
            {
                return BadRequest("Image has been tampered with.");
            }
            else
            {
                return Ok("Image is authentic.");
            }
        }

        private async Task<bool> IsImageTamperedAsync(IFormFile imageFile)
        {
            using (var ms = new MemoryStream())
            {
                await imageFile.CopyToAsync(ms);
                var imageBytes = ms.ToArray();
                var newImageHash = ImageHashHelper.GenerateImageHash(imageBytes);

                // Retrieve original image hash and metadata from the database
                var originalImage = await _context.ImageDetails.FirstOrDefaultAsync(img => img.FileName == imageFile.FileName);
                if (originalImage == null)
                {
                    throw new Exception("Original image not found");
                }

                var isHashDifferent = originalImage.Hash != newImageHash;
                var originalMetadata = MetadataHelper.ExtractMetadata(originalImage.ImageData);
                var newMetadata = MetadataHelper.ExtractMetadata(imageBytes);
                var isMetadataDifferent = !MetadataHelper.CompareMetadata(originalMetadata, newMetadata);

                return isHashDifferent || isMetadataDifferent;
            }
        }
    }

}
