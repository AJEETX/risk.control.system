using MetadataExtractor;
using risk.control.system.Models.ViewModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

using Image = SixLabors.ImageSharp.Image;

namespace risk.control.system.Services.Tool
{
    public interface IImageAnalysisService
    {
        Task<ImageAnalysisViewModel> AnalyzeImageAsync(IFormFile file);
    }

    internal class ImageAnalysisService : IImageAnalysisService
    {
        private readonly string _uploadPath;

        public ImageAnalysisService(IWebHostEnvironment env)
        {
            _uploadPath = Path.Combine(env.WebRootPath, "uploads");
            if (!System.IO.Directory.Exists(_uploadPath)) System.IO.Directory.CreateDirectory(_uploadPath);
        }

        public async Task<ImageAnalysisViewModel> AnalyzeImageAsync(IFormFile file)
        {
            var filePath = Path.Combine(_uploadPath, file.FileName);
            var elaPath = Path.Combine(_uploadPath, "ela_" + file.FileName);

            // Save original file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Run Analysis
            bool isSuspicious = CheckMetadata(filePath);
            var score = GenerateElaImage(filePath, elaPath);

            var model = new ImageAnalysisViewModel
            {
                OriginalImageUrl = $"/uploads/{file.FileName}",
                ElaImageUrl = $"/uploads/ela_{file.FileName}",
                MetadataFlagged = isSuspicious,
                ElaScore = Math.Round(score, 2)
            };
            if (isSuspicious)
                model.AnalysisNotes.Add("Warning: Editing software (Photoshop/GIMP) detected in metadata.");
            return model;
        }

        private double GenerateElaImage(string sourcePath, string outputPath, int quality = 90)
        {
            using var image = Image.Load<Rgba32>(sourcePath);
            using var ms = new MemoryStream();

            // Re-save at a specific quality to see how pixels react
            image.SaveAsJpeg(ms, new JpegEncoder { Quality = quality });
            ms.Position = 0;
            using var compressed = Image.Load<Rgba32>(ms);

            using var elaImage = new Image<Rgba32>(image.Width, image.Height);

            double totalDiff = 0;

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var p1 = image[x, y];
                    var p2 = compressed[x, y];

                    // Calculate difference for the score
                    int diffR = Math.Abs(p1.R - p2.R);
                    int diffG = Math.Abs(p1.G - p2.G);
                    int diffB = Math.Abs(p1.B - p2.B);

                    totalDiff += (diffR + diffG + diffB);

                    // Amplify differences for the visual ELA image (the *20 you had)
                    byte r = (byte)Math.Clamp(diffR * 20, 0, 255);
                    byte g = (byte)Math.Clamp(diffG * 20, 0, 255);
                    byte b = (byte)Math.Clamp(diffB * 20, 0, 255);

                    elaImage[x, y] = new Rgba32(r, g, b);
                }
            }

            elaImage.SaveAsPng(outputPath);

            // Calculate Average Difference per pixel (0 to 255 range)
            double avgDiff = totalDiff / (image.Width * image.Height * 3);

            // Convert to a percentage where 100% is perfectly consistent (0 diff)
            // We cap it at 100 and floor it at 0.
            double consistency = Math.Max(0, 100 - (avgDiff * 2));
            return consistency;
        }

        private bool CheckMetadata(string path)
        {
            var directories = ImageMetadataReader.ReadMetadata(path);
            return directories.Any(d => d.Tags.Any(t =>
                t.Name.Contains("Software") &&
                (t.Description.Contains("Adobe") || t.Description.Contains("GIMP"))));
        }
    }
}