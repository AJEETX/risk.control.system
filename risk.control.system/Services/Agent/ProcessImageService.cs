using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace risk.control.system.Services.Agent
{
    public interface IProcessImageService
    {
        byte[] ProcessCompress(byte[] imageByte, string onlyExtension, float cornerRadius = 10, int quality = 99, Amazon.Rekognition.Model.BoundingBox? faceBox = null);

        byte[] CompressImage(byte[] imageBytes, int quality = 75, string watermarkText = "VERIFIED");
    }

    public class ProcessImageService : IProcessImageService
    {
        private readonly ILogger<ProcessImageService> _logger;

        public ProcessImageService(ILogger<ProcessImageService> logger)
        {
            _logger = logger;
        }

        public byte[] CompressImage(byte[] imageBytes, int quality = 80, string watermarkText = "VERIFIED")
        {
            if (imageBytes == null || imageBytes.Length == 0) return Array.Empty<byte>();

            try
            {
                using var inputStream = new MemoryStream(imageBytes);
                using var outputStream = new MemoryStream();

                using (var image = Image.Load(inputStream))
                {
                    // --- 1. AUTO-RESIZE (Optional: 1200px max width) ---
                    const int maxWidth = 1200;
                    if (image.Width > maxWidth)
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(maxWidth, 0), // 0 maintains aspect ratio
                            Mode = ResizeMode.Max
                        }));
                    }

                    // --- 2. AUTO-SCALE CALCULATIONS (Based on new size) ---
                    float baseScale = Math.Min(image.Width, image.Height);
                    float mainFontSize = baseScale * 0.12f;
                    float dateFontSize = baseScale * 0.045f;

                    float hPadding = mainFontSize * 0.35f;
                    float vPadding = mainFontSize * 0.20f;
                    float borderThickness = Math.Max(1f, baseScale * 0.005f);
                    float shadowOffset = Math.Max(1f, mainFontSize * 0.04f);

                    // --- 3. SETUP FONTS ---
                    if (!SystemFonts.Collection.TryGet("Arial", out var family))
                        family = SystemFonts.Collection.Families.First();

                    Font mainFont = family.CreateFont(mainFontSize, FontStyle.Bold);
                    Font dateFont = family.CreateFont(dateFontSize, FontStyle.Regular);

                    string timestamp = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");

                    image.Mutate(ctx =>
                    {
                        // --- 4. SCALED TIMESTAMP (Top Right) ---
                        var dateOptions = new RichTextOptions(dateFont)
                        {
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Top,
                            Origin = new PointF(image.Width - (baseScale * 0.02f), baseScale * 0.02f)
                        };

                        var dateSize = TextMeasurer.MeasureSize(timestamp, dateOptions);
                        var dateRect = new RectangleF(
                            dateOptions.Origin.X - dateSize.Width - 10,
                            dateOptions.Origin.Y - 5,
                            dateSize.Width + 20,
                            dateSize.Height + 10);

                        //ctx.Fill(Color.White.WithAlpha(0.5f), dateRect);
                        ctx.Draw(Color.Gray.WithAlpha(0.5f), borderThickness / 2, dateRect);
                        ctx.DrawText(dateOptions, timestamp, Color.Black.WithAlpha(0.8f));

                        // --- 5. SCALED SLANTED WATERMARK ---
                        var centerPoint = new PointF(image.Width / 2, image.Height * 0.85f);
                        float radians = -20f * (MathF.PI / 180f);
                        var rotationMatrix = Matrix3x2.CreateRotation(radians, centerPoint);

                        var textOptions = new RichTextOptions(mainFont)
                        {
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top
                        };

                        var textSize = TextMeasurer.MeasureSize(watermarkText, textOptions);

                        var mainRect = new RectangleF(
                            centerPoint.X - (textSize.Width / 2) - hPadding,
                            centerPoint.Y - (textSize.Height / 2) - vPadding,
                            textSize.Width + (hPadding * 2),
                            textSize.Height + (vPadding * 2));

                        // Final vertical nudge for visual centering
                        float verticalNudge = mainFont.Size * 0.15f;
                        var textLocation = new PointF(
                            centerPoint.X - (textSize.Width / 2),
                            centerPoint.Y - (textSize.Height / 2) - verticalNudge
                        );

                        IPath slantedRectPath = new RectangularPolygon(mainRect).Transform(rotationMatrix);

                        // Draw background and border
                        //ctx.Fill(Color.Black.WithAlpha(0.6f), slantedRectPath);
                        ctx.Draw(Color.Silver, borderThickness, slantedRectPath);

                        // --- 6. SCALED 3D TEXT LAYERS ---
                        var textDrawOptions = new DrawingOptions { Transform = rotationMatrix };

                        // Shadow
                        ctx.DrawText(textDrawOptions, watermarkText, mainFont, Color.Black,
                                     new PointF(textLocation.X + shadowOffset, textLocation.Y + shadowOffset));

                        // Main Text
                        ctx.DrawText(textDrawOptions, watermarkText, mainFont, Color.White, textLocation);
                    });

                    // Save with Jpeg Quality compression
                    image.Save(outputStream, new JpegEncoder { Quality = quality });
                }

                return outputStream.ToArray();
            }
            catch (UnknownImageFormatException ex)
            {
                _logger.LogError(ex, "Format Error: Data is not a valid image.");
                return imageBytes;
            }
        }

        public byte[] ProcessCompress(byte[] imageByte, string onlyExtension, float cornerRadius = 10, int quality = 99, Amazon.Rekognition.Model.BoundingBox? faceBox = null)
        {
            using var stream = new MemoryStream(imageByte);
            using var image = Image.Load(stream);
            //using var waterMarkedImage = image.Clone(ctx => ctx.ApplyScalingWaterMark());
            using MemoryStream streamOut = new MemoryStream();
            if (onlyExtension == ".png")
            {
                var pngEncoder = new PngEncoder
                {
                    CompressionLevel = PngCompressionLevel.BestCompression
                };
                image.Save(streamOut, pngEncoder);
            }
            else if (onlyExtension == ".jpg" || onlyExtension == ".jpeg")
            {
                var jpgEncoder = new JpegEncoder
                {
                    Quality = quality, // Adjust this value for desired compression quality
                };
                image.Save(streamOut, jpgEncoder);
            }
            var imageOutByte = streamOut.ToArray();
            return imageOutByte;
        }
    }
}