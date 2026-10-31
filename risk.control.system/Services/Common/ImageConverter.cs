using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

using Image = SixLabors.ImageSharp.Image;

namespace risk.control.system.Services.Common
{
    public interface IImageConverter
    {
        byte[] ConvertToPng(byte[] imageBytes);
        byte[] ConvertToPngFromPath(string imagePath);
        Task<byte[]> DownloadMapImageAsync(string url);
    }
    internal class ImageConverter(IHttpClientFactory httpClientFactory, IWebHostEnvironment env, ILogger<ImageConverter> logger) : IImageConverter
    {
        private const int maxWidth = 50;
        private const int maxHeight = 50;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly IWebHostEnvironment _env = env;
        private readonly ILogger<ImageConverter> _logger = logger;
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

        public byte[] ConvertToPngFromPath(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentException("Input image URL is null or empty.", nameof(imagePath));
            try
            {
                var imageBytes = File.ReadAllBytes(Path.Combine(_env.ContentRootPath, imagePath));
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
        public async Task<byte[]> DownloadMapImageAsync(string url)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                // Set a reasonable timeout to prevent hanging requests
                client.Timeout = TimeSpan.FromSeconds(10);

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }

                // Log non-success status codes (e.g., 404, 500)
                _logger?.LogWarning("Failed to download map image from {Url}. Status Code: {StatusCode}", url, response.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                // Catches DNS resolution failures ("No such host is known"), connection refused, etc.
                _logger?.LogError(ex, "Network or DNS error while downloading map image from {Url}: {Message}", url, ex.Message);
            }
            catch (TaskCanceledException ex)
            {
                // Catches request timeouts
                _logger?.LogError(ex, "Timeout occurred while trying to download map image from {Url}", url);
            }
            catch (Exception ex)
            {
                // Catches any other unexpected errors
                _logger?.LogError(ex, "Unexpected error while downloading map image from {Url}", url);
            }

            // Return a fallback placeholder image byte array so report generation doesn't crash
            return await GetFallbackPlaceholderImage();
        }

        private async Task<byte[]> GetFallbackPlaceholderImage()
        {
            string mapImagePath = Path.Combine(_env.WebRootPath, "img", "no-map.jpeg");

            try
            {
                var byteImage = await File.ReadAllBytesAsync(mapImagePath);
                return byteImage;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to decode fallback placeholder image.");
                return Array.Empty<byte>();
            }
        }
    }
}