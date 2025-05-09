using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
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
                using var image = Image.Load(inputStream); // Auto-detects format
                                                           // Resize image proportionally if larger than max dimensions
                if (image.Width > maxWidth || image.Height > maxHeight)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(maxWidth, maxHeight)
                    }));
                }

                using var outputStream = new MemoryStream();

                //onlyExtension = onlyExtension?.Trim().ToLowerInvariant().Replace(".", "") ?? "png";

                //if (onlyExtension == "jpg" || onlyExtension == "jpeg")
                //{
                //    image.SaveAsPng(outputStream);
                //}
                //else
                //{
                //    image.SaveAsPng(outputStream);
                //}

                //outputStream.Seek(0, SeekOrigin.Begin);
                var pngEncoder = new PngEncoder
                {
                    CompressionLevel = PngCompressionLevel.BestCompression,
                    ColorType = PngColorType.Rgb
                };
                image.Save(outputStream, pngEncoder);
                int width =  image.Width;
                int height =  image.Height;
                return outputStream.ToArray();
            }
            catch (SixLabors.ImageSharp.UnknownImageFormatException)
            {
                throw new InvalidOperationException("The provided byte array is not a supported image format.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to convert image to the specified format.", ex);
            }
        }

    }
}