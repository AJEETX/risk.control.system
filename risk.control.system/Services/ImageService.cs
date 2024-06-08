using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IImageService
    {
        Task SaveImageAsync(IFormFile imageFile);
        Task<bool> IsImageTamperedAsync(IFormFile imageFile);
    }
    public class ImageService : IImageService
    {
        private readonly ApplicationDbContext context;

        public ImageService(ApplicationDbContext context)
        {
            this.context = context;
        }
        public async Task SaveImageAsync(IFormFile imageFile)
        {
            using (var ms = new MemoryStream())
            {
                await imageFile.CopyToAsync(ms);
                var imageBytes = ms.ToArray();
                var imageHash = ImageHashHelper.GenerateImageHash(imageBytes);

                var image = new ImageDetails
                {
                    FileName = imageFile.FileName,
                    Hash = imageHash,
                    ImageData = imageBytes
                };

                context.ImageDetails.Add(image);
                await context.SaveChangesAsync();
            }
        }
        public async Task<bool> IsImageTamperedAsync(IFormFile imageFile)
        {
            using (var ms = new MemoryStream())
            {
                await imageFile.CopyToAsync(ms);
                var imageBytes = ms.ToArray();
                var newImageHash = ImageHashHelper.GenerateImageHash(imageBytes);

                // Retrieve original image hash from database
                var originalImage = await context.ImageDetails.FirstOrDefaultAsync(img => img.FileName == imageFile.FileName);

                if (originalImage == null)
                {
                    throw new Exception("Original image not found");
                }

                return originalImage.Hash != newImageHash;
            }
        }

    }
}
