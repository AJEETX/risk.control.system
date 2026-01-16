using Amazon.Rekognition.Model;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

using Image = SixLabors.ImageSharp.Image;
namespace risk.control.system.Helpers
{
    public static class ImageConverterToPng
    {
        static int maxWidth = 100;
        static int maxHeight = 100;
        public static byte[] ConvertToPng(byte[] imageBytes, string onlyExtension = "png")
        {
            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentException("Input image data is null or empty.", nameof(imageBytes));

            try
            {
                using var inputStream = new MemoryStream(imageBytes);
                using var image = Image.Load(inputStream); 
                if (image.Width > maxWidth || image.Height > maxHeight)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(maxWidth, maxHeight)
                    }));
                }

                using var outputStream = new MemoryStream();

                var pngEncoder = new PngEncoder
                {
                    CompressionLevel = PngCompressionLevel.BestCompression,
                    ColorType = PngColorType.Rgb
                };
                image.Save(outputStream, pngEncoder);
                int width = image.Width;
                int height = image.Height;
                return outputStream.ToArray();
            }
            catch (UnknownImageFormatException)
            {
                throw new InvalidOperationException("The provided byte array is not a supported image format.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to convert image to the specified format.", ex);
            }
        }

        public static byte[] ConvertToPngFromUrl(IWebHostEnvironment webHostEnvironment, string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("Input image URL is null or empty.", nameof(imageUrl));
            try
            {
                var imageBytes = File.ReadAllBytes(Path.Combine(webHostEnvironment.ContentRootPath, imageUrl));
                return ResizeCropToPng(imageBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to download or convert image from URL.", ex);
            }
        }
        
        public static byte[] ResizeCropToPng(byte[] imageBytes,  int width = 150, int height = 150)
        {
            using var image = Image.Load(imageBytes);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Crop,   // fills and crops
                Size = new Size(width, height),
                Position = AnchorPositionMode.Center
            }));

            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());

            return ms.ToArray();
        }

    }
}