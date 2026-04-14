using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

using Image = SixLabors.ImageSharp.Image;

namespace risk.control.system.Services.Common
{
    public interface IImageConverter
    {
        byte[] ConvertToPng(byte[] imageBytes);
        byte[] ConvertToPngFromPath(IWebHostEnvironment webHostEnvironment, string imagePath);
        Task<byte[]> DownloadMapImageAsync(IHttpClientFactory httpClientFactory, string url);
    }
    internal class ImageConverter : IImageConverter
    {
        private const int maxWidth = 50;
        private const int maxHeight = 50;
        public byte[] ConvertToPng(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentException("Input image data is null or empty.", nameof(imageBytes));

            try
            {
                using var inputStream = new MemoryStream(imageBytes);
                using var image = Image.Load(inputStream);

                // 1. Resize if necessary while maintaining aspect ratio
                if (image.Width > maxWidth || image.Height > maxHeight)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(maxWidth, maxHeight)
                    }));
                }

                using var outputStream = new MemoryStream();

                // 2. Configure PNG Encoder
                // Note: PngColorType.Rgb is fine, but RgbWithAlpha is safer if
                // the source image has transparency (like a logo).
                var pngEncoder = new PngEncoder
                {
                    CompressionLevel = PngCompressionLevel.BestCompression,
                    TransparentColorMode = PngTransparentColorMode.Preserve,
                    ColorType = PngColorType.RgbWithAlpha
                };

                image.Save(outputStream, pngEncoder);
                return outputStream.ToArray();
            }
            catch (UnknownImageFormatException)
            {
                throw new InvalidOperationException("The provided byte array is not a supported image format.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to convert image to PNG.", ex);
            }
        }

        public byte[] ConvertToPngFromPath(IWebHostEnvironment webHostEnvironment, string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentException("Input image URL is null or empty.", nameof(imagePath));
            try
            {
                var imageBytes = File.ReadAllBytes(Path.Combine(webHostEnvironment.ContentRootPath, imagePath));
                return ResizeCropToPng(imageBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to download or convert image from URL.", ex);
            }
        }

        private static byte[] ResizeCropToPng(byte[] imageBytes, int width = 150)
        {
            using var image = Image.Load(imageBytes);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Crop,
                Size = new Size(width, 0),
                Position = AnchorPositionMode.Center // Keeps the face/center of the image
            }));

            using var ms = new MemoryStream();
            // Using a faster compression for thumbnails
            image.Save(ms, new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.DefaultCompression
            });

            return ms.ToArray();
        }
        public async Task<byte[]> DownloadMapImageAsync(IHttpClientFactory httpClientFactory, string url)
        {
            var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                throw new Exception($"Failed to download map image. Status: {response.StatusCode}");
            }
        }
    }
}