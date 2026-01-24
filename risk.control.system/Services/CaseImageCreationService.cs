using System.Globalization;
using System.IO.Compression;

using Microsoft.EntityFrameworkCore;

namespace risk.control.system.Services
{
    public interface ICaseImageCreationService
    {
        Task<byte[]> GetImagesWithDataInSubfolder(byte[] zipData, string subfolderName, string filename = "");
    }
    internal class CaseImageCreationService : ICaseImageCreationService
    {
        private readonly ILogger<CaseImageCreationService> logger;

        public CaseImageCreationService(ILogger<CaseImageCreationService> logger)
        {
            this.logger = logger;
        }

        public async Task<byte[]> GetImagesWithDataInSubfolder(byte[] zipData, string subfolderName, string filename = "")
        {
            if (string.IsNullOrWhiteSpace(subfolderName) || string.IsNullOrWhiteSpace(filename))
            {
                return null!;
            }
            List<(string FileName, byte[] ImageData)> images = new List<(string, byte[])>();

            using (MemoryStream zipStream = new MemoryStream(zipData))
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                // Loop through each entry in the archive
                foreach (var entry in archive.Entries)
                {
                    // Convert path to standard format (Windows)
                    string folderPath = entry.FullName.Replace("/", "\\");

                    // Check if the entry is inside the desired subfolder and is an image file
                    if (folderPath.ToLower(CultureInfo.InvariantCulture).Contains("\\" + subfolderName + "\\") && IsImageFile(entry.FullName))
                    {
                        // Extract image data
                        using (MemoryStream imageStream = new MemoryStream())
                        {
                            using (Stream entryStream = entry.Open())
                            {
                                await entryStream.CopyToAsync(imageStream);
                            }

                            // Add file name and byte array to the result list
                            images.Add((entry.Name, imageStream.ToArray()));
                        }
                    }
                }
            }

            var image = images.FirstOrDefault(i => i.FileName == filename);
            if (image.ImageData != null)
            {
                return image.ImageData;
                //var compressed = CompressImage.ProcessCompress(image.ImageData, ".jpg");
                //return compressed;
            }
            return null;
        }

        private static bool IsImageFile(string filePath)
        {
            // Check if the file is an image based on file extension
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
            string extension = Path.GetExtension(filePath)?.ToLower();
            return imageExtensions.Contains(extension);
        }
    }
}
