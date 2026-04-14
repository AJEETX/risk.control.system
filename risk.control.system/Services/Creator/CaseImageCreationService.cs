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
        public async Task<byte[]> GetImagesWithDataInSubfolder(byte[] zipData, string subfolderName, string filename = "")
        {
            if (string.IsNullOrWhiteSpace(subfolderName) || string.IsNullOrWhiteSpace(filename))
            {
                return null!;
            }
            List<(string FileName, byte[] ImageData)> images = [];

            await using (MemoryStream zipStream = new MemoryStream(zipData))
            await using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                // Loop through each entry in the archive
                foreach (var entry in archive.Entries)
                {
                    // Convert path to standard format (Windows)
                    string folderPath = entry.FullName.Replace("/", "\\");

                    // Check if the entry is inside the desired subfolder and is an image file
                    if (folderPath.Contains("\\" + subfolderName + "\\", StringComparison.CurrentCultureIgnoreCase) && IsImageFile(entry.FullName))
                    {
                        // Extract image data
                        await using MemoryStream imageStream = new();
                        await using (Stream entryStream = entry.Open())
                        {
                            await entryStream.CopyToAsync(imageStream);
                        }

                        // Add file name and byte array to the result list
                        images.Add((entry.Name, imageStream.ToArray()));
                    }
                }
            }

            var (FileName, ImageData) = images.FirstOrDefault(i => i.FileName == filename);
            return ImageData ?? null!;
        }

        private static bool IsImageFile(string filePath)
        {
            // Check if the file is an image based on file extension
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
            string extension = Path.GetExtension(filePath)?.ToLower()!;
            return imageExtensions.Contains(extension);
        }
    }
}