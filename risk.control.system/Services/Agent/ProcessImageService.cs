using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace risk.control.system.Services.Agent
{
    public interface IProcessImageService
    {
        byte[] CompressImage(byte[] imageBytes, int quality = 80, float blurSigma = 5f);
        byte[] CompressAndWaterMarkFace(byte[] imageBytes, int quality = 80, string watermarkText = "VERIFIED", Color watermarkColor = default);
        byte[] CompressAndWaterMarkImage(byte[] imageBytes, int quality = 80, string watermarkText = "VERIFIED", float blurSigma = 0f);
    }

    internal class ProcessImageService(ILogger<ProcessImageService> logger) : IProcessImageService
    {
        private const int maxWidth = 1200;
        private readonly ILogger<ProcessImageService> _logger = logger;

        public byte[] CompressImage(byte[] imageBytes, int quality = 80, float blurSigma = 5f)
        {
            if (imageBytes == null || imageBytes.Length == 0) return Array.Empty<byte>();
            try
            {
                using var inputStream = new MemoryStream(imageBytes);
                using var outputStream = new MemoryStream();
                using (var image = Image.Load(inputStream))
                {
                    image.Mutate(x => x.AutoOrient());
                    if (image.Width > maxWidth)
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(maxWidth, 0), Mode = ResizeMode.Max }));
                    }
                    if (blurSigma > 0)
                    {
                        image.Mutate(x => x.GaussianBlur(blurSigma));
                    }
                    image.Save(outputStream, new JpegEncoder { Quality = quality });
                }
                return outputStream.ToArray();
            }
            catch (UnknownImageFormatException ex)
            {
                _logger.LogError(ex, "Format Error: Data is not a valid image.");
                return imageBytes; // Return original on error
            }
        }

        public byte[] CompressAndWaterMarkFace(byte[] imageBytes, int quality = 80, string watermarkText = "VERIFIED", Color watermarkColor = default)
        {
            if (imageBytes == null || imageBytes.Length == 0) return Array.Empty<byte>();
            try
            {
                using var inputStream = new MemoryStream(imageBytes);
                using var outputStream = new MemoryStream();
                using (var image = Image.Load(inputStream))
                {
                    image.Mutate(x => x.AutoOrient());
                    if (image.Width > maxWidth)
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(maxWidth, 0), Mode = ResizeMode.Max }));
                    }
                    ApplyWatermarks(image, watermarkText, watermarkColor);

                    image.Save(outputStream, new JpegEncoder { Quality = quality });
                }
                return outputStream.ToArray();
            }
            catch (UnknownImageFormatException ex)
            {
                _logger.LogError(ex, "Format Error: Data is not a valid image.");
                return imageBytes; // Return original on error
            }
        }

        public byte[] CompressAndWaterMarkImage(byte[] imageBytes, int quality = 80, string watermarkText = "VERIFIED", float blurSigma = 0f)
        {
            if (imageBytes == null || imageBytes.Length == 0) return Array.Empty<byte>();
            try
            {
                using var inputStream = new MemoryStream(imageBytes);
                using var outputStream = new MemoryStream();
                using (var image = Image.Load(inputStream))
                {
                    image.Mutate(x => x.AutoOrient());
                    if (image.Width > maxWidth)
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(maxWidth, 0), Mode = ResizeMode.Max }));
                    }
                    if (blurSigma > 0)
                    {
                        image.Mutate(x => x.GaussianBlur(blurSigma));
                        ApplyWatermarks(image, watermarkText, Color.Red);
                    }
                    else
                    {
                        ApplyWatermarks(image, watermarkText, Color.White);
                    }
                    image.Save(outputStream, new JpegEncoder { Quality = quality });
                }
                return outputStream.ToArray();
            }
            catch (UnknownImageFormatException ex)
            {
                _logger.LogError(ex, "Format Error: Data is not a valid image.");
                return imageBytes; // Return original on error
            }
        }
        private static void ApplyWatermarks(Image image, string watermarkText, Color watermarkColor)
        {
            float baseScale = Math.Min(image.Width, image.Height);
            float mainFontSize = baseScale * 0.12f;
            float dateFontSize = baseScale * 0.045f;
            float hPadding = mainFontSize * 0.35f;
            float vPadding = mainFontSize * 0.20f;
            float borderThickness = Math.Max(1f, baseScale * 0.005f);
            float shadowOffset = Math.Max(1f, mainFontSize * 0.04f);
            if (!SystemFonts.Collection.TryGet("Arial", out var family)) family = SystemFonts.Collection.Families.First();
            Font mainFont = family.CreateFont(mainFontSize, FontStyle.Bold);
            Font dateFont = family.CreateFont(dateFontSize, FontStyle.Bold);
            string timestamp = DateTime.UtcNow.ToLocalTime().ToString("dd-MMM-yyyy HH:mm");
            image.Mutate(ctx =>
            {
                var dateOptions = new RichTextOptions(dateFont) { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Origin = new PointF(image.Width - (baseScale * 0.02f), baseScale * 0.02f) };
                var dateSize = TextMeasurer.MeasureSize(timestamp, dateOptions);
                var dateRect = new RectangleF(dateOptions.Origin.X - dateSize.Width - 10, dateOptions.Origin.Y - 5, dateSize.Width + 20, dateSize.Height + 10);
                //ctx.Fill(Color.White.WithAlpha(0.5f), dateRect);
                ctx.Draw(Color.Gray, borderThickness / 2, dateRect);
                ctx.DrawText(dateOptions, timestamp, Color.Black.WithAlpha(0.8f));
                var centerPoint = new PointF(image.Width / 2, image.Height * 0.85f);
                float radians = -20f * (MathF.PI / 180f);
                var rotationMatrix = Matrix3x2.CreateRotation(radians, centerPoint);
                var textOptions = new RichTextOptions(mainFont) { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                var textSize = TextMeasurer.MeasureSize(watermarkText, textOptions);
                var mainRect = new RectangleF(centerPoint.X - (textSize.Width / 2) - hPadding, centerPoint.Y - (textSize.Height / 2) - vPadding, textSize.Width + (hPadding * 2), textSize.Height + (vPadding * 2));
                float verticalNudge = mainFont.Size * 0.15f;
                var textLocation = new PointF(centerPoint.X - (textSize.Width / 2), centerPoint.Y - (textSize.Height / 2) - verticalNudge);
                IPath slantedRectPath = new RectangularPolygon(mainRect).Transform(rotationMatrix);
                ctx.Draw(Color.Silver, borderThickness, slantedRectPath);
                var textDrawOptions = new DrawingOptions { Transform = rotationMatrix };
                ctx.DrawText(textDrawOptions, watermarkText, mainFont, Color.Black, new PointF(textLocation.X + shadowOffset, textLocation.Y + shadowOffset));
                ctx.DrawText(textDrawOptions, watermarkText, mainFont, watermarkColor, textLocation);
            });
        }
    }
}